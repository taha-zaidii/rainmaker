using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Dapper;

namespace Digi.Shared.Services
{
    public interface IDatabaseMigrationService
    {
        Task<bool> EnsureDatabaseExistsAsync();
        Task<bool> RunMigrationsAsync();
        Task<bool> CheckDatabaseExistsAsync();
        Task<bool> CreateDatabaseAsync();
    }

    public class DatabaseMigrationService : IDatabaseMigrationService
    {
        private readonly string _connectionString;
        private readonly string _masterConnectionString;
        private readonly ILogger<DatabaseMigrationService> _logger;
        private readonly string _databaseName;
        private readonly string _serverName;
        private readonly DatabaseMigrationSettings _settings;

        public DatabaseMigrationService(IConfiguration configuration, ILogger<DatabaseMigrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _settings = DatabaseMigrationConfiguration.GetMigrationSettings(configuration);

            // Parse connection string to get database name and server
            var builder = new SqlConnectionStringBuilder(_connectionString);
            _databaseName = builder.InitialCatalog;
            _serverName = builder.DataSource;

            // Create master connection string for database operations
            builder.InitialCatalog = "master";
            _masterConnectionString = builder.ConnectionString;
        }

        public async Task<bool> CheckDatabaseExistsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_masterConnectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) 
                    FROM sys.databases 
                    WHERE name = @DatabaseName";

                var result = await connection.QuerySingleAsync<int>(query, new { DatabaseName = _databaseName });
                
                _logger.LogInformation("Database '{DatabaseName}' exists: {Exists}", _databaseName, result > 0);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if database '{DatabaseName}' exists", _databaseName);
                return false;
            }
        }

        public async Task<bool> CreateDatabaseAsync()
        {
            try
            {
                using var connection = new SqlConnection(_masterConnectionString);
                await connection.OpenAsync();

                var createDbScript = $@"
                    IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{_databaseName}')
                    BEGIN
                        CREATE DATABASE [{_databaseName}]
                        COLLATE SQL_Latin1_General_CP1_CI_AS;
                        
                        PRINT 'Database [{_databaseName}] created successfully.';
                    END
                    ELSE
                    BEGIN
                        PRINT 'Database [{_databaseName}] already exists.';
                    END";

                await connection.ExecuteAsync(createDbScript);
                
                _logger.LogInformation("Database '{DatabaseName}' created successfully", _databaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating database '{DatabaseName}'", _databaseName);
                return false;
            }
        }

        public async Task<bool> EnsureDatabaseExistsAsync()
        {
            try
            {
                if (!_settings.CreateDatabaseIfNotExists)
                {
                    _logger.LogInformation("Database creation is disabled in configuration");
                    return await CheckDatabaseExistsAsync();
                }

                var exists = await CheckDatabaseExistsAsync();
                if (!exists)
                {
                    _logger.LogInformation("Database '{DatabaseName}' does not exist. Creating...", _databaseName);
                    return await CreateDatabaseAsync();
                }
                
                _logger.LogInformation("Database '{DatabaseName}' already exists", _databaseName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring database '{DatabaseName}' exists", _databaseName);
                return false;
            }
        }

        public async Task<bool> RunMigrationsAsync()
        {
            try
            {
                if (!_settings.AutoRunMigrations)
                {
                    _logger.LogInformation("Auto-run migrations is disabled in configuration");
                    return true;
                }

                // First ensure database exists
                if (!await EnsureDatabaseExistsAsync())
                {
                    _logger.LogError("Failed to ensure database exists");
                    return false;
                }

                // Create migration tracking table if it doesn't exist
                await CreateMigrationTrackingTableAsync();

                // Get all migration scripts
                var migrationScripts = GetMigrationScripts();
                
                foreach (var script in migrationScripts.OrderBy(s => s.Version))
                {
                    if (await IsMigrationAppliedAsync(script.Version))
                    {
                        _logger.LogInformation("Migration {Version} already applied, skipping", script.Version);
                        continue;
                    }

                    _logger.LogInformation("Applying migration {Version}: {Description}", script.Version, script.Description);
                    
                    if (await ExecuteMigrationScriptAsync(script))
                    {
                        await MarkMigrationAsAppliedAsync(script.Version, script.Description);
                        _logger.LogInformation("Migration {Version} applied successfully", script.Version);
                    }
                    else
                    {
                        _logger.LogError("Failed to apply migration {Version}", script.Version);
                        return false;
                    }
                }

                _logger.LogInformation("All migrations completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running migrations");
                return false;
            }
        }

        private async Task CreateMigrationTrackingTableAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var createTableScript = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DatabaseMigrations' AND xtype='U')
                    BEGIN
                        CREATE TABLE [dbo].[DatabaseMigrations] (
                            [Id] INT IDENTITY(1,1) PRIMARY KEY,
                            [Version] NVARCHAR(50) NOT NULL UNIQUE,
                            [Description] NVARCHAR(500) NOT NULL,
                            [AppliedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                            [AppliedBy] NVARCHAR(100) NOT NULL DEFAULT SYSTEM_USER,
                            [ScriptContent] NVARCHAR(MAX) NULL
                        );
                        
                        PRINT 'DatabaseMigrations table created successfully.';
                    END";

                await connection.ExecuteAsync(createTableScript);
                _logger.LogInformation("Migration tracking table ensured");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating migration tracking table");
                throw;
            }
        }

        private async Task<bool> IsMigrationAppliedAsync(string version)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "SELECT COUNT(*) FROM DatabaseMigrations WHERE Version = @Version";
                var result = await connection.QuerySingleAsync<int>(query, new { Version = version });
                
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if migration {Version} is applied", version);
                return false;
            }
        }

        private async Task<bool> ExecuteMigrationScriptAsync(MigrationScript script)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    // Split script by GO statements and execute each part separately
                    var scriptParts = script.Script.Split(new[] { "GO\r\n", "GO\n", "GO " }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var part in scriptParts)
                    {
                        var trimmedPart = part.Trim();
                        if (!string.IsNullOrEmpty(trimmedPart))
                        {
                            await connection.ExecuteAsync(trimmedPart, transaction: transaction, commandTimeout: _settings.CommandTimeout);
                        }
                    }
                    
                    transaction.Commit();
                    
                    if (_settings.LogScriptContent)
                    {
                        _logger.LogDebug("Executed migration script {Version}: {Script}", script.Version, script.Script);
                    }
                    
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing migration script {Version}", script.Version);
                
                if (_settings.StopOnError)
                {
                    return false;
                }
                
                _logger.LogWarning("Continuing with next migration despite error in {Version}", script.Version);
                return true; // Continue with other migrations
            }
        }

        private async Task MarkMigrationAsAppliedAsync(string version, string description)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var insertScript = @"
                    INSERT INTO DatabaseMigrations (Version, Description, AppliedAt, AppliedBy)
                    VALUES (@Version, @Description, GETUTCDATE(), SYSTEM_USER)";

                await connection.ExecuteAsync(insertScript, new { Version = version, Description = description });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking migration {Version} as applied", version);
                throw;
            }
        }

        private List<MigrationScript> GetMigrationScripts()
        {
            var scripts = new List<MigrationScript>();

            // Get all SQL files from the configured Migrations folder
            var migrationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.MigrationsPath);
            
            if (!Directory.Exists(migrationsPath))
            {
                _logger.LogWarning("Migrations folder not found at: {Path}", migrationsPath);
                return scripts;
            }

            var sqlFiles = Directory.GetFiles(migrationsPath, "*.sql", SearchOption.AllDirectories)
                .OrderBy(f => f);

            foreach (var file in sqlFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    
                    // Extract version and description from filename or content
                    var version = ExtractVersionFromFileName(fileName);
                    var description = ExtractDescriptionFromFileName(fileName);

                    scripts.Add(new MigrationScript
                    {
                        Version = version,
                        Description = description,
                        Script = content,
                        FileName = fileName
                    });

                    _logger.LogDebug("Loaded migration script: {FileName} (Version: {Version})", fileName, version);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading migration script: {File}", file);
                }
            }

            return scripts;
        }

        private string ExtractVersionFromFileName(string fileName)
        {
            // Expected format: 001_CreateUsersTable.sql or 001-CreateUsersTable.sql
            var parts = fileName.Split(new[] { '_', '-' }, 2);
            if (parts.Length >= 1 && int.TryParse(parts[0], out _))
            {
                return parts[0];
            }
            
            // If no version prefix, use filename as version
            return fileName;
        }

        private string ExtractDescriptionFromFileName(string fileName)
        {
            // Expected format: 001_CreateUsersTable.sql
            var parts = fileName.Split(new[] { '_', '-' }, 2);
            if (parts.Length >= 2)
            {
                return parts[1].Replace("_", " ").Replace("-", " ");
            }
            
            return fileName;
        }
    }

    public class MigrationScript
    {
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}

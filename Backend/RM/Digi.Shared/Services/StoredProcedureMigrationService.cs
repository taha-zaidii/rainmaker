using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Dapper;

namespace Digi.Shared.Services
{
    public interface IStoredProcedureMigrationService
    {
        Task<bool> RunStoredProcedureMigrationsAsync();
        Task<bool> CheckStoredProcedureExistsAsync(string spName);
        Task<bool> CreateStoredProcedureAsync(string spName, string spScript);
        Task<List<string>> GetStoredProcedureScriptsAsync();
    }

    public class StoredProcedureMigrationService : IStoredProcedureMigrationService
    {
        private readonly string _connectionString;
        private readonly ILogger<StoredProcedureMigrationService> _logger;
        private readonly DatabaseMigrationSettings _settings;

        public StoredProcedureMigrationService(IConfiguration configuration, ILogger<StoredProcedureMigrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _settings = DatabaseMigrationConfiguration.GetMigrationSettings(configuration);
        }

        public async Task<bool> RunStoredProcedureMigrationsAsync()
        {
            try
            {
                if (!_settings.AutoRunStoredProcedureMigrations)
                {
                    _logger.LogInformation("Auto-run stored procedure migrations is disabled in configuration");
                    return true;
                }

                _logger.LogInformation("Starting stored procedure migration process...");

                // Create SP migration tracking table if it doesn't exist
                await CreateSPMigrationTrackingTableAsync();

                // Get all stored procedure scripts
                var spScripts = await GetStoredProcedureScriptsAsync();
                
                int totalSPs = spScripts.Count;
                int processedSPs = 0;
                int createdSPs = 0;
                int failedSPs = 0;

                _logger.LogInformation("Total stored procedures to process: {TotalSPs}", totalSPs);

                foreach (var spScript in spScripts)
                {
                    processedSPs++;
                    var spName = ExtractSPNameFromScript(spScript);
                    
                    _logger.LogInformation("Processing SP {ProcessedSPs}/{TotalSPs}: {SPName}", 
                        processedSPs, totalSPs, spName);

                    try
                    {
                        if (await CheckStoredProcedureExistsAsync(spName))
                        {
                            _logger.LogInformation("Stored procedure {SPName} already exists, skipping", spName);
                            continue;
                        }

                        if (await CreateStoredProcedureAsync(spName, spScript))
                        {
                            await MarkSPMigrationAsAppliedAsync(spName, "Success");
                            createdSPs++;
                            _logger.LogInformation("Successfully created stored procedure: {SPName}", spName);
                        }
                        else
                        {
                            await MarkSPMigrationAsAppliedAsync(spName, "Failed", "Creation failed");
                            failedSPs++;
                            _logger.LogError("Failed to create stored procedure: {SPName}", spName);
                        }
                    }
                    catch (Exception ex)
                    {
                        await MarkSPMigrationAsAppliedAsync(spName, "Failed", ex.Message);
                        failedSPs++;
                        _logger.LogError(ex, "Error creating stored procedure: {SPName}", spName);
                    }
                }

                _logger.LogInformation("Stored procedure migration completed. Created: {CreatedSPs}, Failed: {FailedSPs}, Total: {TotalSPs}", 
                    createdSPs, failedSPs, totalSPs);

                return failedSPs == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running stored procedure migrations");
                return false;
            }
        }

        public async Task<bool> CheckStoredProcedureExistsAsync(string spName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) 
                    FROM sys.objects 
                    WHERE object_id = OBJECT_ID(@SPName) AND type = 'P'";

                var result = await connection.QuerySingleAsync<int>(query, new { SPName = spName });
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if stored procedure {SPName} exists", spName);
                return false;
            }
        }

        public async Task<bool> CreateStoredProcedureAsync(string spName, string spScript)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                using var transaction = connection.BeginTransaction();
                try
                {
                    // Split script by GO statements and execute each part separately
                    var scriptParts = spScript.Split(new[] { "GO\r\n", "GO\n", "GO " }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var part in scriptParts)
                    {
                        var trimmedPart = part.Trim();
                        if (!string.IsNullOrEmpty(trimmedPart))
                        {
                            // For CREATE PROCEDURE statements, ensure they are the first statement in batch
                            if (trimmedPart.ToUpper().Contains("CREATE PROCEDURE"))
                            {
                                // Remove any leading statements before CREATE PROCEDURE
                                var lines = trimmedPart.Split('\n');
                                var createProcIndex = -1;
                                
                                for (int i = 0; i < lines.Length; i++)
                                {
                                    if (lines[i].Trim().ToUpper().StartsWith("CREATE PROCEDURE"))
                                    {
                                        createProcIndex = i;
                                        break;
                                    }
                                }
                                
                                if (createProcIndex > 0)
                                {
                                    // Extract only the CREATE PROCEDURE part and onwards
                                    var createProcLines = lines.Skip(createProcIndex);
                                    trimmedPart = string.Join("\n", createProcLines);
                                }
                            }
                            
                            await connection.ExecuteAsync(trimmedPart, transaction: transaction, commandTimeout: _settings.CommandTimeout);
                        }
                    }
                    
                    transaction.Commit();
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
                _logger.LogError(ex, "Error creating stored procedure {SPName}", spName);
                return false;
            }
        }

        public async Task<List<string>> GetStoredProcedureScriptsAsync()
        {
            var scripts = new List<string>();

            try
            {
                // Get all SQL files from the StoredProcedures folder
                var spPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.MigrationsPath, "011_StoredProcedures");
                
                if (!Directory.Exists(spPath))
                {
                    _logger.LogWarning("Stored procedures folder not found at: {Path}", spPath);
                    return scripts;
                }

                var sqlFiles = Directory.GetFiles(spPath, "*.sql", SearchOption.AllDirectories)
                    .OrderBy(f => f);

                foreach (var file in sqlFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file, Encoding.UTF8);
                        scripts.Add(content);
                        _logger.LogDebug("Loaded stored procedure script: {FileName}", Path.GetFileName(file));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading stored procedure script: {File}", file);
                    }
                }

                _logger.LogInformation("Loaded {Count} stored procedure scripts", scripts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stored procedure scripts");
            }

            return scripts;
        }

        private async Task CreateSPMigrationTrackingTableAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var createTableScript = @"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SP_Migration_Log]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[SP_Migration_Log](
                            [Id] [int] IDENTITY(1,1) NOT NULL,
                            [SPName] [nvarchar](255) NOT NULL,
                            [Status] [nvarchar](50) NOT NULL,
                            [ErrorMessage] [nvarchar](max) NULL,
                            [CreatedAt] [datetime] NOT NULL DEFAULT GETDATE(),
                            [Module] [nvarchar](50) NULL,
                            CONSTRAINT [PK_SP_Migration_Log] PRIMARY KEY CLUSTERED ([Id] ASC)
                        );
                        
                        PRINT 'SP_Migration_Log table created successfully.';
                    END
                    ELSE
                    BEGIN
                        PRINT 'SP_Migration_Log table already exists.';
                    END";

                await connection.ExecuteAsync(createTableScript);
                _logger.LogInformation("SP migration tracking table ensured");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SP migration tracking table");
                throw;
            }
        }

        private async Task MarkSPMigrationAsAppliedAsync(string spName, string status, string errorMessage = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var insertScript = @"
                    INSERT INTO SP_Migration_Log (SPName, Status, ErrorMessage, CreatedAt, Module)
                    VALUES (@SPName, @Status, @ErrorMessage, GETDATE(), 'AutoMigration')";

                await connection.ExecuteAsync(insertScript, new { 
                    SPName = spName, 
                    Status = status, 
                    ErrorMessage = errorMessage 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking SP migration {SPName} as applied", spName);
            }
        }

        private string ExtractSPNameFromScript(string script)
        {
            try
            {
                // Look for CREATE PROCEDURE pattern
                var lines = script.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Trim().ToUpper().StartsWith("CREATE PROCEDURE") || 
                        line.Trim().ToUpper().StartsWith("CREATE PROC"))
                    {
                        var parts = line.Trim().Split(' ');
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (parts[i].ToUpper() == "PROCEDURE" || parts[i].ToUpper() == "PROC")
                            {
                                if (i + 1 < parts.Length)
                                {
                                    var spName = parts[i + 1].Trim();
                                    // Remove brackets if present
                                    spName = spName.Replace("[", "").Replace("]", "");
                                    return spName;
                                }
                            }
                        }
                    }
                }
                
                return "Unknown_SP";
            }
            catch
            {
                return "Unknown_SP";
            }
        }
    }
}

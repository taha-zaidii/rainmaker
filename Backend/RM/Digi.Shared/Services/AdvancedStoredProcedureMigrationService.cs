using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Dapper;

namespace Digi.Shared.Services
{
    public interface IAdvancedStoredProcedureMigrationService
    {
        Task<bool> RunAdvancedStoredProcedureMigrationsAsync();
        Task<bool> ProcessStoredProcedureFileAsync(string filePath);
        Task<List<StoredProcedureInfo>> ExtractStoredProceduresFromScriptAsync(string script);
    }

    public class AdvancedStoredProcedureMigrationService : IAdvancedStoredProcedureMigrationService
    {
        private readonly string _connectionString;
        private readonly ILogger<AdvancedStoredProcedureMigrationService> _logger;
        private readonly DatabaseMigrationSettings _settings;

        public AdvancedStoredProcedureMigrationService(IConfiguration configuration, ILogger<AdvancedStoredProcedureMigrationService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _settings = DatabaseMigrationConfiguration.GetMigrationSettings(configuration);
        }

        public async Task<bool> RunAdvancedStoredProcedureMigrationsAsync()
        {
            try
            {
                if (!_settings.AutoRunStoredProcedureMigrations)
                {
                    _logger.LogInformation("Auto-run stored procedure migrations is disabled in configuration");
                    return true;
                }

                _logger.LogInformation("Starting advanced stored procedure migration process...");

                // Create SP migration tracking table if it doesn't exist
                await CreateSPMigrationTrackingTableAsync();

                // Get all stored procedure files
                var spPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.MigrationsPath, "011_StoredProcedures");
                
                if (!Directory.Exists(spPath))
                {
                    _logger.LogWarning("Stored procedures folder not found at: {Path}", spPath);
                    return true;
                }

                var sqlFiles = Directory.GetFiles(spPath, "*.sql", SearchOption.AllDirectories)
                    .OrderBy(f => f);

                int totalFiles = sqlFiles.Count();
                int processedFiles = 0;
                int totalSPs = 0;
                int createdSPs = 0;
                int failedSPs = 0;

                _logger.LogInformation("Found {TotalFiles} stored procedure files to process", totalFiles);

                foreach (var file in sqlFiles)
                {
                    processedFiles++;
                    _logger.LogInformation("Processing file {ProcessedFiles}/{TotalFiles}: {FileName}", 
                        processedFiles, totalFiles, Path.GetFileName(file));

                    try
                    {
                        var fileResult = await ProcessStoredProcedureFileAsync(file);
                        if (fileResult)
                        {
                            _logger.LogInformation("Successfully processed file: {FileName}", Path.GetFileName(file));
                        }
                        else
                        {
                            _logger.LogError("Failed to process file: {FileName}", Path.GetFileName(file));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing file: {FileName}", Path.GetFileName(file));
                    }
                }

                _logger.LogInformation("Advanced stored procedure migration completed. Processed: {ProcessedFiles}/{TotalFiles} files", 
                    processedFiles, totalFiles);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running advanced stored procedure migrations");
                return false;
            }
        }

        public async Task<bool> ProcessStoredProcedureFileAsync(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                var storedProcedures = await ExtractStoredProceduresFromScriptAsync(content);

                _logger.LogInformation("Found {Count} stored procedures in file: {FileName}", 
                    storedProcedures.Count, Path.GetFileName(filePath));

                foreach (var sp in storedProcedures)
                {
                    try
                    {
                        // For complex migration scripts, skip the existence check
                        if (sp.Name != "ComplexMigrationScript" && await CheckStoredProcedureExistsAsync(sp.Name))
                        {
                            _logger.LogInformation("Stored procedure {SPName} already exists, skipping", sp.Name);
                            continue;
                        }

                        if (await CreateStoredProcedureAsync(sp.Name, sp.Script))
                        {
                            await MarkSPMigrationAsAppliedAsync(sp.Name, "Success", Path.GetFileName(filePath));
                            _logger.LogInformation("Successfully created stored procedure: {SPName}", sp.Name);
                        }
                        else
                        {
                            await MarkSPMigrationAsAppliedAsync(sp.Name, "Failed", "Creation failed", Path.GetFileName(filePath));
                            _logger.LogError("Failed to create stored procedure: {SPName}", sp.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        await MarkSPMigrationAsAppliedAsync(sp.Name, "Failed", ex.Message, Path.GetFileName(filePath));
                        _logger.LogError(ex, "Error creating stored procedure: {SPName}", sp.Name);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stored procedure file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<List<StoredProcedureInfo>> ExtractStoredProceduresFromScriptAsync(string script)
        {
            var storedProcedures = new List<StoredProcedureInfo>();

            try
            {
                // For this specific file, we need to handle the complex structure
                // The file contains DECLARE statements and complex logic
                // We'll process it as a single script instead of trying to extract individual SPs
                
                _logger.LogInformation("Processing complex SQL script with multiple stored procedures");
                
                // Check if this is a complex migration script (contains DECLARE statements)
                if (script.Contains("DECLARE @TotalSPs") || script.Contains("BEGIN TRY"))
                {
                    // This is a complex migration script, process it as whole
                    storedProcedures.Add(new StoredProcedureInfo
                    {
                        Name = "ComplexMigrationScript",
                        Script = script
                    });
                }
                else
                {
                    // Split script by GO statements for simple scripts
                    var scriptParts = script.Split(new[] { "GO\r\n", "GO\n", "GO " }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var part in scriptParts)
                    {
                        var trimmedPart = part.Trim();
                        if (string.IsNullOrEmpty(trimmedPart))
                            continue;

                        // Check if this part contains a stored procedure
                        if (IsStoredProcedureScript(trimmedPart))
                        {
                            var spName = ExtractSPNameFromScript(trimmedPart);
                            if (!string.IsNullOrEmpty(spName) && spName != "Unknown_SP")
                            {
                                storedProcedures.Add(new StoredProcedureInfo
                                {
                                    Name = spName,
                                    Script = trimmedPart
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting stored procedures from script");
            }

            return storedProcedures;
        }

        private bool IsStoredProcedureScript(string script)
        {
            var upperScript = script.ToUpper();
            return upperScript.Contains("CREATE PROCEDURE") || 
                   upperScript.Contains("CREATE PROC") ||
                   upperScript.Contains("ALTER PROCEDURE") ||
                   upperScript.Contains("ALTER PROC");
        }

        private async Task<bool> CheckStoredProcedureExistsAsync(string spName)
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

        private async Task<bool> CreateStoredProcedureAsync(string spName, string spScript)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // For complex migration scripts, we need to handle them differently
                if (spName == "ComplexMigrationScript")
                {
                    _logger.LogInformation("Executing complex migration script");
                    
                    // Execute the entire script as a single batch
                    // This is necessary because the script contains DECLARE statements
                    // that need to be in the same batch as the stored procedures
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        // Execute the entire script as one batch
                        await connection.ExecuteAsync(spScript, transaction: transaction, commandTimeout: _settings.CommandTimeout);
                        
                        transaction.Commit();
                        _logger.LogInformation("Complex migration script executed successfully");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError(ex, "Error executing complex migration script: {ErrorMessage}", ex.Message);
                        
                        // Try alternative approach - execute without transaction
                        try
                        {
                            _logger.LogInformation("Trying alternative execution without transaction");
                            await connection.ExecuteAsync(spScript, commandTimeout: _settings.CommandTimeout);
                            _logger.LogInformation("Complex migration script executed successfully (alternative method)");
                            return true;
                        }
                        catch (Exception altEx)
                        {
                            _logger.LogError(altEx, "Alternative execution also failed: {ErrorMessage}", altEx.Message);
                            throw;
                        }
                    }
                }
                else
                {
                    // For individual stored procedures
                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        await connection.ExecuteAsync(spScript, transaction: transaction, commandTimeout: _settings.CommandTimeout);
                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stored procedure {SPName}", spName);
                return false;
            }
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
                            [FileName] [nvarchar](255) NULL,
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

        private async Task MarkSPMigrationAsAppliedAsync(string spName, string status, string errorMessage = null, string fileName = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check if FileName column exists
                var columnExistsQuery = @"
                    SELECT COUNT(*) 
                    FROM sys.columns 
                    WHERE object_id = OBJECT_ID(N'[dbo].[SP_Migration_Log]') AND name = 'FileName'";

                var columnExists = await connection.QuerySingleAsync<int>(columnExistsQuery);

                string insertScript;
                if (columnExists > 0)
                {
                    // FileName column exists
                    insertScript = @"
                        INSERT INTO SP_Migration_Log (SPName, Status, ErrorMessage, CreatedAt, Module, FileName)
                        VALUES (@SPName, @Status, @ErrorMessage, GETDATE(), 'AdvancedMigration', @FileName)";
                }
                else
                {
                    // FileName column doesn't exist, use old structure
                    insertScript = @"
                        INSERT INTO SP_Migration_Log (SPName, Status, ErrorMessage, CreatedAt, Module)
                        VALUES (@SPName, @Status, @ErrorMessage, GETDATE(), 'AdvancedMigration')";
                }

                await connection.ExecuteAsync(insertScript, new { 
                    SPName = spName, 
                    Status = status, 
                    ErrorMessage = errorMessage,
                    FileName = fileName
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
                var lines = script.Split('\n');
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.ToUpper().StartsWith("CREATE PROCEDURE") || 
                        trimmedLine.ToUpper().StartsWith("CREATE PROC") ||
                        trimmedLine.ToUpper().StartsWith("ALTER PROCEDURE") ||
                        trimmedLine.ToUpper().StartsWith("ALTER PROC"))
                    {
                        var parts = trimmedLine.Split(' ');
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

    public class StoredProcedureInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Script { get; set; } = string.Empty;
    }
}

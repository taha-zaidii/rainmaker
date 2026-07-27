using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text;
using Dapper;

namespace Digi.Shared.Services
{
    public interface IDirectSQLExecutionService
    {
        Task<bool> ExecuteSQLFileAsync(string filePath);
        Task<bool> ExecuteSQLScriptAsync(string script);
    }

    public class DirectSQLExecutionService : IDirectSQLExecutionService
    {
        private readonly string _connectionString;
        private readonly ILogger<DirectSQLExecutionService> _logger;
        private readonly DatabaseMigrationSettings _settings;

        public DirectSQLExecutionService(IConfiguration configuration, ILogger<DirectSQLExecutionService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _settings = DatabaseMigrationConfiguration.GetMigrationSettings(configuration);
        }

        public async Task<bool> ExecuteSQLFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation("Executing SQL file: {FilePath}", filePath);
                
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                return await ExecuteSQLScriptAsync(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL file: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> ExecuteSQLScriptAsync(string script)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                _logger.LogInformation("Executing SQL script directly");

                // Split script by GO statements and execute each part separately
                var scriptParts = script.Split(new[] { "GO\r\n", "GO\n", "GO " }, StringSplitOptions.RemoveEmptyEntries);
                
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
                        
                        await connection.ExecuteAsync(trimmedPart, commandTimeout: 300);
                    }
                }
                
                _logger.LogInformation("SQL script executed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing SQL script: {ErrorMessage}", ex.Message);
                return false;
            }
        }
    }
}

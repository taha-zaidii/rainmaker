using Dapper;
using Digi.Shared.DTOs;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Digi.Shared.SharedLibrary.Services
{
    public class DapperService : IDapperService
    {
        private readonly string _connectionString;
        private readonly ILogger<DapperService> _logger;
        private readonly int _commandTimeout;

        public DapperService(IConfiguration configuration, ILogger<DapperService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _commandTimeout = configuration.GetValue<int?>("Database:CommandTimeout") ?? 30;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: commandType,
                    commandTimeout: _commandTimeout
                );
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in QueryAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Database operation failed", sqlEx);
            }
            
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: commandType,
                    commandTimeout: _commandTimeout
                );
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in QueryFirstOrDefaultAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Database operation failed", sqlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QueryFirstOrDefaultAsync for {StoredProcedure}", storedProcedure);
                throw;
            }
        }

        public async Task<int> ExecuteAsync(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.ExecuteAsync(
                    storedProcedure,
                    parameters,
                    commandType: commandType,
                    commandTimeout: _commandTimeout
                );
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in ExecuteAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Database operation failed", sqlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteAsync for {StoredProcedure}", storedProcedure);
                throw new Exception(ex.Message);
            }
        }
        public async Task<DataAccessResult> ExecuteAsyncx(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                //await connection.OpenAsync();

                // Add output parameter to capture return value
                var dynamicParameters = new DynamicParameters(parameters);
                dynamicParameters.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                await connection.ExecuteAsync(
                    storedProcedure,
                    dynamicParameters,
                    transaction: transaction,
                    commandType: commandType,
                    commandTimeout: _commandTimeout
                );
               
                // Get the return value from the stored procedure
                var returnValue = dynamicParameters.Get<int>("RETURN_VALUE");
                await transaction.CommitAsync();
                return new DataAccessResult
                {
                    Success = returnValue >= 0,
                    AffectedRows = returnValue > 0 ? returnValue : 0,
                    ErrorCode = returnValue < 0 ? returnValue : 0
                };
            }
            catch (SqlException sqlEx) when (sqlEx.Number >= 50000 && sqlEx.Number <= 50099)
            {
                await transaction.RollbackAsync();
                // Handle user-defined errors (50000-50099 range)
                _logger.LogWarning(sqlEx, "Business rule violation in {StoredProcedure}", storedProcedure);
                return new DataAccessResult
                {
                    Success = false,
                    ErrorMessage = sqlEx.Message,
                    ErrorCode = sqlEx.Number
                };
            }
            catch (SqlException sqlEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(sqlEx, "SQL Error in ExecuteAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Database operation failed", sqlEx);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in ExecuteAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException(ex.Message, ex);
            }
        }
        

        public async Task<T> ExecuteScalarAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: commandType,
                    commandTimeout: _commandTimeout
                );
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in ExecuteScalarAsync for {StoredProcedure}", storedProcedure);
                if (sqlEx.Number == 50000) // Your custom exception number
                {
                    throw new DataAccessException(sqlEx.Message);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExecuteScalarAsync for {StoredProcedure}", storedProcedure);
                throw;
            }
        }

        public async Task<SqlMapper.GridReader> QueryMultipleAsync(string storedProcedure, object parameters = null)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.QueryMultipleAsync(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout
                );
            }
            catch (SqlException sqlEx)
            {
                connection.Dispose();
                _logger.LogError(sqlEx, "SQL Error in QueryMultipleAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Database operation failed", sqlEx);
            }
            catch (Exception ex)
            {
                connection.Dispose();
                _logger.LogError(ex, "Error in QueryMultipleAsync for {StoredProcedure}", storedProcedure);
                throw;
            }
        }

        public async Task<T> InsertAndReturnIdAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();

                // For SQL Server, we use OUTPUT INSERTED to get the generated ID
                var result = await connection.QuerySingleAsync<T>(
                    storedProcedure,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout
                );

                return result;
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in InsertAndReturnIdAsync for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Insert operation failed", sqlEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InsertAndReturnIdAsync for {StoredProcedure}", storedProcedure);
                throw;
            }
        }
    }

    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }
        public DataAccessException(string message, Exception innerException) : base(message, innerException) { }
    }
    
}

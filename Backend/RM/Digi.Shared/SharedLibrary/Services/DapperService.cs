using Dapper;
using Digi.Shared.DTOs;
using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Digi.Shared.SharedLibrary.Services
{
    /// <summary>
    /// Unified Dapper service — implements both the raw Dapper passthrough and the generic CRUD
    /// wrapper formerly split across DapperService / DapperServices (plural). Register once as
    /// IDapperService; the plural pair has been removed.
    /// </summary>
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

        // ── Raw Dapper passthrough ─────────────────────────────────────────────

        public async Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<T>(storedProcedure, parameters,
                    commandType: commandType, commandTimeout: _commandTimeout);
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
                return await connection.QueryFirstOrDefaultAsync<T>(storedProcedure, parameters,
                    commandType: commandType, commandTimeout: _commandTimeout);
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
                return await connection.ExecuteAsync(storedProcedure, parameters,
                    commandType: commandType, commandTimeout: _commandTimeout);
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
                var dynamicParameters = new DynamicParameters(parameters);
                dynamicParameters.Add("RETURN_VALUE", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                await connection.ExecuteAsync(storedProcedure, dynamicParameters,
                    transaction: transaction, commandType: commandType, commandTimeout: _commandTimeout);

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
                _logger.LogWarning(sqlEx, "Business rule violation in {StoredProcedure}", storedProcedure);
                return new DataAccessResult { Success = false, ErrorMessage = sqlEx.Message, ErrorCode = sqlEx.Number };
            }
            catch (SqlException sqlEx)
            {
                await transaction.RollbackAsync();
                _logger.LogError(sqlEx, "SQL Error in ExecuteAsyncx for {StoredProcedure}", storedProcedure);
                throw new DataAccessException("Database operation failed", sqlEx);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in ExecuteAsyncx for {StoredProcedure}", storedProcedure);
                throw new DataAccessException(ex.Message, ex);
            }
        }

        public async Task<T> ExecuteScalarAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            await using var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<T>(storedProcedure, parameters,
                    commandType: commandType, commandTimeout: _commandTimeout);
            }
            catch (SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "SQL Error in ExecuteScalarAsync for {StoredProcedure}", storedProcedure);
                if (sqlEx.Number == 50000)
                    throw new DataAccessException(sqlEx.Message);
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
                return await connection.QueryMultipleAsync(storedProcedure, parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: _commandTimeout);
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
                return await connection.QuerySingleAsync<T>(storedProcedure, parameters,
                    commandType: CommandType.StoredProcedure, commandTimeout: _commandTimeout);
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

        // ── Generic CRUD wrapper (consolidated from former DapperServices) ─────

        public async Task<DbOperationResult<T>> QuerySingleAsync<T>(string spExecute, string tableName, string whereClause = null,
            string columns = "*", bool? isActiveFilter = null)
        {
            var result = new DbOperationResult<T>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@TableName", tableName);
                parameters.Add("@Operation", "Select");
                parameters.Add("@Columns", columns);
                parameters.Add("@WhereClause", whereClause);
                parameters.Add("@IsActiveFilter", isActiveFilter);
                parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                var data = await connection.QueryFirstOrDefaultAsync<T>(spExecute, parameters, commandType: CommandType.StoredProcedure);
                var returnCode = parameters.Get<int>("@ReturnCode");
                result.IsSuccess = returnCode >= 0;
                result.ReturnCode = returnCode;
                result.Data = data;
                result.Message = data != null ? "Record retrieved successfully" : "No matching record found";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = "Query failed";
                result.Exception = ex;
                _logger.LogError(ex, "Error in QuerySingleAsync for table {TableName}", tableName);
            }
            return result;
        }

        public async Task<DbOperationResult<IEnumerable<T>>> QueryListAsync<T>(string spExecute, string tableName, string whereClause = null,
            string columns = "*", bool? isActiveFilter = null)
        {
            var result = new DbOperationResult<IEnumerable<T>>();
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@TableName", tableName);
                parameters.Add("@Operation", DbOperationType.Select.ToProcedureName());
                parameters.Add("@Columns", columns);
                parameters.Add("@WhereClause", whereClause);
                parameters.Add("@IsActiveFilter", isActiveFilter);
                parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                var data = await connection.QueryAsync<T>(spExecute, parameters, commandType: CommandType.StoredProcedure);
                var returnCode = parameters.Get<int>("@ReturnCode");

                if (data == null || !data.Any())
                {
                    result.IsSuccess = false;
                    result.ReturnCode = returnCode;
                    result.Data = null;
                    result.Message = "No records found";
                }
                else
                {
                    result.IsSuccess = returnCode >= 0;
                    result.ReturnCode = returnCode;
                    result.Data = data;
                    result.Message = "Records retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Query failed - {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error in QueryListAsync for table {TableName}", tableName);
            }
            return result;
        }

        public async Task<DbOperationResult> ExecuteGenericCrudAsync(
            string spExecute,
            string tableName,
            string operation,
            int? userId = null,
            string ipAddress = null,
            string primaryKeyColumn = null,
            string columns = null,
            string values = null,
            string setClause = null,
            string whereClause = null,
            bool? isActiveFilter = null,
            bool? isActive = null,
            bool? isDeleted = null,
            string updatedBy = null)
        {
            var result = new DbOperationResult();
            SqlConnection connection = null;
            SqlTransaction transaction = null;
            try
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                var parameters = new DynamicParameters();
                parameters.Add("@PrimaryKeyColumn", primaryKeyColumn);
                parameters.Add("@IPaddress", ipAddress);
                parameters.Add("@UserID", userId);
                parameters.Add("@TableName", tableName);
                parameters.Add("@Operation", operation);
                parameters.Add("@Columns", columns);
                parameters.Add("@Values", values);
                parameters.Add("@SetClause", setClause);
                parameters.Add("@WhereClause", whereClause);
                parameters.Add("@IsActiveFilter", isActiveFilter);
                parameters.Add("@IsActive", isActive);
                parameters.Add("@IsDeleted", isDeleted);
                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                var affectedRows = await connection.ExecuteAsync(spExecute, parameters,
                    transaction: transaction, commandType: CommandType.StoredProcedure, commandTimeout: _commandTimeout);
                var returnCode = parameters.Get<int>("@ReturnCode");
                result.ReturnCode = returnCode;
                result.AffectedRows = affectedRows;

                if (returnCode >= 0)
                {
                    result.IsSuccess = true;
                    result.Message = GetCrudSuccessMessage(operation, affectedRows);
                    await transaction.CommitAsync();
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = GetCrudErrorMessage(returnCode);
                    await transaction.RollbackAsync();
                }
            }
            catch (SqlException sqlEx)
            {
                await TryRollbackAsync(transaction);
                result.IsSuccess = false;
                result.Exception = sqlEx;
                result.Message = "Database operation failed";
                result.ReturnCode = sqlEx.Number;
                _logger.LogError(sqlEx, "SQL Error in ExecuteGenericCrudAsync. Table: {Table}, Op: {Op}", tableName, operation);
            }
            catch (Exception ex)
            {
                await TryRollbackAsync(transaction);
                result.IsSuccess = false;
                result.Exception = ex;
                result.Message = "Unexpected error occurred";
                result.ReturnCode = -999;
                _logger.LogError(ex, "Error in ExecuteGenericCrudAsync. Table: {Table}, Op: {Op}", tableName, operation);
            }
            finally
            {
                await TryCloseConnectionAsync(connection);
            }
            return result;
        }

        /// <summary>Transactional SP execute returning DbOperationResult (replaces the old DapperServices.ExecuteAsync).</summary>
        public async Task<DbOperationResult> ExecuteCrudAsync(string spExecute, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            var result = new DbOperationResult();
            SqlConnection connection = null;
            SqlTransaction transaction = null;
            try
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                var data = await connection.QuerySingleOrDefaultAsync<int>(spExecute, parameters,
                    transaction: transaction, commandType: CommandType.StoredProcedure, commandTimeout: _commandTimeout);

                if (data == 1)
                {
                    result.IsSuccess = true;
                    result.Message = "Operation completed successfully";
                    await transaction.CommitAsync();
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "Operation returned no rows";
                    await transaction.RollbackAsync();
                }
            }
            catch (SqlException sqlEx)
            {
                await TryRollbackAsync(transaction);
                result.IsSuccess = false;
                result.Exception = sqlEx;
                result.Message = "Database operation failed";
                result.ReturnCode = sqlEx.Number;
                _logger.LogError(sqlEx, "SQL Error in ExecuteCrudAsync for {SP}", spExecute);
            }
            catch (Exception ex)
            {
                await TryRollbackAsync(transaction);
                result.IsSuccess = false;
                result.Exception = ex;
                result.Message = "Unexpected error occurred";
                result.ReturnCode = -999;
                _logger.LogError(ex, "Error in ExecuteCrudAsync for {SP}", spExecute);
            }
            finally
            {
                await TryCloseConnectionAsync(connection);
            }
            return result;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private async Task TryRollbackAsync(SqlTransaction transaction)
        {
            try { if (transaction?.Connection != null) await transaction.RollbackAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to rollback transaction"); }
        }

        private async Task TryCloseConnectionAsync(SqlConnection connection)
        {
            try { if (connection != null && connection.State != ConnectionState.Closed) await connection.CloseAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to close database connection"); }
        }

        private static string GetCrudSuccessMessage(string operation, int affectedRows) =>
            operation.ToUpper() switch
            {
                "SELECT"    => "Query executed successfully",
                "SELECTID"  => "Record retrieved successfully",
                "INSERT"    => affectedRows > 0 ? "Record created successfully" : "No records created",
                "UPDATE"    => affectedRows > 0 ? "Record updated successfully" : "No records updated",
                "STATUS"    => affectedRows > 0 ? "Status updated successfully" : "No status changes made",
                "ISDELETED" => affectedRows > 0 ? "Deletion status updated" : "No deletion status changes",
                _           => "Operation completed"
            };

        private static string GetCrudErrorMessage(int errorCode) =>
            errorCode switch
            {
                -1    => "Access to the specified table is not allowed",
                -2    => "SetClause and WhereClause are required for UPDATE",
                -3    => "No records were updated. Check your WHERE clause",
                -4    => "Status operation requires a WHERE clause",
                -5    => "IsActive parameter is required",
                -6    => "Cannot update status — record is deleted",
                -7    => "Invalid operation specified",
                50000 => "Access denied to table",
                50001 => "Invalid parameters for UPDATE operation",
                50002 => "No records matched the criteria",
                50003 => "Missing WHERE clause for Status operation",
                50004 => "IsActive parameter is required",
                50005 => "Cannot update deleted records",
                50006 => "Invalid operation specified",
                _     => "Database operation failed"
            };
    }

    public class DataAccessException : Exception
    {
        public DataAccessException(string message) : base(message) { }
        public DataAccessException(string message, Exception innerException) : base(message, innerException) { }
    }
}

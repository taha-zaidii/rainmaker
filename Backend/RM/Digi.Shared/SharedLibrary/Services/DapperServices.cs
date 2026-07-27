using Dapper;
using Digi.Shared.DTOs;
using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.SharedLibrary.Services
{
    public class DapperServices : IDapperServices
    {
        private readonly string _connectionString;
        private readonly ILogger<DapperService> _logger;
        private readonly int _commandTimeout;

        public DapperServices(IConfiguration configuration, ILogger<DapperService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _commandTimeout = configuration.GetValue<int>("Database:CommandTimeout", 30);
        }


        public async Task<DbOperationResult<T>> QuerySingleAsync<T>(string spExecute, string tableName, string whereClause = null,
        string columns = "*", bool? isActiveFilter = null)
        {
            var result = new DbOperationResult<T>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@TableName", tableName);
                    parameters.Add("@Operation", "Select");
                    parameters.Add("@Columns", columns);
                    parameters.Add("@WhereClause", whereClause);
                    parameters.Add("@IsActiveFilter", isActiveFilter);
                    parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                    var data = await connection.QueryFirstOrDefaultAsync<T>(
                        spExecute,
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    var returnCode = parameters.Get<int>("@ReturnCode");

                    result.IsSuccess = returnCode >= 0;
                    result.ReturnCode = returnCode;
                    result.Data = data;
                    result.Message = data != null ? "Record retrieved successfully" : "No matching record found";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = "Query failed";
                result.Exception = ex;
                _logger.LogError(ex, "Error executing query for table {TableName}", tableName);
            }

            return result;
        }

        public async Task<DbOperationResult<IEnumerable<T>>> QueryListAsync<T>(string spExecute, string tableName, string whereClause = null,
            string columns = "*", bool? isActiveFilter = null)
        {
            var result = new DbOperationResult<IEnumerable<T>>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@TableName", tableName);
                    parameters.Add("@Operation", DbOperationType.Select.ToProcedureName());
                    parameters.Add("@Columns", columns);
                    parameters.Add("@WhereClause", whereClause);
                    parameters.Add("@IsActiveFilter", isActiveFilter);
                    parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                    var data = await connection.QueryAsync<T>(
                        spExecute,
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    if (!data.Any() || data == null)
                    {                        
                        var returnCode = parameters.Get<int>("@ReturnCode");

                        result.IsSuccess = false;
                        result.ReturnCode = returnCode;
                        result.Data = null;
                        result.Message = "Record not founds";
                    }
                    else
                    {                        
                        var returnCode = parameters.Get<int>("@ReturnCode");
                        result.IsSuccess = returnCode >= 0;
                        result.ReturnCode = returnCode;
                        result.Data = data;
                        result.Message = "Records retrieved successfully";
                    }

                        
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Query failed - {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error executing query for table {TableName}", tableName);
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

                // Begin transaction with appropriate isolation level
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

                // Execute stored procedure
                var affectedRows = await connection.ExecuteAsync(
                    spExecute,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                // Get the return code from the stored procedure
                var returnCode = parameters.Get<int>("@ReturnCode");

                // Handle different return codes
                result.ReturnCode = returnCode;
                result.AffectedRows = affectedRows;

                if (returnCode >= 0)
                {
                    result.IsSuccess = true;
                    result.Message = GetSuccessMessage(operation, affectedRows);
                    await transaction.CommitAsync();
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = GetErrorMessage(returnCode);
                    await transaction.RollbackAsync();
                }
            }
            catch (SqlException sqlEx)
            {
                await TryRollback(transaction);
                result.IsSuccess = false;
                result.Exception = sqlEx;
                result.Message = "Database operation failed";
                result.ReturnCode = sqlEx.Number;

                _logger.LogError(sqlEx, "SQL Error executing generic CRUD operation. Table: {Table}, Operation: {Operation}",
                    tableName, operation);
            }
            catch (Exception ex)
            {
                await TryRollback(transaction);
                result.IsSuccess = false;
                result.Exception = ex;
                result.Message = "Unexpected error occurred";
                result.ReturnCode = -999;

                _logger.LogError(ex, "Unexpected error executing generic CRUD operation. Table: {Table}, Operation: {Operation}",
                    tableName, operation);
            }
            finally
            {
                await TryCloseConnection(connection);
            }

            return result;
        }

        public async Task<DbOperationResult> ExecuteAsync(string spExecute, object parameters = null, CommandType commandType = CommandType.StoredProcedure)
        {
            var result = new DbOperationResult();
            SqlConnection connection = null;
            SqlTransaction transaction = null;

            try
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                var data = await connection.QuerySingleOrDefaultAsync<int>(
                    spExecute,
                    parameters,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                if (data == 1)
                {
                    result.IsSuccess = true;
                    result.Message = GetSuccessMessage("Insert", data);
                    await transaction.CommitAsync();
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = GetErrorMessage(1);
                    await transaction.RollbackAsync();
                }
            }
            catch (SqlException sqlEx)
            {
                await TryRollback(transaction);
                result.IsSuccess = false;
                result.Exception = sqlEx;
                result.Message = "Database operation failed";
                result.ReturnCode = sqlEx.Number;

                _logger.LogError(sqlEx, "SQL Error executing.");
            }
            catch (Exception ex)
            {
                await TryRollback(transaction);
                result.IsSuccess = false;
                result.Exception = ex;
                result.Message = "Unexpected error occurred";
                result.ReturnCode = -999;

                _logger.LogError(ex, "Unexpected error executing.");
            }
            finally
            {
                await TryCloseConnection(connection);
            }

            return result;
        }


        private async Task TryRollback(SqlTransaction transaction)
        {
            try
            {
                if (transaction != null && transaction.Connection != null)
                {
                    await transaction.RollbackAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rollback transaction");
            }
        }

        private async Task TryCloseConnection(SqlConnection connection)
        {
            try
            {
                if (connection != null && connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to close database connection");
            }
        }

        private string GetSuccessMessage(string operation, int affectedRows)
        {
            return operation.ToUpper() switch
            {
                "SELECT" => "Query executed successfully",
                "SELECTID" => "Record retrieved successfully",
                "INSERT" => affectedRows > 0 ? "Record created successfully" : "No records created",
                "UPDATE" => affectedRows > 0 ? "Record updated successfully" : "No records updated",
                "STATUS" => affectedRows > 0 ? "Status updated successfully" : "No status changes made",
                "ISDELETED" => affectedRows > 0 ? "Deletion status updated" : "No deletion status changes",
                _ => "Operation completed"
            };
        }

        private string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                -1 => "Access to the specified table is not allowed",
                -2 => "SetClause and WhereClause are required for UPDATE",
                -3 => "No records were updated. Check your WHERE clause",
                -4 => "Status operation requires a WHERE clause",
                -5 => "IsActive parameter is required",
                -6 => "Cannot update status - record is deleted",
                -7 => "Invalid operation specified",
                50000 => "Access denied to table",
                50001 => "Invalid parameters for UPDATE operation",
                50002 => "No records matched the criteria",
                50003 => "Missing WHERE clause for Status operation",
                50004 => "IsActive parameter is required",
                50005 => "Cannot update deleted records",
                50006 => "Invalid operation specified",
                _ => "Database operation failed"
            };
        }
    }
}

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
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Digi.Shared.SharedLibrary.Services
{
    public class PackageDapperService : IPackageDapperService
    {
        private readonly string _connectionString;
        private readonly ILogger<PackageDapperService> _logger;
        private readonly int _commandTimeout;
        private readonly IDbConnection _dbConnection;
        public PackageDapperService(IConfiguration configuration, IDbConnection dbConnection, ILogger<PackageDapperService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("DefaultConnection string is missing in configuration");
            _logger = logger;
            _commandTimeout = configuration.GetValue<int?>("Database:CommandTimeout") ?? 30;
            _dbConnection = dbConnection;
        }

        public async Task<DbOperationResult<T>> QueryFirstOrDefaultAsync<T>(string sql, object param = null, CommandType? commandType = null)
        {
            var result = new DbOperationResult<T>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var data = await connection.QueryFirstOrDefaultAsync<T>(
                        sql,
                        param,
                        commandTimeout: _commandTimeout,
                        commandType: commandType ?? CommandType.StoredProcedure);

                    if (data == null)
                    {
                        result.IsSuccess = false;
                        result.Message = "Record not found";
                    }
                    else
                    {
                        result.IsSuccess = true;
                        result.Data = data;
                        result.Message = "Record retrieved successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Query failed - {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error executing query: {sql}");
            }

            return result;
        }

        public async Task<DbOperationResult<IEnumerable<T>>> QueryAsync<T>(string sql, object param = null, CommandType? commandType = null)
        {
            var result = new DbOperationResult<IEnumerable<T>>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var data = await connection.QueryAsync<T>(
                        sql,
                        param,
                        commandTimeout: _commandTimeout,
                        commandType: commandType ?? CommandType.StoredProcedure);

                    result.IsSuccess = true;
                    result.Data = data;
                    result.Message = "Records retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Query failed - {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error executing query: {sql}");
            }

            return result;
        }

        public async Task<DbOperationResult<int>> ExecuteAsync(string sql, object param = null, CommandType? commandType = null)
        {
            var result = new DbOperationResult<int>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var affectedRows = await connection.ExecuteAsync(
                        sql,
                        param,
                        commandTimeout: _commandTimeout,
                        commandType: commandType ?? CommandType.StoredProcedure);

                    result.IsSuccess = true;
                    result.Data = affectedRows;
                    result.Message = "Command executed successfully";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Execution failed - {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error executing command: {sql}");
            }

            return result;
        }

        //public async Task<DbOperationResult<SqlMapper.GridReader>> QueryMultipleAsync(string sql, object param = null, CommandType? commandType = null)
        //{
        //    var result = new DbOperationResult<SqlMapper.GridReader>();
        //    SqlConnection connection = null;

        //    try
        //    {
        //        connection = new SqlConnection(_connectionString);
        //        await connection.OpenAsync();

        //        var gridReader = await connection.QueryMultipleAsync(
        //            sql,
        //            param,
        //            commandTimeout: _commandTimeout,
        //            commandType: commandType ?? CommandType.StoredProcedure);

        //        result.IsSuccess = true;
        //        result.Data = gridReader;
        //        result.Message = "Multiple queries executed successfully";
        //    }
        //    catch (Exception ex)
        //    {
        //        result.IsSuccess = false;
        //        result.Message = $"Multiple query execution failed - {ex.Message}";
        //        result.Exception = ex;
        //        _logger.LogError(ex, "Error executing multiple queries: {sql}");

        //        connection?.Dispose();
        //        throw;
        //    }

        //    return result;
        //}

        public async Task<SqlMapper.GridReader> QueryMultipleAsync(
        string sql,
        object param = null,
        CommandType? commandType = null)
        {
            try
            {
                if (_dbConnection.State != ConnectionState.Open)
                {
                    // With this line:
                    if (_dbConnection is DbConnection dbConnection)
                    {
                        await dbConnection.OpenAsync();
                    }
                    else
                    {
                        throw new InvalidOperationException("The provided IDbConnection does not support asynchronous operations.");
                    }
                }

                return await _dbConnection.QueryMultipleAsync(
                    sql,
                    param,
                    commandType: commandType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing QueryMultiple for {Sql}", sql);
                throw; // Re-throw to let calling method handle
            }
        }

        
        public async Task<DbOperationResult<T>> ExecuteWithReturnAsync<T>(string procedureName, object parameters = null)
        {
            var result = new DbOperationResult<T>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var output = await connection.ExecuteScalarAsync<T>(
                        procedureName,
                        parameters,
                        commandTimeout: _commandTimeout,
                        commandType: CommandType.StoredProcedure);

                    result.IsSuccess = true;
                    result.Data = output;
                    result.Message = "Command executed with return value successfully";
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Execution with return failed - {ex.Message}";
                result.Exception = ex;
                _logger.LogError(ex, "Error executing with return: {procedureName}");
            }

            return result;
        }

        public int Execute(string sql, DynamicParameters parameters, CommandType commandType = CommandType.StoredProcedure)
        {
            try
            {

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.OpenAsync();
                    return connection.Execute(sql, parameters, commandType: commandType);
                }
            }
            catch (Exception ex)
            {                
                _logger.LogError(ex, "Error executing with return: {procedureName}");
            }

            return 0;
                      
        }
    }
}
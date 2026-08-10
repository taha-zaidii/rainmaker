using Dapper;
using Digi.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Digi.Shared.SharedLibrary.Services.DapperService;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    /// <summary>
    /// Unified Dapper abstraction. Consolidates the former IDapperServices (generic CRUD wrapper)
    /// into a single injection token. Register DapperService as this interface; the plural
    /// IDapperServices / DapperServices pair has been removed.
    /// </summary>
    public interface IDapperService
    {
        // ── Raw Dapper passthrough ────────────────────────────────────────────
        Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> QueryFirstOrDefaultAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<int> ExecuteAsync(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<DataAccessResult> ExecuteAsyncx(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> ExecuteScalarAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<SqlMapper.GridReader> QueryMultipleAsync(string storedProcedure, object parameters = null);
        Task<T> InsertAndReturnIdAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);

        // ── Generic CRUD wrapper (consolidated from former IDapperServices) ───
        Task<DbOperationResult> ExecuteCrudAsync(string spExecute, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<DbOperationResult> ExecuteGenericCrudAsync(
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
            string updatedBy = null);
        Task<DbOperationResult<T>> QuerySingleAsync<T>(string spExecute, string tableName, string whereClause = null,
            string columns = "*", bool? isActiveFilter = null);
        Task<DbOperationResult<IEnumerable<T>>> QueryListAsync<T>(string spExecute, string tableName, string whereClause = null,
            string columns = "*", bool? isActiveFilter = null);
    }

}

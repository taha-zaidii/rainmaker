using Dapper;
using Digi.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IDapperServices
    {
        Task<DbOperationResult> ExecuteAsync(string spExecute, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
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

        Task<DbOperationResult<IEnumerable<T>>> QueryListAsync<T>(string spExecute,  string tableName, string whereClause = null,
            string columns = "*", bool? isActiveFilter = null);

    }
}

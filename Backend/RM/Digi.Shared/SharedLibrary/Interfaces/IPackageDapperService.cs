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
    public interface IPackageDapperService
    {
        Task<DbOperationResult<T>> QueryFirstOrDefaultAsync<T>(string sql, object param = null, CommandType? commandType = null);
        Task<DbOperationResult<IEnumerable<T>>> QueryAsync<T>(string sql, object param = null, CommandType? commandType = null);
        Task<DbOperationResult<int>> ExecuteAsync(string sql, object param = null, CommandType? commandType = null);

        int Execute(string sql, DynamicParameters parameters, CommandType commandType);

        //Task<DbOperationResult<SqlMapper.GridReader>> QueryMultipleAsync(string sql, object param = null, CommandType? commandType = null);

        Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object param = null, CommandType? commandType = null);

    }
}

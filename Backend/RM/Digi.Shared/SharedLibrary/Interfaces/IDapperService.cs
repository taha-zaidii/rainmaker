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
    public interface IDapperService
    {
        //IDbConnection CreateConnection();
        //Task<IEnumerable<T>> QueryAsync<T>(string sp, object param = null!);
        //Task<T> QueryFirstOrDefaultAsync<T>(string sp, object param = null!);
        //Task<int> ExecuteAsync(string sp, object param = null!);
        //Task ExecuteAsync(string sql, object parameters = null, CommandType commandType = CommandType.Text);
        //Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        //Task<T> QueryFirstOrDefaultAsync<T>(string query, object value, CommandType commandType);

        Task<IEnumerable<T>> QueryAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> QueryFirstOrDefaultAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<int> ExecuteAsync(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<DataAccessResult> ExecuteAsyncx(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<T> ExecuteScalarAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
        Task<SqlMapper.GridReader> QueryMultipleAsync(string storedProcedure, object parameters = null);
        Task<T> InsertAndReturnIdAsync<T>(string storedProcedure, object parameters = null, CommandType commandType = CommandType.StoredProcedure);
    }

}

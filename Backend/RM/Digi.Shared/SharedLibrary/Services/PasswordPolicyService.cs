using Dapper;
using Digi.Shared.DTOs.admin.module;
using Digi.Shared.SharedLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.SharedLibrary.Services
{
    public class PasswordPolicyService
    {
        private readonly IDapperService _dapper;
        public PasswordPolicyService(IDapperService dapper)
        {
             _dapper = dapper;   
        }
        public async Task<PasswordPolicyResult> ValidatePasswordPolicyAsync(int userId, string newPassword)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);
            parameters.Add("@NewPassword", newPassword);

            var result = await _dapper.QueryFirstOrDefaultAsync<PasswordPolicyResult>(
                "sp_ValidatePasswordAgainstPolicy",
                parameters,
                commandType: CommandType.StoredProcedure);

            return result;
        }

    }
}

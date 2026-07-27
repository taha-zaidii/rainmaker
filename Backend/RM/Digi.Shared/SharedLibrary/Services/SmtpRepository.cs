using Dapper;
using Digi.Shared.DTOs.admin.module;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Digi.Shared.SharedLibrary.Services
{
    public class SmtpRepository : ISmtpRepository
    {
        private readonly IDbConnection _db;

        public SmtpRepository(IConfiguration config)
        {
            _db = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        }

        public async Task<IEnumerable<SmtpResponseDto>> ExecuteAsync(SmtpRequestDto dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SMTPID", dto.SmtpId);
            parameters.Add("@CompanyID", dto.CompanyId);
            parameters.Add("@MailProtocol", dto.MailProtocol);
            parameters.Add("@MailEncryption", dto.MailEncryption);
            parameters.Add("@MailHost", dto.MailHost);
            parameters.Add("@MailPort", dto.MailPort);
            parameters.Add("@MailUserName", dto.MailUserName);
            parameters.Add("@MailPassword", dto.MailPassword);
            parameters.Add("@IsEnableSSL", dto.IsEnableSSL);
            parameters.Add("@CreatedBy", dto.CreatedBy);
            parameters.Add("@IsSuperAdmin", dto.IsSuperAdmin);
            parameters.Add("@Action", dto.Action);

            var result = await _db.QueryAsync<SmtpResponseDto>(
                "sp_Adm_SMTP_CRUD",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<SmtpResponseDto>> GetAllSuperAdminAsync()
        {
            var result = await _db.QueryAsync<SmtpResponseDto>(
                "sp_Adm_SMTP_GetAllSuperAdmin",
                commandType: CommandType.StoredProcedure
            );
            return result;
        }

        public async Task<SmtpResponseDto?> GetSmtpByCompanyIdAsync(int? companyId)
        {
            var parameters = new DynamicParameters();

            if (companyId.HasValue && companyId.Value > 0)
            {
                parameters.Add("@CompanyID", companyId.Value, DbType.Int32);
                parameters.Add("@IsSuperAdmin", 0, DbType.Int32);
            }
            else
            {
                parameters.Add("@CompanyID", null, DbType.Int32);
                parameters.Add("@IsSuperAdmin", 1, DbType.Int32);
            }

            parameters.Add("@Action", "getall", DbType.String);

            var result = await _db.QueryFirstOrDefaultAsync<SmtpResponseDto>(
                "sp_Adm_SMTP_CRUD",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
    }
}

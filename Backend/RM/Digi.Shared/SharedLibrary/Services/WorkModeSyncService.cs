using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Digi.Shared.SharedLibrary.Interfaces;

namespace Digi.Shared.SharedLibrary.Services
{
    public class WorkModeSyncService : IWorkModeSyncService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkModeSyncService> _logger;

        public WorkModeSyncService(IConfiguration configuration, ILogger<WorkModeSyncService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SyncWorkModeOnLoginAsync(int? companyId, int? employeeId)
        {
            if (!companyId.HasValue || companyId.Value <= 0 || !employeeId.HasValue || employeeId.Value <= 0)
                return;

            try
            {
                var cs = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(cs))
                    return;

                await using var conn = new SqlConnection(cs);

                await conn.ExecuteAsync(
                    "dbo.sp_Hr_WorkMode_RevertToOnsiteIfNoActiveWFH",
                    new { CompanyID = companyId.Value, EmployeeID = employeeId.Value },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "WorkMode sync failed for EmployeeID={EmployeeID}, CompanyID={CompanyID}",
                    employeeId, companyId);
            }
        }
    }
}
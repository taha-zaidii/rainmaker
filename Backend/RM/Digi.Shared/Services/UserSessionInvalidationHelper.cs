using Digi.Shared.SharedLibrary.Interfaces;
using System.Data;

namespace Digi.Shared.Services
{
    /// <summary>
    /// Revokes refresh tokens and deactivates registered devices after password reset/change (ERP session invalidation).
    /// </summary>
    public static class UserSessionInvalidationHelper
    {
        public const string DefaultRevokedBy = "password-change";

        public static async Task InvalidateAllSessionsAsync(
            IDapperService dapper,
            int userId,
            string? revokedBy = null,
            ISessionRevocationNotifier? notifier = null,
            int? companyId = null,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
                return;

            var actor = string.IsNullOrWhiteSpace(revokedBy) ? DefaultRevokedBy : revokedBy.Trim();

            if (notifier != null)
            {
                await notifier.NotifySessionsRevokedAsync(
                    userId,
                    companyId,
                    SessionRevocationConstants.ReasonPasswordChanged,
                    cancellationToken);
            }

            await dapper.ExecuteAsync(
                "sp_Global_RefreshToken_Revoke",
                new { UserID = userId, RevokedBy = actor },
                commandType: CommandType.StoredProcedure);

            await dapper.ExecuteAsync(
                @"UPDATE dbo.TblNotificationDeviceToken
                  SET IsActive = 0,
                      IsDeleted = 1,
                      UpdatedOn = SYSUTCDATETIME(),
                      UpdatedBy = @UpdatedBy
                  WHERE UserID = @UserID
                    AND IsDeleted = 0",
                new { UserID = userId, UpdatedBy = actor },
                commandType: CommandType.Text);
        }

        public static async Task<string?> GetSecurityStampAsync(
            IDapperService dapper,
            int userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
                return null;

            return await dapper.QueryFirstOrDefaultAsync<string>(
                @"SELECT SecurityStamp
                  FROM dbo.Tbl_Adm_Users
                  WHERE UserID = @UserId
                    AND ISNULL(IsDeleted, 0) = 0",
                new { UserId = userId },
                commandType: CommandType.Text);
        }
    }
}

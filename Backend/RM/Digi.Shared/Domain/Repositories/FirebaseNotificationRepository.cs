using Dapper;
using Digi.Shared.Domain.Repositories.Interfaces;
using Digi.Shared.DTOs;
using Digi.Shared.DTOs.notification;
using Digi.Shared.Helper;
using Digi.Shared.Services;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Digi.Shared.Module.Domain.Repositories
{
    public class FirebaseNotificationRepository : IFirebaseNotificationRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<FirebaseNotificationRepository> _logger;
        private readonly IDapperService _dapper;
        private readonly IConfiguration _configuration;

        public FirebaseNotificationRepository(
            string connectionString,
            ILogger<FirebaseNotificationRepository> logger,
            IDapperService dapper,
            IConfiguration configuration)
        {
            _connectionString = connectionString;
            _logger = logger;
            _dapper = dapper;
            _configuration = configuration;
        }

        public async Task<DbOperationResult<FirebaseDeviceTokenDto>> RegisterDeviceTokenAsync(RegisterDeviceTokenRequestDto request)
        {
            try
            {
                var identity = NotificationDeviceTokenRegistrationHelper.Resolve(request);

                var preCheck = await NotificationDeviceTokenRegistrationHelper.ValidateAgainstActiveDeviceAsync(
                    _dapper,
                    request.CompanyID,
                    request.UserID,
                    identity,
                    _configuration);

                if (!preCheck.IsAllowed)
                {
                    return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>(
                        preCheck.ErrorMessage ?? preCheck.ErrorCode ?? "Device registration blocked.",
                        returnCode: string.Equals(preCheck.ErrorCode, "ALREADY_LOGGED_IN_ELSEWHERE", StringComparison.OrdinalIgnoreCase) ? 409 : 400);
                }

                var spResult = await NotificationDeviceTokenRegistrationHelper.RegisterViaStoredProcedureAsync(
                    _dapper,
                    _configuration,
                    request.CompanyID,
                    request.UserID,
                    identity,
                    request.CreatedBy);

                return NotificationDeviceTokenRegistrationHelper.MapSpResultToDeviceDto(spResult, identity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device token for UserID: {UserID}", request.UserID);
                return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>(ex.Message, exception: ex);
            }
        }

        public async Task<DbOperationResult<List<FirebaseDeviceTokenDto>>> GetUserDeviceTokensAsync(int companyID, int userID)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var tokens = (await connection.QueryAsync<FirebaseDeviceTokenDto>(
                    @"SELECT * FROM TblNotificationDeviceToken 
                      WHERE CompanyID = @CompanyID AND UserID = @UserID AND IsActive = 1 AND IsDeleted = 0
                      ORDER BY LastUsedOn DESC",
                    new { CompanyID = companyID, UserID = userID })).ToList();

                return DbOperationResultHelpers.Success(tokens, "Device tokens retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device tokens for UserID: {UserID}", userID);
                return DbOperationResultHelpers.Failure<List<FirebaseDeviceTokenDto>>(ex.Message, exception: ex);
            }
        }

        public async Task<DbOperationResult> UpdateDeviceTokenAsync(int deviceTokenID, string deviceToken, string updatedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    @"UPDATE TblNotificationDeviceToken 
                      SET DeviceToken = @DeviceToken, LastUsedOn = GETDATE(), UpdatedOn = GETDATE(), UpdatedBy = @UpdatedBy
                      WHERE DeviceTokenID = @DeviceTokenID",
                    new { DeviceTokenID = deviceTokenID, DeviceToken = deviceToken, UpdatedBy = updatedBy });

                return DbOperationResultHelpers.Success("Device token updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device token: {DeviceTokenID}", deviceTokenID);
                return DbOperationResultHelpers.Failure(ex.Message, exception: ex);
            }
        }

        public async Task<DbOperationResult> DeleteDeviceTokenAsync(int deviceTokenID)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    @"UPDATE TblNotificationDeviceToken 
                      SET IsDeleted = 1, IsActive = 0, UpdatedOn = GETDATE()
                      WHERE DeviceTokenID = @DeviceTokenID",
                    new { DeviceTokenID = deviceTokenID });

                return DbOperationResultHelpers.Success("Device token deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device token: {DeviceTokenID}", deviceTokenID);
                return DbOperationResultHelpers.Failure(ex.Message, exception: ex);
            }
        }

        public async Task<DbOperationResult<FirebaseDeviceTokenDto>> GetDeviceTokenByTokenAsync(string deviceToken)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                var token = await connection.QueryFirstOrDefaultAsync<FirebaseDeviceTokenDto>(
                    @"SELECT * FROM TblNotificationDeviceToken 
                      WHERE DeviceToken = @DeviceToken AND IsActive = 1 AND IsDeleted = 0",
                    new { DeviceToken = deviceToken });

                if (token == null)
                {
                    return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>("Device token not found", (int?)ReturnCodes.NotFound);
                }

                return DbOperationResultHelpers.Success(token, "Device token retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device token by token");
                return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>(ex.Message, exception: ex);
            }
        }

        public async Task<DbOperationResult> LogNotificationAsync(int companyID, int userID, string messageId, bool isSuccess, string errorMessage = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.ExecuteAsync(
                    @"INSERT INTO TblNotificationLog 
                     (CompanyID, UserID, MessageId, IsSuccess, ErrorMessage, CreatedOn)
                     VALUES 
                     (@CompanyID, @UserID, @MessageId, @IsSuccess, @ErrorMessage, GETDATE())",
                    new { CompanyID = companyID, UserID = userID, MessageId = messageId, IsSuccess = isSuccess, ErrorMessage = errorMessage });

                return DbOperationResultHelpers.Success("Notification logged successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging notification for UserID: {UserID}", userID);
                return DbOperationResultHelpers.Failure(ex.Message, exception: ex);
            }
        }
    }
}


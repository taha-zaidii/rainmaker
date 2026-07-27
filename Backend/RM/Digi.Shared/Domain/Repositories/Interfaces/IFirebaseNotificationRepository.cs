

using Digi.Shared.DTOs;
using Digi.Shared.DTOs.notification;

namespace Digi.Shared.Domain.Repositories.Interfaces
{
    public interface IFirebaseNotificationRepository
    {
        Task<DbOperationResult<FirebaseDeviceTokenDto>> RegisterDeviceTokenAsync(RegisterDeviceTokenRequestDto request);
        Task<DbOperationResult<List<FirebaseDeviceTokenDto>>> GetUserDeviceTokensAsync(int companyID, int userID);
        Task<DbOperationResult> UpdateDeviceTokenAsync(int deviceTokenID, string deviceToken, string updatedBy);
        Task<DbOperationResult> DeleteDeviceTokenAsync(int deviceTokenID);
        Task<DbOperationResult<FirebaseDeviceTokenDto>> GetDeviceTokenByTokenAsync(string deviceToken);
        Task<DbOperationResult> LogNotificationAsync(int companyID, int userID, string messageId, bool isSuccess, string errorMessage = null);
    }
}


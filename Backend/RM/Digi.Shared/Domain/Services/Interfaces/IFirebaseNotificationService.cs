using Digi.Shared.DTOs.notification;
using Digi.Shared.Helper;

namespace Digi.Shared.Domain.Services.Interfaces
{
    public interface IFirebaseNotificationService
    {
        Task<ApiResponse<FirebaseDeviceTokenDto>> RegisterDeviceTokenAsync(RegisterDeviceTokenRequestDto request);
        Task<ApiResponse<List<FirebaseDeviceTokenDto>>> GetUserDeviceTokensAsync(int companyID, int userID);
        Task<ApiResponse<string>> DeleteDeviceTokenAsync(int deviceTokenID);
        Task<ApiResponse<FirebaseNotificationResponseDto>> SendNotificationAsync(SendFirebaseNotificationRequestDto request);
        Task<ApiResponse<FirebaseNotificationResponseDto>> SendBulkNotificationAsync(SendBulkFirebaseNotificationRequestDto request);
        Task<ApiResponse<string>> InitializeFirebaseAsync();
    }
}


using Digi.Shared.DTOs;
using Digi.Shared.DTOs.notification;
using Digi.Shared.Helper;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Digi.Shared.Domain.Services.Interfaces;
using Digi.Shared.Domain.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Digi.Shared.Domain.Services
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly IFirebaseNotificationRepository _repository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FirebaseNotificationService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private bool _isFirebaseInitialized = false;

        public FirebaseNotificationService(
            IFirebaseNotificationRepository repository,
            IConfiguration configuration,
            ILogger<FirebaseNotificationService> logger,
            IHostEnvironment hostEnvironment)
        {
            _repository = repository;
            _configuration = configuration;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<ApiResponse<string>> InitializeFirebaseAsync()
        {
            try
            {
                if (_isFirebaseInitialized && FirebaseApp.DefaultInstance != null)
                {
                    var existingProjectId = FirebaseApp.DefaultInstance!.Options?.ProjectId ?? "Unknown";
                    return ApiResponse<string>.Success("Firebase already initialized. Project ID: " + existingProjectId);
                }

                var projectId = _configuration["Firebase:ProjectId"];
                var credentialsPath = _configuration["Firebase:CredentialsPath"];
                
                // Try multiple ways to read CredentialsJson
                string credentialsJson = _configuration["Firebase:CredentialsJson"];
                
                if (string.IsNullOrWhiteSpace(credentialsJson))
                {
                    credentialsJson = _configuration.GetSection("Firebase")["CredentialsJson"];
                }
                
                if (string.IsNullOrWhiteSpace(credentialsJson))
                {
                    credentialsJson = _configuration.GetValue<string>("Firebase:CredentialsJson");
                }
                
                // Fallback: Read from file if still not found
                if (string.IsNullOrWhiteSpace(credentialsJson))
                {
                    try
                    {
                        var contentRootPath = _hostEnvironment?.ContentRootPath ?? Directory.GetCurrentDirectory();
                        var appsettingsPath = Path.Combine(contentRootPath, "appsettings.json");
                        
                        if (File.Exists(appsettingsPath))
                        {
                            var jsonContent = await File.ReadAllTextAsync(appsettingsPath);
                            using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                            {
                                if (doc.RootElement.TryGetProperty("Firebase", out var firebaseElement))
                                {
                                    if (firebaseElement.TryGetProperty("CredentialsJson", out var credJsonElement))
                                    {
                                        credentialsJson = credJsonElement.GetString();
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(fileEx, "Could not read CredentialsJson from file: {Error}", fileEx.Message);
                    }
                }

                if (string.IsNullOrEmpty(projectId))
                {
                    _logger.LogError("Firebase ProjectId is not configured");
                    return ApiResponse<string>.Fail("Firebase ProjectId is not configured", HttpStatusCode.BadRequest);
                }

                if (string.IsNullOrWhiteSpace(credentialsJson) && (string.IsNullOrEmpty(credentialsPath) || !File.Exists(credentialsPath)))
                {
                    _logger.LogError("Firebase credentials not found. Please set CredentialsPath or CredentialsJson in appsettings.json");
                    return ApiResponse<string>.Fail("No Firebase credentials provided. Please set CredentialsPath or CredentialsJson in appsettings.json", HttpStatusCode.BadRequest);
                }

                AppOptions options = new AppOptions
                {
                    ProjectId = projectId
                };

                if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
                {
                    options.Credential = GoogleCredential.FromFile(credentialsPath);
                }
                else if (!string.IsNullOrEmpty(credentialsJson))
                {
                    try
                    {
                        credentialsJson = credentialsJson.Trim();
                        
                        if (!credentialsJson.StartsWith("{") || !credentialsJson.EndsWith("}"))
                        {
                            _logger.LogError("Invalid JSON format for Firebase credentials");
                            return ApiResponse<string>.Fail("Invalid credentials JSON format - must be valid JSON object", HttpStatusCode.BadRequest);
                        }
                        
                        options.Credential = GoogleCredential.FromJson(credentialsJson);
                    }
                    catch (Exception jsonEx)
                    {
                        _logger.LogError(jsonEx, "Error parsing credentials JSON: {Error}", jsonEx.Message);
                        return ApiResponse<string>.Fail($"Invalid credentials JSON format: {jsonEx.Message}", HttpStatusCode.BadRequest);
                    }
                }
                else
                {
                    try
                    {
                        options.Credential = GoogleCredential.GetApplicationDefault();
                    }
                    catch (Exception defaultEx)
                    {
                        _logger.LogError(defaultEx, "Default credentials not available: {Error}", defaultEx.Message);
                        return ApiResponse<string>.Fail("No Firebase credentials provided. Please set CredentialsPath or CredentialsJson in appsettings.json", HttpStatusCode.BadRequest);
                    }
                }

                // Check if default instance already exists
                FirebaseApp firebaseApp;
                if (FirebaseApp.DefaultInstance != null)
                {
                    firebaseApp = FirebaseApp.DefaultInstance;
                    _isFirebaseInitialized = true;
                }
                else
                {
                    firebaseApp = FirebaseApp.Create(options);
                    _isFirebaseInitialized = true;
                }
                
                var initializedProjectId = firebaseApp.Options?.ProjectId ?? projectId;
                _logger.LogInformation("Firebase initialized successfully. Project ID: {ProjectId}", initializedProjectId);

                return ApiResponse<string>.Success($"Firebase initialized successfully! Project ID: {initializedProjectId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Firebase: {Message}", ex.Message);
                return ApiResponse<string>.Fail($"Error initializing Firebase: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<FirebaseDeviceTokenDto>> RegisterDeviceTokenAsync(RegisterDeviceTokenRequestDto request)
        {
            try
            {
                var result = await _repository.RegisterDeviceTokenAsync(request);
                if (result.IsSuccess)
                {
                    return ApiResponse<FirebaseDeviceTokenDto>.Success(result.Data, result.Message);
                }
                var status = result.ReturnCode == 409 ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest;
                return ApiResponse<FirebaseDeviceTokenDto>.Fail(result.Message, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device token");
                return ApiResponse<FirebaseDeviceTokenDto>.Fail($"Error registering device token: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<List<FirebaseDeviceTokenDto>>> GetUserDeviceTokensAsync(int companyID, int userID)
        {
            try
            {
                var result = await _repository.GetUserDeviceTokensAsync(companyID, userID);
                if (result.IsSuccess)
                {
                    return ApiResponse<List<FirebaseDeviceTokenDto>>.Success(result.Data, result.Message);
                }
                return ApiResponse<List<FirebaseDeviceTokenDto>>.Fail(result.Message, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user device tokens");
                return ApiResponse<List<FirebaseDeviceTokenDto>>.Fail($"Error getting user device tokens: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<string>> DeleteDeviceTokenAsync(int deviceTokenID)
        {
            try
            {
                var result = await _repository.DeleteDeviceTokenAsync(deviceTokenID);
                if (result.IsSuccess)
                {
                    return ApiResponse<string>.Success(null, result.Message);
                }
                return ApiResponse<string>.Fail(result.Message, HttpStatusCode.BadRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device token");
                return ApiResponse<string>.Fail($"Error deleting device token: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<FirebaseNotificationResponseDto>> SendNotificationAsync(SendFirebaseNotificationRequestDto request)
        {
            try
            {
                var initialized = await InitializeFirebaseAsync();
                if (!initialized.IsSuccess)
                {
                    return ApiResponse<FirebaseNotificationResponseDto>.Fail(
                        initialized.Message, HttpStatusCode.BadRequest);
                }

                // Get device tokens for the user
                var tokensResult = await _repository.GetUserDeviceTokensAsync(request.CompanyID, request.UserID);
                if (!tokensResult.IsSuccess || tokensResult.Data == null || !tokensResult.Data.Any())
                {
                    return ApiResponse<FirebaseNotificationResponseDto>.Fail(
                        "No device tokens found for this user", HttpStatusCode.NotFound);
                }

                var response = new FirebaseNotificationResponseDto
                {
                    IsSuccess = false,
                    SuccessCount = 0,
                    FailureCount = 0,
                    Errors = new List<FirebaseNotificationErrorDto>()
                };

                var messaging = FirebaseMessaging.DefaultInstance;

                foreach (var deviceToken in tokensResult.Data)
                {
                    try
                    {
                        var message = new Message
                        {
                            Token = deviceToken.DeviceToken,
                            Notification = new FirebaseAdmin.Messaging.Notification
                            {
                                Title = request.Title,
                                //Link = 
                                Body = request.Body,
                                ImageUrl = request.ImageUrl
                            },
                            Data = request.Data,
                            Android = deviceToken.DeviceType == "Android" ? new AndroidConfig
                            {
                                Priority = request.Priority == 2 ? Priority.High : Priority.Normal,
                                Notification = new AndroidNotification
                                {
                                    Sound = request.Sound,
                                    ChannelId = request.ChannelId ?? "default",
                                    ClickAction = request.ClickAction,
                                    Icon = "ic_notification",
                                    Color = "#FF5722"
                                }
                            } : null,
                            Apns = deviceToken.DeviceType == "iOS" ? new ApnsConfig
                            {
                                Aps = new Aps
                                {
                                    Sound = request.Sound,
                                    ContentAvailable = request.IsSilent,
                                    Badge = 1,
                                    Alert = new ApsAlert
                                    {
                                        Title = request.Title,
                                        Body = request.Body
                                    }
                                }
                            } : null
                        };

                        var messageId = await messaging.SendAsync(message);
                        
                        response.SuccessCount++;
                        response.MessageId = messageId;
                        response.IsSuccess = true;
                        response.Message = "Notification sent successfully";

                        // Log success
                        await _repository.LogNotificationAsync(request.CompanyID, request.UserID, messageId, true);
                    }
                    catch (FirebaseMessagingException ex)
                    {
                        response.FailureCount++;
                        response.Errors.Add(new FirebaseNotificationErrorDto
                        {
                            UserID = request.UserID,
                            DeviceToken = deviceToken.DeviceToken,
                            ErrorCode = ex.ErrorCode.ToString(),
                            ErrorMessage = ex.Message
                        });

                        // Log failure
                        await _repository.LogNotificationAsync(request.CompanyID, request.UserID, null, false, ex.Message);

                        // If token is invalid, delete it
                        var errorCodeStr = ex.ErrorCode.ToString();
                        if (errorCodeStr == "InvalidArgument" || errorCodeStr == "NotFound")
                        {
                            await _repository.DeleteDeviceTokenAsync(deviceToken.DeviceTokenID);
                        }
                    }
                    catch (Exception ex)
                    {
                        response.FailureCount++;
                        response.Errors.Add(new FirebaseNotificationErrorDto
                        {
                            UserID = request.UserID,
                            DeviceToken = deviceToken.DeviceToken,
                            ErrorCode = "UNKNOWN",
                            ErrorMessage = ex.Message
                        });

                        await _repository.LogNotificationAsync(request.CompanyID, request.UserID, null, false, ex.Message);
                    }
                }

                if (response.SuccessCount > 0)
                {
                    return ApiResponse<FirebaseNotificationResponseDto>.Success(response, "Notification sent successfully");
                }
                else
                {
                    // Return success with response object containing error details
                    return ApiResponse<FirebaseNotificationResponseDto>.Success(response, "Failed to send notification to any device");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Firebase notification");
                return ApiResponse<FirebaseNotificationResponseDto>.Fail($"Error sending Firebase notification: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ApiResponse<FirebaseNotificationResponseDto>> SendBulkNotificationAsync(SendBulkFirebaseNotificationRequestDto request)
        {
            try
            {
                var initialized = await InitializeFirebaseAsync();
                if (!initialized.IsSuccess)
                {
                    return ApiResponse<FirebaseNotificationResponseDto>.Fail(
                        initialized.Message, HttpStatusCode.BadRequest);
                }

                var response = new FirebaseNotificationResponseDto
                {
                    IsSuccess = false,
                    SuccessCount = 0,
                    FailureCount = 0,
                    Errors = new List<FirebaseNotificationErrorDto>()
                };

                var messaging = FirebaseMessaging.DefaultInstance;
                var messages = new List<Message>();

                // Collect all device tokens for all users
                foreach (var userID in request.UserIDs)
                {
                    var tokensResult = await _repository.GetUserDeviceTokensAsync(request.CompanyID, userID);
                    if (tokensResult.IsSuccess && tokensResult.Data != null)
                    {
                        foreach (var deviceToken in tokensResult.Data)
                        {
                            var message = new Message
                            {
                                Token = deviceToken.DeviceToken,
                                Notification = new FirebaseAdmin.Messaging.Notification
                                {
                                    Title = request.Title,
                                    Body = request.Body,
                                    ImageUrl = request.ImageUrl
                                },
                                Data = request.Data,
                                Android = deviceToken.DeviceType == "Android" ? new AndroidConfig
                                {
                                    Priority = request.Priority == 2 ? Priority.High : Priority.Normal,
                                    Notification = new AndroidNotification
                                    {
                                        Sound = request.Sound,
                                        ChannelId = request.ChannelId ?? "default",
                                        ClickAction = request.ClickAction,
                                        Icon = "ic_notification",
                                        Color = "#FF5722"
                                    }
                                } : null,
                                Apns = deviceToken.DeviceType == "iOS" ? new ApnsConfig
                                {
                                    Aps = new Aps
                                    {
                                        Sound = request.Sound,
                                        ContentAvailable = request.IsSilent,
                                        Badge = 1,
                                        Alert = new ApsAlert
                                        {
                                            Title = request.Title,
                                            Body = request.Body
                                        }
                                    }
                                } : null
                            };

                            messages.Add(message);
                        }
                    }
                }

                if (!messages.Any())
                {
                    return ApiResponse<FirebaseNotificationResponseDto>.Fail(
                        "No device tokens found for the specified users", HttpStatusCode.NotFound);
                }

                // Send all messages
                var batchResponse = await messaging.SendEachAsync(messages);

                response.SuccessCount = batchResponse.SuccessCount;
                response.FailureCount = batchResponse.FailureCount;
                response.IsSuccess = batchResponse.SuccessCount > 0;
                response.Message = $"Sent {batchResponse.SuccessCount} notifications, {batchResponse.FailureCount} failed";

                // Log results
                for (int i = 0; i < batchResponse.Responses.Count; i++)
                {
                    var sendResponse = batchResponse.Responses[i];
                    var message = messages[i];
                    
                    if (sendResponse.IsSuccess)
                    {
                        // Find userID from token (you may need to adjust this logic)
                        await _repository.LogNotificationAsync(request.CompanyID, 0, sendResponse.MessageId, true);
                    }
                    else
                    {
                        response.Errors.Add(new FirebaseNotificationErrorDto
                        {
                            DeviceToken = message.Token,
                            ErrorCode = sendResponse.Exception?.ErrorCode.ToString() ?? "UNKNOWN",
                            ErrorMessage = sendResponse.Exception?.Message ?? "Unknown error"
                        });
                        await _repository.LogNotificationAsync(request.CompanyID, 0, null, false, sendResponse.Exception?.Message ?? "Unknown error");
                    }
                }

                return ApiResponse<FirebaseNotificationResponseDto>.Success(response, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk Firebase notifications");
                return ApiResponse<FirebaseNotificationResponseDto>.Fail($"Error sending bulk Firebase notifications: {ex.Message}", HttpStatusCode.InternalServerError);
            }
        }
    }
}


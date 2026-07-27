using Digi.Shared.DTOs;
using Digi.Shared.DTOs.admin.module;
using Digi.Shared.DTOs.notification;
using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text.RegularExpressions;

namespace Digi.Shared.Services
{
    public sealed class DeviceRegistrationIdentity
    {
        public string? MacAddress { get; init; }
        public string? DeviceUniqueId { get; init; }
        public bool IsDeviceUniqueId { get; init; }
        public string DeviceType { get; init; } = "Android";
        public string? DeviceName { get; init; }
        public string? DeviceModel { get; init; }
        public string? OSVersion { get; init; }
        public string? AppVersion { get; init; }
        public string? PlainDeviceToken { get; init; }
        public string ComputedUniqueId { get; init; } = string.Empty;

        public bool HasClientIdentity =>
            IsDeviceUniqueId
            || !string.IsNullOrWhiteSpace(PlainDeviceToken)
            || !string.IsNullOrWhiteSpace(MacAddress)
            || !string.IsNullOrWhiteSpace(DeviceUniqueId);
    }

    public sealed class SingleDeviceValidationResult
    {
        public bool IsAllowed { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? ActiveDeviceName { get; init; }

        public static SingleDeviceValidationResult Allow() => new() { IsAllowed = true };

        public static SingleDeviceValidationResult Block(string code, string message, string? activeDeviceName = null) =>
            new()
            {
                IsAllowed = false,
                ErrorCode = code,
                ErrorMessage = message,
                ActiveDeviceName = activeDeviceName
            };
    }

    public static partial class NotificationDeviceTokenRegistrationHelper
    {
        public static DeviceRegistrationIdentity Resolve(RegisterDeviceTokenRequestDto request, string? deviceIdHeader = null)
        {
            var macAddress = TrimOrNull(request.MacAddress);
            var deviceUniqueId = TrimOrNull(request.DeviceUniqueId)
                ?? TrimOrNull(deviceIdHeader);

            var isDeviceUniqueId = request.IsDeviceUniqueId
                || !string.IsNullOrWhiteSpace(deviceUniqueId)
                || !string.IsNullOrWhiteSpace(macAddress);

            var deviceType = string.IsNullOrWhiteSpace(request.DeviceType) ? "Android" : request.DeviceType.Trim();
            var lookupKey = FirstNonEmpty(deviceUniqueId, macAddress);
            var computedUniqueId = DeviceUniqueIdHelper.Compute(
                !string.IsNullOrWhiteSpace(lookupKey),
                lookupKey,
                deviceType,
                request.DeviceName,
                request.DeviceModel,
                request.OSVersion,
                request.AppVersion);

            return new DeviceRegistrationIdentity
            {
                MacAddress = macAddress,
                DeviceUniqueId = deviceUniqueId,
                IsDeviceUniqueId = isDeviceUniqueId,
                DeviceType = deviceType,
                DeviceName = request.DeviceName,
                DeviceModel = request.DeviceModel,
                OSVersion = request.OSVersion,
                AppVersion = request.AppVersion,
                PlainDeviceToken = string.IsNullOrWhiteSpace(request.DeviceToken) ? null : request.DeviceToken.Trim(),
                ComputedUniqueId = lookupKey ?? computedUniqueId
            };
        }

        /// <summary>
        /// Single-device policy applies only to mobile app logins (Android/iOS), not Angular/web.
        /// </summary>
        public static bool IsMobileLoginContext(LoginDto? loginDto, string? deviceIdHeader)
        {
            if (!string.IsNullOrWhiteSpace(deviceIdHeader))
                return true;

            if (loginDto == null)
                return false;

            if (!string.IsNullOrWhiteSpace(loginDto.MacAddress))
                return true;

            if (!string.IsNullOrWhiteSpace(loginDto.DeviceToken))
                return true;

            return IsMobileDeviceType(loginDto.DeviceType);
        }

        /// <summary>
        /// When enabled, single-device binding runs for mobile logins only.
        /// Web/browser logins are skipped unless <c>Auth:EnforceSingleActiveDeviceForWeb</c> is true.
        /// </summary>
        public static bool ShouldEnforceSingleDeviceOnLogin(
            LoginDto? loginDto,
            string? deviceIdHeader,
            IConfiguration? configuration,
            string? userAgent = null)
        {
            if (configuration?.GetValue<bool?>("Auth:EnforceSingleActiveDevice") == false)
                return false;

            if (configuration?.GetValue<bool?>("Auth:EnforceSingleActiveDeviceForWeb") == true)
                return true;

            var identity = Resolve(loginDto, deviceIdHeader);
            if (IsWebLoginWithoutDeviceProof(loginDto, deviceIdHeader, identity))
                return false;

            if (!IsLikelyMobileAppClient(userAgent, loginDto, deviceIdHeader))
                return false;

            return true;
        }

        /// <summary>
        /// Detects Flutter/Dart/OkHttp and other native mobile HTTP stacks (not Angular/browser).
        /// </summary>
        public static bool IsLikelyMobileAppClient(string? userAgent, LoginDto? loginDto, string? deviceIdHeader)
        {
            if (IsMobileLoginContext(loginDto, deviceIdHeader))
                return true;

            if (string.IsNullOrWhiteSpace(userAgent))
                return false;

            var ua = userAgent.ToLowerInvariant();
            return ua.Contains("okhttp")
                || ua.Contains("dart")
                || ua.Contains("flutter")
                || (ua.Contains("cfnetwork") && ua.Contains("darwin"))
                || ua.Contains("alamofire");
        }

        public static DeviceRegistrationIdentity Resolve(LoginDto? loginDto, string? deviceIdHeader = null)
        {
            var macAddress = TrimOrNull(loginDto?.MacAddress);
            var deviceUniqueId = TrimOrNull(loginDto?.DeviceUniqueId)
                ?? TrimOrNull(deviceIdHeader);

            var isDeviceUniqueId = loginDto?.IsDeviceUniqueId == true
                || !string.IsNullOrWhiteSpace(deviceUniqueId)
                || !string.IsNullOrWhiteSpace(macAddress);

            var deviceType = loginDto?.DeviceType?.Trim() ?? string.Empty;
            var lookupKey = FirstNonEmpty(deviceUniqueId, macAddress);
            var computedUniqueId = DeviceUniqueIdHelper.Compute(
                !string.IsNullOrWhiteSpace(lookupKey),
                lookupKey,
                deviceType,
                loginDto?.DeviceName,
                loginDto?.DeviceModel,
                loginDto?.OSVersion,
                loginDto?.AppVersion);

            return new DeviceRegistrationIdentity
            {
                MacAddress = macAddress,
                DeviceUniqueId = deviceUniqueId,
                IsDeviceUniqueId = isDeviceUniqueId,
                DeviceType = deviceType,
                DeviceName = loginDto?.DeviceName,
                DeviceModel = loginDto?.DeviceModel,
                OSVersion = loginDto?.OSVersion,
                AppVersion = loginDto?.AppVersion,
                PlainDeviceToken = string.IsNullOrWhiteSpace(loginDto?.DeviceToken) ? null : loginDto!.DeviceToken!.Trim(),
                ComputedUniqueId = lookupKey ?? computedUniqueId
            };
        }

        /// <summary>
        /// Login/register: active row exists → incoming <see cref="DeviceRegistrationIdentity.DeviceUniqueId"/>
        /// must match table <c>DeviceUniqueId</c> (case-insensitive). No row → allow (first bind via SP).
        /// </summary>
        public static async Task<SingleDeviceValidationResult> ValidateLoginDeviceBindingAsync(
            IDapperService dapper,
            int companyId,
            int userId,
            DeviceRegistrationIdentity incoming,
            IConfiguration? configuration = null,
            LoginDto? loginDto = null,
            string? deviceIdHeader = null,
            string? userAgent = null)
        {
            if (configuration?.GetValue<bool?>("Auth:EnforceSingleActiveDevice") == false)
                return SingleDeviceValidationResult.Allow();

            if (!ShouldEnforceSingleDeviceOnLogin(loginDto, deviceIdHeader, configuration, userAgent))
                return SingleDeviceValidationResult.Allow();

            var active = await QueryActiveDeviceRowAsync(dapper, companyId, userId);
            if (active == null)
                return SingleDeviceValidationResult.Allow();

            var incomingId = TrimOrNull(incoming.DeviceUniqueId);
            if (string.IsNullOrWhiteSpace(incomingId))
            {
                return SingleDeviceValidationResult.Block(
                    "DEVICE_PROOF_REQUIRED",
                    "Send deviceUniqueId in the login body (same value as registered on this phone).",
                    active.DeviceName);
            }

            var registeredId = TrimOrNull(active.DeviceUniqueId);
            if (string.IsNullOrWhiteSpace(registeredId))
                return SingleDeviceValidationResult.Allow();

            if (string.Equals(registeredId, incomingId, StringComparison.OrdinalIgnoreCase))
                return SingleDeviceValidationResult.Allow();

            return SingleDeviceValidationResult.Block(
                "ALREADY_LOGGED_IN_ELSEWHERE",
                "You are already logged in from another device.",
                active.DeviceName);
        }

        public static async Task<SingleDeviceValidationResult> ValidateAgainstActiveDeviceAsync(
            IDapperService dapper,
            int companyId,
            int userId,
            DeviceRegistrationIdentity incoming,
            IConfiguration? configuration = null,
            bool enforcePolicy = true,
            LoginDto? loginDto = null,
            string? deviceIdHeader = null,
            string? userAgent = null)
        {
            if (!enforcePolicy)
                return SingleDeviceValidationResult.Allow();

            if (configuration?.GetValue<bool?>("Auth:EnforceSingleActiveDevice") == false)
                return SingleDeviceValidationResult.Allow();

            _ = loginDto;
            _ = deviceIdHeader;
            _ = userAgent;
            return await ValidateLoginDeviceBindingAsync(
                dapper,
                companyId,
                userId,
                incoming,
                configuration,
                loginDto,
                deviceIdHeader,
                userAgent);
        }

        private static async Task<ActiveNotificationDeviceRow?> QueryActiveDeviceRowAsync(
            IDapperService dapper,
            int companyId,
            int userId) =>
            await dapper.QueryFirstOrDefaultAsync<ActiveNotificationDeviceRow>(
                @"SELECT TOP 1
                      DeviceTokenID,
                      DeviceUniqueId,
                      MacAddress,
                      DeviceName,
                      DeviceToken,
                      DeviceType,
                      DeviceModel,
                      OSVersion,
                      AppVersion
                  FROM dbo.TblNotificationDeviceToken
                  WHERE CompanyID = @CompanyID
                    AND UserID = @UserID
                    AND IsActive = 1
                    AND IsDeleted = 0
                  ORDER BY LastUsedOn DESC",
                new { CompanyID = companyId, UserID = userId },
                CommandType.Text);

        public static async Task<NotificationDeviceTokenRegisterResultDto?> RegisterViaStoredProcedureAsync(
            IDapperService dapper,
            IConfiguration configuration,
            int companyId,
            int userId,
            DeviceRegistrationIdentity identity,
            string createdBy,
            bool enforceSingleActiveDevice = true)
        {
            if (string.IsNullOrWhiteSpace(identity.PlainDeviceToken))
                return null;

            var encryptAtRest = configuration.GetValue<bool?>("Firebase:EncryptDeviceTokensAtRest") == true;
            var tokenForDb = encryptAtRest
                ? EncryptionHelper.EncryptText(identity.PlainDeviceToken)
                : identity.PlainDeviceToken;

            return await dapper.QueryFirstOrDefaultAsync<NotificationDeviceTokenRegisterResultDto>(
                "sp_NotificationDeviceToken_Register",
                new
                {
                    CompanyID = companyId,
                    UserID = userId,
                    DeviceToken = tokenForDb,
                    DeviceType = identity.DeviceType,
                    identity.DeviceName,
                    identity.DeviceModel,
                    identity.OSVersion,
                    identity.AppVersion,
                    CreatedBy = createdBy,
                    MacAddress = identity.MacAddress,
                    DeviceUniqueId = identity.DeviceUniqueId,
                    IsDeviceUniqueId = identity.IsDeviceUniqueId,
                    EnforceSingleActiveDevice = enforceSingleActiveDevice
                },
                CommandType.StoredProcedure);
        }

        public static SingleDeviceValidationResult MapSpResult(NotificationDeviceTokenRegisterResultDto? spResult)
        {
            if (spResult == null)
            {
                return SingleDeviceValidationResult.Block(
                    "DEVICE_REGISTER_FAILED",
                    "Device registration did not return a result.");
            }

            if (!spResult.IsSuccess || !string.IsNullOrWhiteSpace(spResult.ErrorCode))
            {
                return SingleDeviceValidationResult.Block(
                    spResult.ErrorCode ?? "DEVICE_REGISTER_FAILED",
                    spResult.ErrorMessage
                        ?? "You are already logged in from another device.",
                    spResult.ActiveDeviceName);
            }

            if (spResult.DeviceTokenID <= 0)
            {
                return SingleDeviceValidationResult.Block(
                    "DEVICE_REGISTER_FAILED",
                    spResult.ErrorMessage ?? "Device registration failed.");
            }

            return SingleDeviceValidationResult.Allow();
        }

        public static DbOperationResult<FirebaseDeviceTokenDto> MapSpResultToDeviceDto(
            NotificationDeviceTokenRegisterResultDto? spResult,
            DeviceRegistrationIdentity? identity = null,
            string successMessage = "Device token registered successfully")
        {
            if (spResult == null)
                return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>("No response from device registration.");

            if (!spResult.IsSuccess || !string.IsNullOrWhiteSpace(spResult.ErrorCode))
            {
                var code = string.Equals(spResult.ErrorCode, "ALREADY_LOGGED_IN_ELSEWHERE", StringComparison.OrdinalIgnoreCase)
                    ? 409
                    : 400;
                return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>(
                    spResult.ErrorMessage ?? spResult.ErrorCode ?? "Device registration failed.",
                    returnCode: code);
            }

            if (spResult.DeviceTokenID <= 0)
                return DbOperationResultHelpers.Failure<FirebaseDeviceTokenDto>("Device registration failed.");

            var token = new FirebaseDeviceTokenDto
            {
                DeviceTokenID = spResult.DeviceTokenID,
                CompanyID = spResult.CompanyID,
                UserID = spResult.UserID,
                DeviceToken = spResult.DeviceToken ?? string.Empty,
                DeviceType = spResult.DeviceType ?? "Android",
                DeviceName = spResult.DeviceName,
                DeviceUniqueId = spResult.DeviceUniqueId ?? identity?.DeviceUniqueId,
                MacAddress = spResult.MacAddress ?? identity?.MacAddress,
                IsDeviceUniqueId = spResult.IsDeviceUniqueId || (identity?.IsDeviceUniqueId ?? false),
                DeviceModel = spResult.DeviceModel,
                OSVersion = spResult.OSVersion,
                AppVersion = spResult.AppVersion,
                IsActive = spResult.IsActive,
                IsDeleted = spResult.IsDeleted,
                LastUsedOn = spResult.LastUsedOn ?? DateTime.UtcNow,
                CreatedOn = spResult.CreatedOn ?? DateTime.UtcNow,
                CreatedBy = spResult.CreatedBy,
                UpdatedOn = spResult.UpdatedOn,
                UpdatedBy = spResult.UpdatedBy,
                UserName = spResult.UserName
            };

            return DbOperationResultHelpers.Success(token, successMessage);
        }

        private static string? TrimOrNull(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }

            return null;
        }

        private static bool IsMobileDeviceType(string? deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                return false;

            var t = deviceType.Trim();
            return string.Equals(t, "Android", StringComparison.OrdinalIgnoreCase)
                || IsAppleMobileDeviceType(t);
        }

        private static bool IsAppleMobileDeviceType(string? deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                return false;

            var t = deviceType.Trim();
            return string.Equals(t, "iOS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "iphone", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t, "ipad", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// True only for browser-style auth with no device proof. Register API and mobile payloads never qualify.
        /// </summary>
        private static bool IsWebLoginWithoutDeviceProof(
            LoginDto? loginDto,
            string? deviceIdHeader,
            DeviceRegistrationIdentity incoming)
        {
            if (incoming.HasClientIdentity || IsMobileDeviceType(incoming.DeviceType))
                return false;

            if (!string.IsNullOrWhiteSpace(deviceIdHeader))
                return false;

            // Register / notification endpoints call validation without LoginDto — must enforce.
            if (loginDto == null)
                return false;

            return !IsMobileLoginContext(loginDto, deviceIdHeader);
        }

        /// <summary>
        /// OS build id (RP1A..., RKQ1...) — same on many phones; not valid for single-device ERP policy.
        /// </summary>
        public static bool IsWeakDeviceUniqueId(string? deviceUniqueId) =>
            IsUntrustedDeviceIdentity(deviceUniqueId, deviceUniqueId);

        /// <summary>
        /// Build fingerprint / OS build id (e.g. RP1A.200720.011) is not unique per phone — do not treat as device identity.
        /// </summary>
        private static bool IsUntrustedDeviceIdentity(string? deviceUniqueId, string? macAddress)
        {
            if (string.IsNullOrWhiteSpace(deviceUniqueId))
                return false;

            var id = deviceUniqueId.Trim();
            if (!string.IsNullOrWhiteSpace(macAddress)
                && string.Equals(id, macAddress.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            return AndroidBuildFingerprintRegex().IsMatch(id);
        }

        [GeneratedRegex(@"^[A-Z]{1,4}\d?[A-Z]?\.[\d]+\.[\d]+\.[\d]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex AndroidBuildFingerprintRegex();

        private static bool DeviceUniqueIdsMatch(ActiveNotificationDeviceRow active, DeviceRegistrationIdentity incoming)
        {
            if (string.IsNullOrWhiteSpace(active.DeviceUniqueId))
                return false;

            var activeId = active.DeviceUniqueId.Trim();
            foreach (var candidate in new[] { incoming.DeviceUniqueId, incoming.ComputedUniqueId })
            {
                if (!string.IsNullOrWhiteSpace(candidate)
                    && string.Equals(activeId, candidate.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static async Task RebindDeviceUniqueIdAsync(
            IDapperService dapper,
            int deviceTokenId,
            DeviceRegistrationIdentity incoming)
        {
            await dapper.ExecuteAsync(
                @"UPDATE dbo.TblNotificationDeviceToken
                  SET DeviceUniqueId = @DeviceUniqueId,
                      IsDeviceUniqueId = 1,
                      LastUsedOn = SYSUTCDATETIME(),
                      UpdatedOn = SYSUTCDATETIME()
                  WHERE DeviceTokenID = @DeviceTokenID",
                new
                {
                    DeviceTokenID = deviceTokenId,
                    DeviceUniqueId = incoming.DeviceUniqueId ?? incoming.ComputedUniqueId
                },
                CommandType.Text);
        }

        private sealed class ActiveNotificationDeviceRow
        {
            public int DeviceTokenID { get; set; }
            public string? DeviceUniqueId { get; set; }
            public string? MacAddress { get; set; }
            public string? DeviceName { get; set; }
            public string? DeviceToken { get; set; }
            public string? DeviceType { get; set; }
            public string? DeviceModel { get; set; }
            public string? OSVersion { get; set; }
            public string? AppVersion { get; set; }
        }
    }
}

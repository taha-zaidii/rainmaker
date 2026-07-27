using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Security.Claims;

namespace Digi.Shared.Services
{
    public static class JwtSecurityStampValidator
    {
        public const string SecurityStampClaimType = "SecurityStamp";

        public static async Task ValidateAsync(TokenValidatedContext context)
        {
            var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
            if (configuration?.GetValue("Auth:ValidateSecurityStampOnJwt", true) == false)
                return;

            var identity = context.Principal?.Identity as ClaimsIdentity;
            if (identity == null || !identity.IsAuthenticated)
                return;

            var userIdClaim = identity.FindFirst("UserID")?.Value;
            if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
                return;

            var stampClaim = identity.FindFirst(SecurityStampClaimType)?.Value;
            if (string.IsNullOrWhiteSpace(stampClaim))
            {
                context.Fail("Session expired. Please login again.");
                return;
            }

            var dapper = context.HttpContext.RequestServices.GetService<IDapperService>();
            if (dapper == null)
                return;

            var dbStamp = await UserSessionInvalidationHelper.GetSecurityStampAsync(dapper, userId);
            if (string.IsNullOrWhiteSpace(dbStamp)
                || !string.Equals(dbStamp, stampClaim, StringComparison.Ordinal))
            {
                context.Fail("Session expired. Please login again.");
            }
        }
    }

    public static class JwtBearerDigiExtensions
    {
        /// <summary>
        /// Wraps existing <see cref="JwtBearerEvents.OnTokenValidated"/> to reject tokens after password reset (SecurityStamp mismatch).
        /// </summary>
        public static void ApplyDigiSoftErpJwtSecurityStampValidation(this JwtBearerOptions options)
        {
            options.Events ??= new JwtBearerEvents();
            var previousOnValidated = options.Events.OnTokenValidated;

            options.Events.OnTokenValidated = async context =>
            {
                await JwtSecurityStampValidator.ValidateAsync(context);

                if (context.Result != null && !context.Result.Succeeded)
                    return;

                if (previousOnValidated != null)
                    await previousOnValidated(context);
            };
        }
    }
}

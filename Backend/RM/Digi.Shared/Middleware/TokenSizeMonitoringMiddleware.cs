using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Digi.Shared.Middleware
{
    /// <summary>
    /// Middleware to monitor and log JWT token sizes
    /// Helps identify token size issues before they cause problems
    /// </summary>
    public class TokenSizeMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenSizeMonitoringMiddleware> _logger;
        private const int WARNING_SIZE_BYTES = 3072; // 3KB - IIS default header limit warning
        private const int ERROR_SIZE_BYTES = 4096; // 4KB - Critical threshold

        public TokenSizeMonitoringMiddleware(RequestDelegate next, ILogger<TokenSizeMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Monitor Authorization header size
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader.Substring(7); // Remove "Bearer "
                    var tokenSize = Encoding.UTF8.GetByteCount(token);

                    // Log warnings for large tokens
                    if (tokenSize >= ERROR_SIZE_BYTES)
                    {
                        _logger.LogError(
                            "CRITICAL: JWT token size ({Size} bytes) exceeds critical threshold ({Threshold} bytes). " +
                            "UserID: {UserID}, Path: {Path}",
                            tokenSize, ERROR_SIZE_BYTES,
                            context.User?.FindFirst("UserID")?.Value ?? "Unknown",
                            context.Request.Path);
                    }
                    else if (tokenSize >= WARNING_SIZE_BYTES)
                    {
                        _logger.LogWarning(
                            "WARNING: JWT token size ({Size} bytes) exceeds recommended limit ({Threshold} bytes). " +
                            "UserID: {UserID}, Path: {Path}",
                            tokenSize, WARNING_SIZE_BYTES,
                            context.User?.FindFirst("UserID")?.Value ?? "Unknown",
                            context.Request.Path);
                    }
                    else
                    {
                        _logger.LogDebug("JWT token size: {Size} bytes", tokenSize);
                    }

                    // Add custom header for monitoring (optional)
                    context.Response.Headers.Append("X-Token-Size-Bytes", tokenSize.ToString());
                }
            }

            await _next(context);
        }
    }
}


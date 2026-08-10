using Digi.Shared.DTOs;
using Digi.Shared.SharedLibrary.Interfaces;
using Digi.Shared.SharedLibrary.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Middleware
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var corsSection = configuration.GetSection("Cors");
            var corsSettings = corsSection.Get<CorsDto>();

            if (corsSettings?.AllowedOrigins == null || corsSettings.AllowedOrigins.Length == 0)
                throw new InvalidOperationException("CORS settings are missing.");

            services.AddSingleton(corsSettings);

            services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", builder =>
                {
                    builder
                        .WithOrigins(corsSettings.AllowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Content-Disposition");
                });
            });

            return services;
        }

        /// <summary>
        /// Register Audit Log Service for generic audit logging across all modules
        /// </summary>
        public static IServiceCollection AddAuditLogService(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<IAuditLogService, AuditLogService>();
            return services;
        }
    }
}

//using Digi.Admin.Module.Domain.Services;
//using Digi.Admin.Module.Domain.Services.IServices;
using Digi.Recruitment.Module.Domain.AI.Multinet;
using Digi.Recruitment.Module.Domain.Repositories;
using Digi.Recruitment.Module.Domain.Repositories.IRepositories;
using Digi.Recruitment.Module.Domain.Services;
using Digi.Recruitment.Module.Domain.Services.IServices;
using Digi.Shared.DTOs.admin.module;
using Digi.Shared.Services;
using Digi.Shared.SharedLibrary.Interfaces;
using Digi.Shared.SharedLibrary.Options;
using Digi.Shared.SharedLibrary.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using SharedSmtpRepository = Digi.Shared.SharedLibrary.Services.SmtpRepository;

namespace Digi.Recruitment.Module.Middleware
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddRecruitmentModuleServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register HttpClientFactory for AI services
            services.AddHttpClient();

            // Multinet's own in-house AI service (hrms-ai-service): a metered,
            // API-key-authenticated provider alongside the OpenAI / Anthropic /
            // Google integrations, running on company GPUs. Off by default —
            // enable it per environment with MultinetAI:Enabled.
            services.AddMultinetAiService(configuration);

            // Register IDbConnection
            var connectionStringForDb = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionStringForDb))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' is not configured in appsettings.json");
            }
            
            services.AddScoped<IDbConnection>(sp =>
            {
                var conn = new SqlConnection();
                conn.ConnectionString = connectionStringForDb;
                
                if (string.IsNullOrWhiteSpace(conn.ConnectionString))
                {
                    throw new InvalidOperationException(
                        $"Failed to set ConnectionString on SqlConnection. " +
                        $"Connection string value length: {connectionStringForDb?.Length ?? 0}");
                }
                
                return conn;
            });

            // Register Dapper Service
            services.AddScoped<IDapperService, DapperService>();

            // Register Recruitment Repositories
            services.AddScoped<IRecruitmentRepository, RecruitmentRepository>();
            services.AddScoped<IRecruitmentAIRepository, RecruitmentAIRepository>();

            // Register Recruitment Services
            services.AddScoped<IRecruitmentService, RecruitmentService>();
            services.AddScoped<IRecruitmentAIService, RecruitmentAIService>();

            // Register File Storage Services
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IFileService, FileService>();

            //services.AddScoped<ICompanyRegistrationService, CompanyRegistrationService>();



            // Register centralized email service for Recruitment module
            services.AddScoped<ISmtpRepository>(sp => 
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new SharedSmtpRepository(config);
            });
            services.AddScoped<ICentralizedEmailService, CentralizedEmailService>();

            // Register Workflow Service (from Shared)
            services.AddScoped<IWorkflowService, WorkflowService>();

            // Configure file storage settings
            services.Configure<FileStorageSettings>(configuration.GetSection("FileStorage"));

            return services;
        }
    }
}

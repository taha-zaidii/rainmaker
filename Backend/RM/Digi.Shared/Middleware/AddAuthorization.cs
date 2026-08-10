using Digi.Shared.Middleware;
using Digi.Shared.SharedLibrary.Interfaces;
using Digi.Shared.SharedLibrary.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Digi.Shared.Middleware
{
    public static class AuthorizationServiceExtensions
    {
        public static IServiceCollection AddCustomAuthorizationPolicies(this IServiceCollection services)
        {
            const string superAdminEmail = "superadmin@gmail.com";

            services.AddAuthorization(options =>
            {
                // SuperAdmin has all permissions
                options.AddPolicy("SuperAdmin", policy =>
                    policy.RequireAssertion(context =>
                        context.User.HasClaim(c =>
                            c.Type == ClaimTypes.Email &&
                            c.Value.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))));

                // Module-wise policies
                options.AddPolicy("HRM_VIEW", policy =>
                    policy.RequireClaim("Permission", "HRM_VIEW")
                          .RequireAssertion(context => IsSuperAdmin(context, superAdminEmail)));

                options.AddPolicy("HRM_CREATE", policy =>
                    policy.RequireClaim("Permission", "HRM_CREATE")
                          .RequireAssertion(context => IsSuperAdmin(context, superAdminEmail)));

                // Active subscription policy
                options.AddPolicy("ActiveSubscription", policy =>
                    policy.AddRequirements(new ActiveSubscriptionRequirement())
                          .AddAuthenticationSchemes("Bearer")  // Specify your auth scheme
                          .RequireAuthenticatedUser()
                          .RequireAssertion(context => IsSuperAdmin(context, superAdminEmail)));
            });

            // Register authorization handlers
            //services.AddScoped<IAuthorizationHandler, ActiveSubscriptionHandler>();

            return services;
        }

        private static bool IsSuperAdmin(AuthorizationHandlerContext context, string superAdminEmail)
        {
            return context.User.HasClaim(c =>
                c.Type == ClaimTypes.Email &&
                c.Value.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase));
        }
    }
}
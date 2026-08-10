using Digi.Shared.DTOs.admin.module;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Digi.Shared.Middleware
{
    public class ActiveSubscriptionRequirement : IAuthorizationRequirement { }

    public class ActiveSubscriptionHandler : AuthorizationHandler<ActiveSubscriptionRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private const string SuperAdminEmail = "superadmin@gmail.com";

        public ActiveSubscriptionHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ActiveSubscriptionRequirement requirement)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (requirement == null) throw new ArgumentNullException(nameof(requirement));

            if (IsSuperAdmin(context.User))
            {
                context.Succeed(requirement);
                return;
            }

            var companyIdClaim = context.User.FindFirst("CompanyID");
            if (companyIdClaim == null || !int.TryParse(companyIdClaim.Value, out int companyId))
            {
                context.Fail();
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dapper = scope.ServiceProvider.GetRequiredService<IDapperService>();

                var subscription = await dapper.QueryFirstOrDefaultAsync<SubscriptionDto>(
                    "sp_Adm_GetCompanySubscription",
                    new { CompanyId = companyId },
                    CommandType.StoredProcedure);

                if (IsSubscriptionActive(subscription))
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            catch
            {
                context.Fail();
            }
        }

        private bool IsSuperAdmin(ClaimsPrincipal user)
        {
            return user.HasClaim(c =>
                c.Type == ClaimTypes.Email &&
                string.Equals(c.Value, SuperAdminEmail, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsSubscriptionActive(SubscriptionDto subscription)
        {
            return subscription != null &&
                   subscription.IsActive &&
                   subscription.EndDate >= DateTime.UtcNow.Date;
        }
    }
}

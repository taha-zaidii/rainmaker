using Digi.Shared.DTOs.admin.module;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Middleware
{
    public class SubscriptionCheckMiddleware
    {
        private readonly RequestDelegate _next;
       // private readonly IDapperService _dapper;

        public SubscriptionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
           // _dapper = dapper;
        }

        public async Task Invoke(HttpContext context)
        {
            // SuperAdmin ko skip karein
            if (context.User.HasClaim(c => c.Type == ClaimTypes.Email && c.Value == "digisoft@gmail.com"))
            {
                await _next(context);
                return;
            }

            if (context.User.Identity.IsAuthenticated)
            {
                var companyIdClaim = context.User.FindFirst("CompanyID");
                if (companyIdClaim != null)
                {
                    // ✅ Resolve Scoped Service here
                    using var scope = context.RequestServices.CreateScope();
                    var dapper = scope.ServiceProvider.GetRequiredService<IDapperService>();

                    var subscription = await dapper.QueryFirstOrDefaultAsync<SubscriptionDto>(
                        "sp_Adm_GetCompanySubscription",
                        new { CompanyId = int.Parse(companyIdClaim.Value) },
                        CommandType.StoredProcedure);

                    if (subscription == null || !subscription.IsActive || subscription.EndDate < DateTime.UtcNow)
                    {
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Company subscription has expired. Please renew your subscription.");
                        return;
                    }
                }
            }

            await _next(context);
        }

    }

}

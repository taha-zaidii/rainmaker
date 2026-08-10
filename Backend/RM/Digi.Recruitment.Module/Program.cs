using Digi.Recruitment.Module.Middleware;
using Digi.Shared.Helper;
using Digi.Shared.Services;
using Digi.Shared.Middleware;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Text;

// Set EPPlus license for non-commercial use (EPPlus 8+ compatible)
ExcelPackage.License.SetNonCommercialPersonal("DigiSoftERP");

// Disable wwwroot creation for Recruitment Module - only API Gateway needs wwwroot
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = null // Disable wwwroot
});
builder.Configuration.AddCentralConfiguration();

// Configure Kestrel for Recruitment Module (no wwwroot needed)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
    });
});

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "DigiSoftERP",
            ValidAudience = "DigiSoftERPUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };

        // Custom Claims Logging or Role/Permission Injection
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var identity = context.Principal.Identity as ClaimsIdentity;

                if (identity != null)
                {
                    var userId = identity.FindFirst("UserID")?.Value;
                    var userName = identity.FindFirst("UserName")?.Value;
                    var email = identity.FindFirst("Email")?.Value;
                    var companyID = identity.FindFirst("CompanyID")?.Value;
                    var employeeCode = identity.FindFirst("EmployeeCode")?.Value;
                    var role = identity.FindFirst("Role")?.Value;

                    Console.WriteLine($"✅ JWT Validated for User: {userName}, Role: {role}, CompanyID: {companyID}");
                }

                return Task.CompletedTask;
            }
        };

        options.ApplyDigiSoftErpJwtSecurityStampValidation();
    });

// Add services to the container
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<FileExtensionContentTypeProvider>();

// CORS - Centralized Configuration
builder.Services.AddCorsConfiguration(builder.Configuration);

// Add Recruitment Module services and repositories
builder.Services.AddRecruitmentModuleServices(builder.Configuration);

// Build a temporary service provider (safe before Build)
var tempProvider = builder.Services.BuildServiceProvider();
using var scope = tempProvider.CreateScope();
var dapper = scope.ServiceProvider.GetRequiredService<IDapperService>();

// Get permission list from DB (with error handling)
var permissions = new List<string>();
try
{
    permissions = (await dapper.QueryAsync<string>("sp_Adm_GetAllPermissionNames_v2", null, CommandType.StoredProcedure)).ToList();
    Console.WriteLine($"✅ Loaded {permissions.Count} permissions from database");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Warning: Could not load permissions from database: {ex.Message}");
    Console.WriteLine("⚠️ Application will continue without dynamic permission policies");
}

// Register dynamic policies BEFORE builder.Build()
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in permissions.Distinct())
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim("Permission", permission));
    }
});

// Advanced ERP: Enforce row-level self-scope for non-admin users
builder.Services.AddScoped<Digi.Shared.Filters.EnforceSelfScopeFilter>();

builder.Services.AddControllers(options =>
{
    options.Filters.AddService<Digi.Shared.Filters.EnforceSelfScopeFilter>();
})
.AddJsonOptions(option =>
{
    option.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
    option.JsonSerializerOptions.Converters.Add(new JsonTimeOnlyConverter());
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Recruitment Module API", Version = "v1" });
    opt.OperationFilter<Digi.Recruitment.Module.Middleware.SwaggerFileUploadFilter>();

    // Avoid 500 when generating swagger.json: prevent duplicate schema IDs and conflicting actions
    //opt.CustomSchemaIds(type => type.FullName?.Replace("+", ".").Replace("[]", "Array") ?? type.Name);
    //opt.ResolveConflictingActions(apiDesc => apiDesc.First());

    // Add JWT Bearer authentication in Swagger
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Map DateOnly to a custom string format in Swagger
    opt.MapType<DateOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "date",
        Example = new OpenApiString(DateTime.Today.ToString("yyyy-MM-dd"))
    });

    // Map TimeOnly to a custom string format in Swagger
    opt.MapType<TimeOnly>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "time",
        Example = new OpenApiString(DateTime.Now.TimeOfDay.ToString("hh\\:mm\\:ss"))
    });
});

var app = builder.Build();

app.UseExceptionHandler("/error");

app.Map("/error", (HttpContext context) =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var detail = exception?.Message;
    if (app.Environment.IsDevelopment() && exception != null)
        detail += "\n\n" + exception.StackTrace;
    return Results.Problem(title: "Internal Server Error", detail: detail);
});

// Swagger (always enabled so /swagger/v1/swagger.json works; restrict in production if needed)
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");
app.UseAuthentication();

// Claims transformation middleware
app.UseMiddleware<Digi.Shared.Middleware.ClaimsTransformationMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();

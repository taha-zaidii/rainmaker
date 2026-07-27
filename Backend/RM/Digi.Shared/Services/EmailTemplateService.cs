using Digi.Shared.Constants;
using Digi.Shared.DTOs.admin.module;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;

namespace Digi.Shared.Services
{
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IDapperService _dapper;
        private readonly ILogger<EmailTemplateService> _logger;

        public EmailTemplateService(IDapperService dapper, ILogger<EmailTemplateService> logger)
        {
            _dapper = dapper;
            _logger = logger;
        }

        public async Task<RenderedEmailTemplateDto> RenderAsync(
            int companyId,
            string templateCode,
            IReadOnlyDictionary<string, string> placeholders)
        {
            var builtIn = GetBuiltInTemplate(templateCode);
            var template = await TryLoadFromDatabaseAsync(companyId, templateCode) ?? builtIn;

            if (string.IsNullOrWhiteSpace(template.BodyHtml)
                || template.BodyHtml.Contains("Use built-in renderer", StringComparison.OrdinalIgnoreCase))
            {
                template.BodyHtml = builtIn.BodyHtml;
                if (string.IsNullOrWhiteSpace(template.Subject))
                    template.Subject = builtIn.Subject;
            }

            return new RenderedEmailTemplateDto
            {
                Subject = ReplacePlaceholders(template.Subject, placeholders),
                BodyHtml = ReplacePlaceholders(template.BodyHtml, placeholders),
                IsHtml = template.IsHtml
            };
        }

        private async Task<EmailTemplateDto?> TryLoadFromDatabaseAsync(int companyId, string templateCode)
        {
            try
            {
                const string sql = @"
                    SELECT TOP 1 EmailTemplateID, CompanyID, TemplateCode, TemplateName,
                           Subject, BodyHtml, IsHtml, IsActive
                    FROM dbo.Tbl_Adm_EmailTemplate
                    WHERE CompanyID = @CompanyID AND TemplateCode = @TemplateCode
                      AND IsActive = 1 AND (IsDeleted = 0 OR IsDeleted IS NULL)
                    ORDER BY EmailTemplateID DESC";

                return await _dapper.QueryFirstOrDefaultAsync<EmailTemplateDto>(
                    sql, new { CompanyID = companyId, TemplateCode = templateCode }, CommandType.Text);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Template {Code} not in DB for company {CompanyId}; using built-in.", templateCode, companyId);
                return null;
            }
        }

        private static string ReplacePlaceholders(string text, IReadOnlyDictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return Regex.Replace(text, @"\{\{(\w+)\}\}", m =>
                placeholders.TryGetValue(m.Groups[1].Value, out var v) ? v ?? "" : m.Value);
        }

        private static EmailTemplateDto GetBuiltInTemplate(string templateCode) => templateCode switch
        {
            AdminEmailTemplateCodes.UserWelcome => new EmailTemplateDto
            {
                TemplateCode = templateCode,
                Subject = "Welcome to {{CompanyName}} — Your account is ready",
                IsHtml = true,
                BodyHtml = Layout("Welcome to {{CompanyName}}",
                    @"<p>Hello {{FirstName}} {{LastName}},</p>
                      <p>Your system account has been created.</p>
                      <div class=""detail-box"">
                        <p><strong>Username:</strong> {{UserName}}</p>
                        <p><strong>Password:</strong> {{PasswordHint}}</p>
                        <p><strong>Sign in:</strong> <a href=""{{LoginUrl}}"">{{LoginUrl}}</a></p>
                      </div>
                      <p>Please sign in and change your password if prompted.</p>")
            },
            AdminEmailTemplateCodes.UserCreatedManager => new EmailTemplateDto
            {
                TemplateCode = templateCode,
                Subject = "New system account — {{FirstName}} {{LastName}} ({{EmployeeCode}})",
                IsHtml = true,
                BodyHtml = Layout("System account created",
                    @"<p>{{MessageBody}}</p><p style=""color:#666;font-size:14px;"">No password is included in this notification.</p>")
            },
            AdminEmailTemplateCodes.UserUpdatedManager => new EmailTemplateDto
            {
                TemplateCode = templateCode,
                Subject = "User account updated — {{FirstName}} {{LastName}} ({{EmployeeCode}})",
                IsHtml = true,
                BodyHtml = Layout("User account updated",
                    @"<p>{{MessageBody}}</p><p><strong>Changes:</strong> {{ChangedFields}}</p>")
            },
            AdminEmailTemplateCodes.UserEmailChanged => new EmailTemplateDto
            {
                TemplateCode = templateCode,
                Subject = "Your {{CompanyName}} account email was updated",
                IsHtml = true,
                BodyHtml = Layout("Account email updated",
                    @"<p>Hello {{FirstName}} {{LastName}},</p>
                      <p>Your login email for {{CompanyName}} has been updated.</p>
                      <p><strong>Username:</strong> {{UserName}}</p>
                      <p><a href=""{{LoginUrl}}"">Sign in</a></p>")
            },
            _ => new EmailTemplateDto
            {
                TemplateCode = templateCode,
                Subject = "{{CompanyName}} notification",
                IsHtml = true,
                BodyHtml = Layout("Notification", "<p>{{MessageBody}}</p>")
            }
        };

        private static string Layout(string heading, string inner) => $@"
<!DOCTYPE html><html><head><meta charset=""utf-8""/>
<style>
body{{font-family:'Segoe UI',Arial,sans-serif;background:#f4f6f8;margin:0;padding:24px}}
.container{{max-width:600px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)}}
.header{{background:linear-gradient(135deg,#1a5276,#2980b9);color:#fff;padding:24px 28px}}
.header h1{{margin:0;font-size:22px}}
.content{{padding:28px;color:#333;line-height:1.6}}
.detail-box{{background:#f8f9fa;border-left:4px solid #2980b9;padding:16px 20px;margin:20px 0}}
.footer{{background:#2c3e50;color:#bdc3c7;padding:20px 28px;font-size:12px;text-align:center}}
</style></head><body>
<div class=""container""><div class=""header""><h1>{heading}</h1></div>
<div class=""content"">{inner}</div>
<div class=""footer""><p><strong>{{{{CompanyName}}}}</strong></p>
<p>Automated message from DigiSoft ERP.</p></div></div></body></html>";
    }
}

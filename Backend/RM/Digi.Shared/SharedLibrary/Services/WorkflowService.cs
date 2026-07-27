using Dapper;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Digi.Shared.Services;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Digi.Shared.SharedLibrary.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IDbConnection _db;
        private readonly ICentralizedEmailService _emailService;
        public WorkflowService(IConfiguration configuration, ICentralizedEmailService emailService)
        {

            _db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            _emailService = emailService;
        }

        public async Task StartApprovalWorkflowAsync(string formType, int formId, int employeeId, int companyId, string createdBy)
        {
            int? navId = await GetNavIdByNameAsync(formType);

            if (navId == null)
                throw new Exception($"NavId not configured for form type: {formType}");

            var dto = new ApprovalFlowRequestDto
            {
                FormType = formType,
                FormId = formId,
                EmployeeId = employeeId,
                CompanyId = companyId,
                NavId = navId,
                CreatedBy = createdBy
            };

            await AssignApprovalFlowAsync(dto);
        }

        private async Task<int?> GetNavIdByNameAsync(string navName)
        {
            string sql = "SELECT NavID FROM Tbl_Adm_Nav_v2 WHERE DisplayName = @NavName AND IsActive = 1";
            return await _db.QueryFirstOrDefaultAsync<int?>(sql, new { NavName = navName });

        }

        private async Task AssignApprovalFlowAsync(ApprovalFlowRequestDto dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FormType", dto.FormType);
            parameters.Add("@FormID", dto.FormId);
            parameters.Add("@EmployeeID", dto.EmployeeId);
            parameters.Add("@CompanyID", dto.CompanyId);
            parameters.Add("@NavID", dto.NavId);
            parameters.Add("@CreatedBy", dto.CreatedBy);

            await _db.ExecuteAsync("[HRM].[sp_WF_Create_ApprovalRequest]", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> IsApprovalFlowConfiguredAsync(string formType, int companyId, int? employeeId = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FormType", formType);
            parameters.Add("@CompanyID", companyId);
            if (employeeId.HasValue)
                parameters.Add("@EmployeeID", employeeId);
            parameters.Add("@IsConfigured", dbType: DbType.Boolean, direction: ParameterDirection.Output);

            await _db.ExecuteAsync("sp_Check_ApprovalFlowConfigured", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<bool>("@IsConfigured");
        }

        public async Task<List<ApproverEmailDto>> GetApproverEmailsByWorkflowAsync(string formType, int formID, int companyID)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@FormType", formType);
                parameters.Add("@FormID", formID);
                parameters.Add("@CompanyID", companyID);

                var result = await _db.QueryAsync<ApproverEmailDto>(
                    "sp_Wf_GetApproverEmailsByWorkflow",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result.Where(e => !string.IsNullOrWhiteSpace(e.Email)).ToList();
            }
            catch (Exception)
            {
                return new List<ApproverEmailDto>();
            }
        }

        private async Task<WorkflowEmailTemplateDto?> GetWorkflowEmailTemplateAsync(string formType, string eventCode)
        {
            const string sql = @"
                SELECT TOP 1
                    EmailTemplateID,
                    FormType,
                    EventCode,
                    SubjectTemplate,
                    BodyTemplate,
                    IsHtml
                FROM dbo.Tbl_WF_EmailTemplate
                WHERE EventCode = @EventCode
                  AND IsActive = 1
                  AND IsDeleted = 0
                  AND (FormType = @FormType OR FormType IS NULL)
                ORDER BY
                    CASE WHEN FormType = @FormType THEN 0 ELSE 1 END,
                    EmailTemplateID DESC;";

            return await _db.QueryFirstOrDefaultAsync<WorkflowEmailTemplateDto>(sql, new {FormType = formType, EventCode = eventCode});
        }

        private static string ReplaceTemplateTokens(string template, Dictionary<string, object?> payload)
        {
            if (string.IsNullOrWhiteSpace(template))
                return string.Empty;

            return Regex.Replace(template, @"\{\{(.*?)\}\}", match =>
            {
                var key = match.Groups[1].Value.Trim();

                if (!payload.TryGetValue(key, out var value) || value == null)
                    return string.Empty;

                if (value is DateTime dt)
                    return dt.ToString("yyyy-MM-dd");

                return value.ToString() ?? string.Empty;
            });
        }

        private static string DecryptEmailIfNeeded(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            try
            {
                var decrypted = EncryptionHelper.DecryptText(email);

                if (!string.IsNullOrWhiteSpace(decrypted) && decrypted.Contains("@"))
                    return decrypted;

                throw new Exception($"Email decryption returned invalid value. Original value: {email}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Email decryption failed for value: {email}. Error: {ex.Message}", ex);
            }
        }

        public async Task<WorkflowEmailDispatchResultDto> SendWorkflowEventEmailsAsync(string formType, int formId, int companyId, string triggerEvent, int? workflowId = null, string? actionType = null, string? remarks = null)
        {
            var result = new WorkflowEmailDispatchResultDto();

            var rules = await GetEmailNotificationRulesAsync(formType, companyId, triggerEvent);

            if (rules.Count == 0)
            {
                result.Messages.Add($"No email rules configured for TriggerEvent={triggerEvent}, FormType={formType}");
                return result;
            }

            var payload = await GetGenericWorkflowEmailPayloadAsync(
                formType,
                formId,
                companyId,
                workflowId);

            if (payload.Count == 0)
            {
                result.Messages.Add($"No email payload found for FormType={formType}, FormID={formId}");
                return result;
            }

            payload["TriggerEvent"] = triggerEvent;
            payload["ActionType"] = actionType ?? string.Empty;
            payload["Remarks"] = remarks ?? payload.GetValueOrDefault("ActionRemarks")?.ToString() ?? string.Empty;

            await FlattenRequestDataJsonIntoPayloadAsync(payload, formType, companyId);

            foreach (var rule in rules.OrderBy(x => x.SendOrder))
            {
                result.Attempted++;

                var template = await GetWorkflowEmailTemplateAsync(formType, rule.TemplateEventCode);

                if (template == null)
                {
                    result.Messages.Add($"Template missing. FormType={formType}, EventCode={rule.TemplateEventCode}");
                    continue;
                }

                var toEmailRaw = ResolveRecipientEmail(rule.RecipientType, payload);
                var toName = ResolveRecipientName(rule.RecipientType, payload);

                if (string.IsNullOrWhiteSpace(toEmailRaw))
                {
                    result.Messages.Add($"No recipient for RecipientType={rule.RecipientType}, TriggerEvent={triggerEvent}");
                    continue;
                }

                var toEmail = DecryptEmailIfNeeded(toEmailRaw);

                if (string.IsNullOrWhiteSpace(toEmail) || !toEmail.Contains("@"))
                {
                    result.Messages.Add($"Invalid recipient email for RecipientType={rule.RecipientType}");
                    continue;
                }

                payload["RecipientName"] = toName ?? string.Empty;
                payload["RecipientEmail"] = toEmail;

                var subject = ReplaceTemplateTokens(template.SubjectTemplate, payload);
                var body = ReplaceTemplateTokens(template.BodyTemplate, payload);

                var emailResult = await _emailService.SendEmailAsync(
                    companyId,
                    toEmail,
                    subject,
                    body,
                    template.IsHtml);

                if (emailResult != null && emailResult.IsSuccess)
                {
                    result.Sent++;
                    result.Messages.Add($"Sent {rule.TemplateEventCode} to {rule.RecipientType}: {toEmail}");
                }
                else
                {
                    result.Messages.Add($"Failed {rule.TemplateEventCode} to {toEmail}: {emailResult?.Message}");
                }
            }

            return result;
        }

        private async Task<List<WorkflowEmailRuleDto>> GetEmailNotificationRulesAsync(string formType, int companyId, string triggerEvent)
        {
            const string sql = @"
                SELECT
                    RuleID,
                    TriggerEvent,
                    RecipientType,
                    TemplateEventCode,
                    SendOrder
                FROM dbo.Tbl_WF_EmailNotificationRule
                WHERE TriggerEvent = @TriggerEvent
                  AND IsActive = 1
                  AND IsDeleted = 0
                  AND (CompanyID = @CompanyID OR CompanyID IS NULL)
                  AND (FormType = @FormType OR FormType IS NULL)
                ORDER BY
                    CASE WHEN CompanyID = @CompanyID THEN 0 ELSE 1 END,
                    CASE WHEN FormType = @FormType THEN 0 ELSE 1 END,
                    SendOrder;";

            var rows = await _db.QueryAsync<WorkflowEmailRuleDto>(
                sql,
                new
                {
                    TriggerEvent = triggerEvent,
                    FormType = formType,
                    CompanyID = companyId
                });

            return rows.ToList();
        }

        private async Task<Dictionary<string, object?>> GetGenericWorkflowEmailPayloadAsync(string formType, int formId, int companyId, int? workflowId)
        {
            var row = await _db.QueryFirstOrDefaultAsync(
                "dbo.sp_WF_EmailPayload_Generic",
                new
                {
                    FormType = formType,
                    FormID = formId,
                    CompanyID = companyId,
                    WorkflowID = workflowId
                },
                commandType: CommandType.StoredProcedure);

            if (row == null)
                return new Dictionary<string, object?>();

            var dictionary = (IDictionary<string, object?>)row;

            return dictionary.ToDictionary(
                x => x.Key,
                x => x.Value == DBNull.Value ? null : x.Value,
                StringComparer.OrdinalIgnoreCase);
        }

        private static string? ResolveRecipientEmail(string recipientType, Dictionary<string, object?> payload)
        {
            return recipientType.ToUpperInvariant() switch
            {
                "REQUESTER" => payload.GetValueOrDefault("RequesterEmail")?.ToString(),
                "CURRENT_APPROVER" => payload.GetValueOrDefault("CurrentApproverEmail")?.ToString(),
                "ACTION_BY" => payload.GetValueOrDefault("ActionByEmail")?.ToString(),
                _ => null
            };
        }

        private static string? ResolveRecipientName(string recipientType, Dictionary<string, object?> payload)
        {
            return recipientType.ToUpperInvariant() switch
            {
                "REQUESTER" => payload.GetValueOrDefault("EmployeeName")?.ToString(),
                "CURRENT_APPROVER" => payload.GetValueOrDefault("CurrentApproverName")?.ToString(),
                "ACTION_BY" => payload.GetValueOrDefault("ActionByName")?.ToString(),
                _ => null
            };
        }

        private async Task FlattenRequestDataJsonIntoPayloadAsync(Dictionary<string, object?> payload, string formType, int companyId)
        {
            if (!payload.TryGetValue("RequestDataJson", out var jsonObj) || jsonObj == null)
                return;

            var json = jsonObj.ToString();

            if (string.IsNullOrWhiteSpace(json))
                return;

            using var document = System.Text.Json.JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
                return;

            var fieldConfigs = await GetEmailFieldConfigsAsync(formType, companyId);
            var hasFieldConfig = fieldConfigs.Any();

            payload["DetailsHtml"] = hasFieldConfig
                ? await BuildConfiguredDetailsHtmlAsync(document.RootElement, fieldConfigs, companyId, payload)
                : BuildJsonObjectDetailsHtml(document.RootElement);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var child in property.Value.EnumerateObject())
                    {
                        AddJsonToken(payload, child.Name, child.Value);
                        AddJsonToken(payload, $"{property.Name}_{child.Name}", child.Value);
                    }
                }
                else if (property.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var htmlKey = property.Name + "Html";

                    if (!(hasFieldConfig && htmlKey.Equals("DetailsHtml", StringComparison.OrdinalIgnoreCase)))
                    {
                        payload[htmlKey] = BuildJsonArrayHtmlTable(property.Value);
                    }
                }
                else
                {
                    AddJsonToken(payload, property.Name, property.Value);
                }
            }
        }

        private async Task<List<WorkflowEmailFieldConfigDto>> GetEmailFieldConfigsAsync(string formType, int companyId)
        {
            const string sql = @"
                SELECT
                    FieldConfigID,
                    FormType,
                    JsonPath,
                    DisplayLabel,
                    DisplayOrder,
                    TokenName,
                    LookupTableName,
                    LookupKeyColumn,
                    LookupValueColumn,
                    LookupCompanyColumn
                FROM dbo.Tbl_WF_EmailFieldConfig
                WHERE FormType = @FormType
                  AND ShowInEmail = 1
                  AND IsActive = 1
                  AND IsDeleted = 0
                  AND (CompanyID = @CompanyID OR CompanyID IS NULL)
                ORDER BY
                    CASE WHEN CompanyID = @CompanyID THEN 0 ELSE 1 END,
                    DisplayOrder;";

            var rows = await _db.QueryAsync<WorkflowEmailFieldConfigDto>(
                sql,
                new
                {
                    FormType = formType,
                    CompanyID = companyId
                });

            return rows.ToList();
        }

        private async Task<string> BuildConfiguredDetailsHtmlAsync(System.Text.Json.JsonElement root, List<WorkflowEmailFieldConfigDto> configs, int companyId, Dictionary<string, object?> payload)
        {
            var html = new StringBuilder();

            // 1. Root fields like TotalAmount, FromDate, ToDate
            var rootRows = new StringBuilder();

            foreach (var config in configs.Where(x => !IsArrayJsonPath(root, x.JsonPath)).OrderBy(x => x.DisplayOrder))
            {
                if (!TryGetJsonValue(root, config.JsonPath, out var valueElement))
                    continue;

                var displayValue = await ResolveDisplayValueAsync(config, valueElement, companyId);

                if (string.IsNullOrWhiteSpace(displayValue))
                    continue;

                if (!string.IsNullOrWhiteSpace(config.TokenName))
                    payload[config.TokenName] = displayValue;

                var label = System.Net.WebUtility.HtmlEncode(config.DisplayLabel);
                var safeValue = System.Net.WebUtility.HtmlEncode(displayValue);

                rootRows.Append($@"
                    <tr>
                        <td style=""padding:6px 10px;border-bottom:1px solid #eee;""><b>{label}</b></td>
                        <td style=""padding:6px 10px;border-bottom:1px solid #eee;"">{safeValue}</td>
                    </tr>"
                );
            }

            if (rootRows.Length > 0)
            {
                html.Append($@"
                    <table style=""width:100%;border-collapse:collapse;margin-bottom:14px;"">
                        {rootRows}
                    </table>"
                );
            }

            // 2. Array fields like Details.RequestDate, Details.ClaimTypeID
            var arrayGroups = configs
                .Where(x => IsArrayJsonPath(root, x.JsonPath))
                .GroupBy(x => x.JsonPath.Split('.')[0]);

            foreach (var group in arrayGroups)
            {
                var arrayName = group.Key;

                if (!root.TryGetProperty(arrayName, out var arrayElement) ||
                    arrayElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                    continue;

                var columns = group.OrderBy(x => x.DisplayOrder).ToList();

                if (!columns.Any())
                    continue;

                var table = new StringBuilder();

                table.Append(@"
                    <table style=""width:100%;border-collapse:collapse;margin-top:10px;"">
                    <thead>
                    <tr>"
                );

                foreach (var col in columns)
                {
                    var label = System.Net.WebUtility.HtmlEncode(col.DisplayLabel);
                    table.Append($@"
                        <th style=""text-align:left;padding:8px 10px;border-bottom:2px solid #ddd;background:#f8f9fa;"">{label}</th>"
                    );
                }

                table.Append(@"
                    </tr>
                    </thead>
                    <tbody>"
                );

                var tokenValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in arrayElement.EnumerateArray())
                {
                    if (item.ValueKind != System.Text.Json.JsonValueKind.Object)
                        continue;

                    table.Append("<tr>");

                    foreach (var col in columns)
                    {
                        var relativePath = col.JsonPath.Substring(arrayName.Length + 1);

                        string displayValue = "";

                        if (TryGetJsonValue(item, relativePath, out var valueElement))
                        {
                            displayValue = await ResolveDisplayValueAsync(col, valueElement, companyId) ?? "";
                        }

                        if (!string.IsNullOrWhiteSpace(col.TokenName) && !string.IsNullOrWhiteSpace(displayValue))
                        {
                            if (!tokenValues.ContainsKey(col.TokenName))
                                tokenValues[col.TokenName] = new List<string>();

                            tokenValues[col.TokenName].Add(displayValue);
                        }

                        var safeValue = System.Net.WebUtility.HtmlEncode(displayValue);

                        table.Append($@"
                            <td style=""padding:8px 10px;border-bottom:1px solid #eee;"">{safeValue}</td>"
                        );
                    }

                    table.Append("</tr>");
                }

                table.Append(@"
                    </tbody>
                    </table>"
                );

                foreach (var token in tokenValues)
                {
                    payload[token.Key] = string.Join(", ", token.Value.Distinct());
                }

                html.Append($@"
                    <div style=""margin-top:14px;"">
                        <div style=""font-weight:600;color:#374151;margin-bottom:6px;"">{System.Net.WebUtility.HtmlEncode(ToDisplayLabel(arrayName))}</div>
                        {table}
                    </div>"
                );
            }

            return html.ToString();
        }

        private static bool TryGetJsonValue(System.Text.Json.JsonElement root, string jsonPath, out System.Text.Json.JsonElement value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(jsonPath))
                return false;

            var parts = jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

            var current = root;

            foreach (var part in parts)
            {
                if (current.ValueKind != System.Text.Json.JsonValueKind.Object)
                    return false;

                if (!current.TryGetProperty(part, out current))
                    return false;
            }

            value = current;
            return true;
        }

        private static bool IsArrayJsonPath(System.Text.Json.JsonElement root, string jsonPath)
        {
            if (string.IsNullOrWhiteSpace(jsonPath))
                return false;

            var parts = jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
                return false;

            return root.TryGetProperty(parts[0], out var firstElement)
                && firstElement.ValueKind == System.Text.Json.JsonValueKind.Array;
        }

        private async Task<string?> ResolveDisplayValueAsync(WorkflowEmailFieldConfigDto config, System.Text.Json.JsonElement valueElement, int companyId)
        {
            var rawValue = valueElement.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => valueElement.GetString(),
                System.Text.Json.JsonValueKind.Number => valueElement.ToString(),
                System.Text.Json.JsonValueKind.True => "Yes",
                System.Text.Json.JsonValueKind.False => "No",
                System.Text.Json.JsonValueKind.Null => "",
                _ => valueElement.ToString()
            };

            if (string.IsNullOrWhiteSpace(rawValue))
                return string.Empty;

            bool hasLookup =
                !string.IsNullOrWhiteSpace(config.LookupTableName) &&
                !string.IsNullOrWhiteSpace(config.LookupKeyColumn) &&
                !string.IsNullOrWhiteSpace(config.LookupValueColumn);

            if (!hasLookup)
                return rawValue;

            var tableName = QuoteMultipartName(config.LookupTableName!);
            var keyColumn = QuoteName(config.LookupKeyColumn!);
            var valueColumn = QuoteName(config.LookupValueColumn!);

            var companyFilter = "";

            if (!string.IsNullOrWhiteSpace(config.LookupCompanyColumn))
            {
                companyFilter = $" AND {QuoteName(config.LookupCompanyColumn)} = @CompanyID";
            }

            var sql = $@"SELECT TOP 1 CAST({valueColumn} AS NVARCHAR(MAX)) FROM {tableName} WHERE {keyColumn} = @LookupValue {companyFilter};";

            var lookupValue = await _db.QueryFirstOrDefaultAsync<string>(sql, new {LookupValue = rawValue, CompanyID = companyId});

            return string.IsNullOrWhiteSpace(lookupValue) ? rawValue : lookupValue;
        }

        private static string QuoteName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SQL name cannot be empty.");

            return "[" + name.Replace("]", "]]") + "]";
        }

        private static string QuoteMultipartName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("SQL object name cannot be empty.");

            return string.Join(".", name.Split('.').Select(QuoteName));
        }

        private static string BuildJsonObjectDetailsHtml(System.Text.Json.JsonElement root)
        {
            var excludedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "IsActive",
                "IsDeleted",
                "IsDeletedOn",
                "CreatedBy",
                "UpdatedBy",
                "UpdatedOn",
                "CompanyID",
                "EmployeeID"
            };

            var rows = new StringBuilder();

            foreach (var prop in root.EnumerateObject())
            {
                if (excludedKeys.Contains(prop.Name))
                    continue;

                if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Object || prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                    continue;

                var value = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                    System.Text.Json.JsonValueKind.Number => prop.Value.ToString(),
                    System.Text.Json.JsonValueKind.True => "Yes",
                    System.Text.Json.JsonValueKind.False => "No",
                    System.Text.Json.JsonValueKind.Null => "",
                    _ => prop.Value.ToString()
                };

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var label = System.Net.WebUtility.HtmlEncode(ToDisplayLabel(prop.Name));
                var safeValue = System.Net.WebUtility.HtmlEncode(value);

                rows.Append($@"
                    <tr>
                        <td style=""padding:6px 10px;border-bottom:1px solid #eee;""><b>{label}</b></td>
                        <td style=""padding:6px 10px;border-bottom:1px solid #eee;"">{safeValue}</td>
                    </tr>"
                );
            }

            if (rows.Length == 0)
                return string.Empty;

            return $@"<table style=""width:100%;border-collapse:collapse;"">{rows}</table>";
        }

        private static string ToDisplayLabel(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return string.Empty;

            var result = Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
            result = result.Replace("_", " ");
            return result;
        }

        private static void AddJsonToken(Dictionary<string, object?> payload, string key, System.Text.Json.JsonElement value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            object? finalValue = value.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => value.GetString(),
                System.Text.Json.JsonValueKind.Number => value.ToString(),
                System.Text.Json.JsonValueKind.True => "Yes",
                System.Text.Json.JsonValueKind.False => "No",
                System.Text.Json.JsonValueKind.Null => "",
                _ => value.ToString()
            };

            if (!payload.ContainsKey(key))
                payload[key] = finalValue;

            payload["Data_" + key] = finalValue;
        }

        private static string BuildJsonArrayHtmlTable(System.Text.Json.JsonElement arrayElement)
        {
            var rows = new StringBuilder();

            foreach (var item in arrayElement.EnumerateArray())
            {
                if (item.ValueKind != System.Text.Json.JsonValueKind.Object)
                    continue;

                foreach (var prop in item.EnumerateObject())
                {
                    var label = System.Net.WebUtility.HtmlEncode(prop.Name);
                    var value = System.Net.WebUtility.HtmlEncode(prop.Value.ToString());

                    rows.Append($@"
                        <tr>
                            <td style=""padding:6px 10px;border-bottom:1px solid #eee;""><b>{label}</b></td>
                            <td style=""padding:6px 10px;border-bottom:1px solid #eee;"">{value}</td>
                        </tr>"
                    );
                }
            }

            if (rows.Length == 0)
                return string.Empty;

            return $@"<table style=""width:100%;border-collapse:collapse;"">{rows}</table>";
        }

        private sealed class WorkflowEmailFieldConfigDto
        {
            public int FieldConfigID { get; set; }
            public string FormType { get; set; } = string.Empty;
            public string JsonPath { get; set; } = string.Empty;
            public string DisplayLabel { get; set; } = string.Empty;
            public int DisplayOrder { get; set; }
            public string? TokenName { get; set; }

            public string? LookupTableName { get; set; }
            public string? LookupKeyColumn { get; set; }
            public string? LookupValueColumn { get; set; }
            public string? LookupCompanyColumn { get; set; }
        }

        private sealed class WorkflowEmailTemplateDto
        {
            public int EmailTemplateID { get; set; }
            public string FormType { get; set; } = string.Empty;
            public string EventCode { get; set; } = string.Empty;
            public string SubjectTemplate { get; set; } = string.Empty;
            public string BodyTemplate { get; set; } = string.Empty;
            public bool IsHtml { get; set; }
        }

        private sealed class WorkflowEmailRuleDto
        {
            public int RuleID { get; set; }
            public string TriggerEvent { get; set; } = string.Empty;
            public string RecipientType { get; set; } = string.Empty;
            public string TemplateEventCode { get; set; } = string.Empty;
            public int SendOrder { get; set; }
        }
    }
}
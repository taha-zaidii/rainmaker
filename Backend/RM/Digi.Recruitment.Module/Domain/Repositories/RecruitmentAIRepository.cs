using Dapper;
using Digi.Recruitment.Module.Domain.Repositories.IRepositories;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Digi.Recruitment.Module.Domain.Repositories
{
    public class RecruitmentAIRepository : IRecruitmentAIRepository
    {
        private readonly IDbConnection _db;
        private readonly ILogger<RecruitmentAIRepository> _logger;
        private readonly IRecruitmentRepository _recruitmentRepository;

        public RecruitmentAIRepository(IDbConnection db, ILogger<RecruitmentAIRepository> logger, IRecruitmentRepository recruitmentRepository)
        {
            _db = db;
            _logger = logger;
            _recruitmentRepository = recruitmentRepository;
        }

        public async Task<ApiKeyStatusResponseDto?> GetApiKeyStatusAsync(int companyId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var sql = @"
                    SELECT TOP 1 
                        Provider,
                        CASE WHEN ApiKey IS NOT NULL AND ApiKey != '' THEN 1 ELSE 0 END AS HasApiKey,
                        1 AS IsValid
                    FROM Tbl_Ruc_RecruitmentAI_Settings
                    WHERE CompanyID = @CompanyID AND IsActive = 1";

                var result = await _db.QueryFirstOrDefaultAsync<ApiKeyStatusResponseDto>(sql, new { CompanyID = companyId });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key status for CompanyID: {CompanyID}", companyId);
                return null;
            }
        }

        public async Task<ApiKeySettingsResponseDto?> GetApiKeySettingsAsync(int companyId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var sql = @"
                    SELECT TOP 1
                        ApiKey,
                        Provider,
                        ApiEndpoint,
                        Model,
                        MaxTokens,
                        Temperature,
                        AutoScreening,
                        AutoMatching,
                        AutoParse,
                        AutoShortlistThreshold,
                        GenerateQuestions,
                        EmailNotifications,
                        CreatedOn,
                        UpdatedOn
                    FROM Tbl_Ruc_RecruitmentAI_Settings
                    WHERE CompanyID = @CompanyID AND IsActive = 1";

                var result = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { CompanyID = companyId });
                
                if (result == null)
                    return null;

                return new ApiKeySettingsResponseDto
                {
                    ApiKey = result.ApiKey?.ToString(), // Masked for display
                    Provider = result.Provider?.ToString() ?? "",
                    ApiEndpoint = result.ApiEndpoint?.ToString(),
                    Model = result.Model?.ToString(),
                    MaxTokens = result.MaxTokens ?? 1000,
                    Temperature = result.Temperature ?? 0.7m,
                    AutoShortlistThreshold = result.AutoShortlistThreshold ?? 40,
                    Settings = new FeatureSettingsDto
                    {
                        AutoScreening = result.AutoScreening ?? false,
                        AutoMatching = result.AutoMatching ?? false,
                        GenerateQuestions = result.GenerateQuestions ?? false,
                        EmailNotifications = result.EmailNotifications ?? false,
                        AutoParse = result.AutoParse ?? false
                    },
                    CreatedOn = result.CreatedOn,
                    UpdatedOn = result.UpdatedOn
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key settings for CompanyID: {CompanyID}", companyId);
                return null;
            }
        }

        public async Task<string?> GetEncryptedApiKeyAsync(int companyId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var sql = @"
                    SELECT TOP 1 ApiKey
                    FROM Tbl_Ruc_RecruitmentAI_Settings
                    WHERE CompanyID = @CompanyID AND IsActive = 1";

                var result = await _db.QueryFirstOrDefaultAsync<string>(sql, new { CompanyID = companyId });
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting encrypted API key for CompanyID: {CompanyID}", companyId);
                return null;
            }
        }

        public async Task<(int? Id, bool IsSuccess, string Message)> SaveApiKeySettingsAsync(SaveApiKeySettingsRequestDto request, string userId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Encrypt API key before storing
                var encryptedApiKey = EncryptionHelper.EncryptText(request.ApiKey);

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyId);
                parameters.Add("@Provider", request.Provider);
                parameters.Add("@ApiKey", encryptedApiKey);
                parameters.Add("@ApiEndpoint", request.ApiEndpoint);
                parameters.Add("@Model", request.Model);
                parameters.Add("@MaxTokens", request.MaxTokens);
                parameters.Add("@Temperature", request.Temperature);
                parameters.Add("@AutoScreening", request.Settings?.AutoScreening ?? false);
                parameters.Add("@AutoMatching", request.Settings?.AutoMatching ?? false);
                parameters.Add("@GenerateQuestions", request.Settings?.GenerateQuestions ?? false);
                parameters.Add("@EmailNotifications", request.Settings?.EmailNotifications ?? true);
                parameters.Add("@AutoParse", request.Settings?.AutoParse ?? false);
                parameters.Add("@AutoShortlistThreshold", request.Settings?.AutoShortlistThreshold ?? 80);
                parameters.Add("@CreatedBy", userId);
                parameters.Add("@UpdatedBy", userId);
                parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "ruc.SP_Ruc_RecruitmentAI_Settings_Save",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var id = parameters.Get<int?>("@Id");
                return (id, true, "API key settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API key settings for CompanyID: {CompanyID}", request.CompanyId);
                return (null, false, $"Error saving API key settings: {ex.Message}");
            }
        }

        public async Task<bool> DeleteApiKeyAsync(int companyId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyId);
                parameters.Add("@RowsAffected", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "ruc.SP_Ruc_RecruitmentAI_Settings_Delete",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var rowsAffected = parameters.Get<int>("@RowsAffected");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key for CompanyID: {CompanyID}", companyId);
                return false;
            }
        }

        public async Task<DashboardStatsResponseDto?> GetDashboardStatsAsync(int companyId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var statsSql = @"
                    SELECT
                        TotalRequisitions   = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_Ruc_RecruitmentRequisition WHERE CompanyID = @CompanyID AND IsActive = 1), 0),
                        ActiveRequisitions  = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_Ruc_RecruitmentRequisition WHERE CompanyID = @CompanyID AND IsActive = 1), 0),
                        TotalApplications   = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_Ruc_JobApplication WHERE CompanyID = @CompanyID AND IsActive = 1), 0),
                        InterviewsScheduled = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_Ruc_JobApplication WHERE CompanyID = @CompanyID AND IsActive = 1 AND (CurrentStatusID = 3 OR StatusCode = 'INTERVIEW')), 0),
                        HiredCount          = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_Ruc_JobApplication WHERE CompanyID = @CompanyID AND IsActive = 1 AND (CurrentStatusID = 4 OR StatusCode = 'HIRED')), 0),
                        PendingEvaluations  = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_Ruc_JobApplication WHERE CompanyID = @CompanyID AND IsActive = 1 AND (CurrentStatusID = 1 OR StatusCode = 'APPLIED')), 0),
                        TotalJobsAnalyzed   = ISNULL((SELECT COUNT(DISTINCT RequisitionID) FROM dbo.Tbl_Ruc_RecruitmentRequisition WHERE CompanyID = @CompanyID AND IsActive = 1), 0),
                        ResumesScreened     = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_RecruitmentAI_Screening WHERE CompanyID = @CompanyID), 0),
                        CandidatesMatched   = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_RecruitmentAI_Screening WHERE CompanyID = @CompanyID AND MatchScore >= 70), 0),
                        TimeSaved           = ISNULL((SELECT COUNT(*) FROM dbo.Tbl_RecruitmentAI_Screening WHERE CompanyID = @CompanyID) * 2, 0)";

                var stats = await _db.QueryFirstOrDefaultAsync<DashboardStatsDto>(statsSql, new { CompanyID = companyId });

                var activitySql = @"
                    SELECT TOP 10
                        Id           = ap.ApplicationID,
                        ActivityType = 'resume_parsing',
                        Title        = 'Candidate Application',
                        Description  = 'Candidate ' + ISNULL(a.FirstName + ' ' + ISNULL(a.LastName, ''), 'Applicant') + ' applied for ' + ISNULL(r.JobTitle, 'Job Requisition'),
                        RelatedId    = ap.ApplicationID,
                        CreatedOn    = ISNULL(ap.ApplicationDate, GETDATE())
                    FROM dbo.Tbl_Ruc_JobApplication ap
                    LEFT JOIN dbo.Tbl_Ruc_Applicant a ON a.ApplicantID = ap.ApplicantID
                    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition r ON r.RequisitionID = ap.RequisitionID
                    WHERE ap.CompanyID = @CompanyID AND ap.IsActive = 1
                    ORDER BY ap.ApplicationID DESC";

                var activities = await _db.QueryAsync<RecentActivityDto>(activitySql, new { CompanyID = companyId });

                return new DashboardStatsResponseDto
                {
                    Stats = stats ?? new DashboardStatsDto(),
                    RecentActivity = activities.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats for CompanyID: {CompanyID}", companyId);
                return null;
            }
        }


        public async Task<(int? Id, bool IsSuccess, string Message)> SaveJobDescriptionAsync(int companyId, int? jobRequisitionId, string generatedDescription, string promptUsed, string model, int tokensUsed, string userId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyId);
                parameters.Add("@JobRequisitionID", jobRequisitionId);
                parameters.Add("@GeneratedDescription", generatedDescription);
                parameters.Add("@PromptUsed", promptUsed);
                parameters.Add("@Model", model);
                parameters.Add("@TokensUsed", tokensUsed);
                parameters.Add("@CreatedBy", userId);
                parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "ruc.SP_Ruc_RecruitmentAI_JobDescriptions_Save",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var id = parameters.Get<int>("@Id");
                return (id, true, "Job description saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job description for CompanyID: {CompanyID}", companyId);
                return (null, false, $"Error saving job description: {ex.Message}");
            }
        }

        public async Task<(int? Id, bool IsUpdate, bool IsSuccess, string Message, int? JobRequisitionId)> SaveJobDescriptionWithUpdateAsync(SaveJobDescriptionRequestDto request, string userId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                bool isUpdate = false;
                int? jobDescId = null;
                int? jobRequisitionId = null;

                // Step 1: Find existing Job Description record (may have null JobRequisitionID)
                var existingJobDescSql = @"
                    SELECT TOP 1 Id, JobRequisitionID
                    FROM Tbl_Ruc_RecruitmentAI_JobDescriptions
                    WHERE CompanyID = @CompanyID
                    ORDER BY CreatedOn DESC";

                var existingJobDesc = await _db.QueryFirstOrDefaultAsync<(int Id, int? JobRequisitionID)?>(existingJobDescSql, new 
                { 
                    CompanyID = request.CompanyId
                });

                if (existingJobDesc.HasValue)
                {
                    // UPDATE existing record - only update JobRequisitionID
                    isUpdate = true;
                    jobDescId = existingJobDesc.Value.Id;
                    
                    // Step 2: Always CREATE new Job Requisition (never update existing)
                    jobRequisitionId = await CreateJobRequisitionFromRequest(request, userId);
                    if (!jobRequisitionId.HasValue)
                    {
                        return (null, isUpdate, false, "Failed to create job requisition", null);
                    }

                    // Step 3: Update Tbl_Ruc_RecruitmentAI_JobDescriptions - only JobRequisitionID
                    var parameters = new DynamicParameters();
                    parameters.Add("@Id", jobDescId.Value);
                    parameters.Add("@CompanyID", request.CompanyId);
                    parameters.Add("@JobRequisitionID", jobRequisitionId.Value);
                    parameters.Add("@UpdatedBy", userId);

                    var rowsAffected = await _db.ExecuteAsync(
                        "ruc.SP_Ruc_RecruitmentAI_JobDescriptions_Update",
                        parameters,
                        commandType: CommandType.StoredProcedure);
                }
                else
                {
                    // No existing record found - this should not happen, but handle it
                    return (null, false, false, "No existing job description found to update", null);
                }

                // Step 4: Update Tbl_Ruc_RecruitmentAI_Activity - update RelatedId
                await UpdateActivityRelatedId(request.CompanyId, jobRequisitionId.Value, "job_description");

                return (jobDescId, isUpdate, true, "Job description linked to requisition successfully", jobRequisitionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job description with update for CompanyID: {CompanyID}", request.CompanyId);
                return (null, false, false, $"Error saving job description: {ex.Message}", request.JobRequisitionId);
            }
        }

        private async Task UpdateActivityRelatedId(int companyId, int relatedId, string activityType)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyId);
                parameters.Add("@ActivityType", activityType);
                parameters.Add("@RelatedId", relatedId);

                await _db.ExecuteAsync(
                    "ruc.SP_Ruc_RecruitmentAI_Activity_UpdateRelatedId",
                    parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating activity RelatedId for CompanyID: {CompanyID}, RelatedId: {RelatedId}", companyId, relatedId);
                // Don't throw - this is not critical
            }
        }

        private async Task<int?> CreateJobRequisitionFromRequest(SaveJobDescriptionRequestDto request, string userId)
        {
            try
            {
                var keyResponsibilities = !string.IsNullOrWhiteSpace(request.KeyResponsibilities)
                    ? request.KeyResponsibilities
                    : ExtractResponsibilities(request.JobDescription);

                var skills = !string.IsNullOrWhiteSpace(request.Skills)
                    ? request.Skills
                    : ExtractSkills(request.JobDescription, request.Skills);

                var experience = !string.IsNullOrWhiteSpace(request.Experience)
                    ? request.Experience
                    : ExtractExperience(request.JobDescription, request.Experience)?.ToString();

                int.TryParse(experience, out var expYears);

                var qualifications = !string.IsNullOrWhiteSpace(request.Qualifications)
                    ? request.Qualifications
                    : ExtractQualifications(request.JobDescription);

                var benefits = !string.IsNullOrWhiteSpace(request.Benefits)
                    ? request.Benefits
                    : ExtractBenefits(request.JobDescription);

                var additionalInfo = !string.IsNullOrWhiteSpace(request.AdditionalInfo)
                    ? request.AdditionalInfo
                    : ExtractAdditionalInfo(request.JobDescription, request.AdditionalInfo);

                var jobSummary = !string.IsNullOrWhiteSpace(request.JobSummary)
                    ? request.JobSummary
                    : ExtractJobSummary(request.JobDescription);

                var requisitionRequest = new SaveRecruitmentRequisitionRequest
                {
                    Action = "INSERT",
                    CompanyID = request.CompanyId,
                    RecruitmentRequisitionName = request.JobTitle ?? "AI Generated Job Position",
                    KeyResponsibilities = keyResponsibilities,
                    SkillsRequired = skills,
                    ExperienceYears = expYears > 0 ? expYears : null,
                    QualificationsEntryRequirments = qualifications,

                    Comments = additionalInfo,
                    OtherRequirments = benefits,
                    JobSummary = jobSummary,
                    Justification = request.Justification,
                    Location = request.Location,
                    Vacancies = request.Vacancies ?? 1,
                    MinSalary = request.MinSalary,
                    MaxSalary = request.MaxSalary,
                    EmployeeCode = userId,
                    IsSystemDefault = true,
                    ApprovalStatus = "Pending",
                    PublishStatus = request.IsPublished == 1 ? "Published" : "Pending",
                    PublishedDate = DateTime.UtcNow,
                    PublishedBy = userId,
                    RecruitmentRequisitionDate = DateTime.UtcNow,
                    RecruitmentRequisitionClosingDate = request.ClosingDate ?? DateTime.UtcNow.AddDays(30),
                    AlwaysPublished = request.IsPublished == 1,
                    IsClosed = false
                };


                // Call Recruitment Repository directly to avoid circular dependency
                var (newId, isSuccess, message, row) = await _recruitmentRepository.SaveAsync(requisitionRequest);

                if (!isSuccess || !newId.HasValue)
                {
                    _logger.LogError("Failed to create job requisition: {Message}", message);
                    return null;
                }

                _logger.LogInformation("Successfully created new Job Requisition ID: {RequisitionID} for CompanyID: {CompanyID}", newId.Value, request.CompanyId);
                return newId.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job requisition from request");
                return null;
            }
        }

        private string? ExtractJobSummary(string jobDescription)
        {
            var lines = jobDescription.Split('\n');
            var summary = new List<string>();
            bool inSummarySection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Job Summary", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Summary", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Overview", StringComparison.OrdinalIgnoreCase))
                {
                    inSummarySection = true;
                    continue;
                }

                if (inSummarySection)
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine) ||
                        trimmedLine.StartsWith("Responsibilities", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Requirements", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Qualifications", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (trimmedLine.Length > 10)
                    {
                        summary.Add(trimmedLine);
                    }
                }
            }

            return summary.Count > 0 ? string.Join("\n", summary) : null;
        }

        private string BuildPromptFromRequest(SaveJobDescriptionRequestDto request)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Generate job description for:");
            if (!string.IsNullOrWhiteSpace(request.JobTitle))
                sb.AppendLine($"Title: {request.JobTitle}");
            if (!string.IsNullOrWhiteSpace(request.Department))
                sb.AppendLine($"Department: {request.Department}");
            if (!string.IsNullOrWhiteSpace(request.Experience))
                sb.AppendLine($"Experience: {request.Experience}");
            if (!string.IsNullOrWhiteSpace(request.Skills))
                sb.AppendLine($"Skills: {request.Skills}");
            if (!string.IsNullOrWhiteSpace(request.AdditionalInfo))
                sb.AppendLine($"Additional Info: {request.AdditionalInfo}");
            return sb.ToString();
        }


        private string ExtractResponsibilities(string jobDescription)
        {
            if (string.IsNullOrWhiteSpace(jobDescription))
                return string.Empty;

            var lines = jobDescription.Split('\n');
            var responsibilities = new List<string>();
            bool inResponsibilitiesSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Key Responsibilities", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Responsibilities", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Duties", StringComparison.OrdinalIgnoreCase))
                {
                    inResponsibilitiesSection = true;
                    continue;
                }

                if (inResponsibilitiesSection)
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine) ||
                        trimmedLine.StartsWith("Required", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Qualifications", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Education", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Skills", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•") || trimmedLine.StartsWith("*") || trimmedLine.Length > 20)
                    {
                        responsibilities.Add(trimmedLine.TrimStart('-', '•', '*', ' '));
                    }
                }
            }

            return responsibilities.Count > 0
                ? string.Join("\n", responsibilities)
                : jobDescription.Substring(0, Math.Min(500, jobDescription.Length)); // Fallback to first 500 chars
        }

        private string? ExtractSkills(string jobDescription, string? providedSkills)
        {
            if (!string.IsNullOrWhiteSpace(providedSkills))
                return providedSkills;

            var lines = jobDescription.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Contains("Skills", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Technical", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmedLine.Split(new[] { ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        return string.Join(", ", parts.Skip(1).Select(p => p.Trim()));
                    }
                }
            }
            return null;
        }

        private int? ExtractExperience(string jobDescription, string? providedExperience)
        {
            if (!string.IsNullOrWhiteSpace(providedExperience))
            {
                // Try to extract number from experience string (e.g., "5-7 years" -> 5)
                var match = System.Text.RegularExpressions.Regex.Match(providedExperience, @"(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var exp))
                {
                    return exp;
                }
            }

            var lines = jobDescription.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Contains("Experience", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Years", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(trimmedLine, @"(\d+)\s*(?:-|\+)?\s*(?:years|year|yr)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var exp))
                    {
                        return exp;
                    }
                }
            }
            return null;
        }

        private string? ExtractQualifications(string jobDescription)
        {
            var lines = jobDescription.Split('\n');
            var qualifications = new List<string>();
            bool inQualificationSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Education", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Qualification", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Degree", StringComparison.OrdinalIgnoreCase))
                {
                    inQualificationSection = true;
                    continue;
                }

                if (inQualificationSection)
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine) ||
                        trimmedLine.StartsWith("Experience", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Skills", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (trimmedLine.Length > 10)
                    {
                        qualifications.Add(trimmedLine);
                    }
                }
            }

            return qualifications.Count > 0 ? string.Join("\n", qualifications) : null;
        }

        private string? ExtractAdditionalInfo(string jobDescription, string? providedAdditionalInfo)
        {
            // If provided in request, use that
            if (!string.IsNullOrWhiteSpace(providedAdditionalInfo))
                return providedAdditionalInfo;

            // Otherwise, extract from job description
            var lines = jobDescription.Split('\n');
            var additionalInfo = new List<string>();
            bool inAdditionalSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Additional Information", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Additional Info", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Additional Details", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Notes", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Other Information", StringComparison.OrdinalIgnoreCase))
                {
                    inAdditionalSection = true;
                    continue;
                }

                if (inAdditionalSection)
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine) ||
                        trimmedLine.StartsWith("Benefits", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Compensation", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Salary", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (trimmedLine.Length > 10)
                    {
                        additionalInfo.Add(trimmedLine);
                    }
                }
            }

            return additionalInfo.Count > 0 ? string.Join("\n", additionalInfo) : null;
        }

        private string? ExtractBenefits(string jobDescription)
        {
            var lines = jobDescription.Split('\n');
            var benefits = new List<string>();
            bool inBenefitsSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.Contains("Benefits", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Compensation", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Perks", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Rewards", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.Contains("Job Summary", StringComparison.OrdinalIgnoreCase))
                {
                    inBenefitsSection = true;
                    continue;
                }

                if (inBenefitsSection)
                {
                    if (string.IsNullOrWhiteSpace(trimmedLine) ||
                        trimmedLine.StartsWith("How to Apply", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Application", StringComparison.OrdinalIgnoreCase) ||
                        trimmedLine.StartsWith("Contact", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("•") || trimmedLine.StartsWith("*") || trimmedLine.Length > 10)
                    {
                        benefits.Add(trimmedLine.TrimStart('-', '•', '*', ' '));
                    }
                }
            }

            return benefits.Count > 0 ? string.Join("\n", benefits) : null;
        }

        public async Task<(int? Id, bool IsSuccess, string Message)> SaveResumeScreeningAsync(
            int companyId, 
            int? applicationId, 
            int? applicantId, 
            int? resumeParsingId, 
            int matchScore, 
            string skillsMatch, 
            string experienceMatch, 
            string qualificationsMatch, 
            string redFlags, 
            string recommendation, 
            string screeningMethod, 
            string screeningProvider, 
            string modelUsed, 
            int processingTime, 
            string userId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationId);
                parameters.Add("@ApplicantID", applicantId);
                parameters.Add("@ResumeParsingID", resumeParsingId);
                parameters.Add("@MatchScore", matchScore);
                parameters.Add("@SkillsMatch", skillsMatch);
                parameters.Add("@ExperienceMatch", experienceMatch);
                parameters.Add("@QualificationsMatch", qualificationsMatch);
                parameters.Add("@RedFlags", redFlags);
                parameters.Add("@Recommendation", recommendation);
                parameters.Add("@ScreeningMethod", screeningMethod);
                parameters.Add("@ScreeningProvider", screeningProvider);
                parameters.Add("@ModelUsed", modelUsed);
                parameters.Add("@ProcessingTime", processingTime);
                parameters.Add("@CompanyID", companyId);
                parameters.Add("@CreatedBy", userId ?? "System");
                parameters.Add("@UpdatedBy", userId ?? "System");
                parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "ruc.SP_Ruc_RecruitmentAI_ResumeScreening_Save",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var id = parameters.Get<int>("@Id");
                return (id, true, "Resume screening saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving resume screening for CompanyID: {CompanyID}", companyId);
                return (null, false, $"Error saving resume screening: {ex.Message}");
            }
        }

        public async Task<(int? Id, bool IsSuccess, string Message)> SaveActivityAsync(int companyId, string activityType, string title, string description, int? relatedId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyId);
                parameters.Add("@ActivityType", activityType);
                parameters.Add("@Title", title);
                parameters.Add("@Description", description);
                parameters.Add("@RelatedId", relatedId);
                parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_RecruitmentAI_Activity_Save]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var id = parameters.Get<int?>("@Id");
                return (id, true, "Activity saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving activity for CompanyID: {CompanyID}", companyId);
                return (null, false, $"Error saving activity: {ex.Message}");
            }
        }

        public async Task<GetSettingsResponseDto?> GetSettingsAsync(int companyId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Check if AutoShortlistThreshold column exists in table
                var hasThresholdColumn = false;
                //try
                //{
                //    var checkColumnSql = @"
                //        SELECT COUNT(*) 
                //        FROM INFORMATION_SCHEMA.COLUMNS 
                //        WHERE TABLE_NAME = 'Tbl_Ruc_RecruitmentAI_Settings' 
                //        AND COLUMN_NAME = 'AutoShortlistThreshold'";
                //    var columnCount = await _db.QueryFirstOrDefaultAsync<int>(checkColumnSql);
                //    hasThresholdColumn = columnCount > 0;
                //}
                //catch
                //{
                //    // Column doesn't exist, use default
                //    hasThresholdColumn = false;
                //}

                var sql = hasThresholdColumn
                    ? @"
                        SELECT TOP 1
                            CompanyID,
                            AutoScreening,
                            AutoMatching,
                            AutoParse,
                            GenerateQuestions,
                            EmailNotifications,
                            AutoShortlistThreshold
                        FROM Tbl_Ruc_RecruitmentAI_Settings
                        WHERE CompanyID = @CompanyID AND IsActive = 1"
                    : @"
                        SELECT TOP 1
                            CompanyID,
                            AutoScreening,
                            AutoMatching,
                            AutoParse,
                            GenerateQuestions,
                            EmailNotifications
                        FROM Tbl_Ruc_RecruitmentAI_Settings
                        WHERE CompanyID = @CompanyID AND IsActive = 1";

                var result = await _db.QueryFirstOrDefaultAsync<dynamic>(sql, new { CompanyID = companyId });
                
                if (result == null)
                    return null;

                return new GetSettingsResponseDto
                {
                    CompanyId = result.CompanyID ?? companyId,
                    Settings = new FeatureSettingsDto
                    {
                        AutoScreening = result.AutoScreening ?? false,
                        AutoMatching = result.AutoMatching ?? false,
                        GenerateQuestions = result.GenerateQuestions ?? false,
                        EmailNotifications = result.EmailNotifications ?? true,
                        AutoShortlistThreshold = hasThresholdColumn && result.AutoShortlistThreshold != null 
                            ? Convert.ToInt32(result.AutoShortlistThreshold) 
                            : 80 // Default value
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings for CompanyID: {CompanyID}", companyId);
                return null;
            }
        }
        //arsalan-raza commited Query convert to Sp
        //public async Task<(bool IsSuccess, string Message)> SaveSettingsAsync(SaveSettingsRequestDto request)
        //{
        //    try
        //    {
        //        if (_db.State != ConnectionState.Open)
        //            _db.Open();

        //        // Check if AutoShortlistThreshold column exists
        //        var hasThresholdColumn = false;
        //        try
        //        {
        //            var checkColumnSql = @"
        //                SELECT COUNT(*) 
        //                FROM INFORMATION_SCHEMA.COLUMNS 
        //                WHERE TABLE_NAME = 'Tbl_Ruc_RecruitmentAI_Settings' 
        //                AND COLUMN_NAME = 'AutoShortlistThreshold'";
        //            var columnCount = await _db.QueryFirstOrDefaultAsync<int>(checkColumnSql);
        //            hasThresholdColumn = columnCount > 0;
        //        }
        //        catch
        //        {
        //            hasThresholdColumn = false;
        //        }

        //        var sql = hasThresholdColumn
        //            ? @"
        //                IF EXISTS (SELECT 1 FROM Tbl_Ruc_RecruitmentAI_Settings WHERE CompanyID = @CompanyID AND IsActive = 1)
        //                BEGIN
        //                    UPDATE Tbl_Ruc_RecruitmentAI_Settings
        //                    SET AutoScreening = @AutoScreening,
        //                        AutoMatching = @AutoMatching,
        //                        GenerateQuestions = @GenerateQuestions,
        //                        EmailNotifications = @EmailNotifications,
        //                        AutoParse = @AutoParse,
        //                        AutoShortlistThreshold = @AutoShortlistThreshold,
        //                        UpdatedOn = GETDATE()
        //                    WHERE CompanyID = @CompanyID AND IsActive = 1
        //                END
        //                ELSE
        //                BEGIN
        //                    INSERT INTO Tbl_Ruc_RecruitmentAI_Settings 
        //                    (CompanyID, AutoScreening, AutoMatching, GenerateQuestions, EmailNotifications, AutoParse, AutoShortlistThreshold, CreatedOn, UpdatedOn, IsActive)
        //                    VALUES 
        //                    (@CompanyID, @AutoScreening, @AutoMatching, @GenerateQuestions, @EmailNotifications, @AutoParse, @AutoShortlistThreshold, GETDATE(), GETDATE(), 1)
        //                END"
        //            : @"
        //                IF EXISTS (SELECT 1 FROM Tbl_Ruc_RecruitmentAI_Settings WHERE CompanyID = @CompanyID AND IsActive = 1)
        //                BEGIN
        //                    UPDATE Tbl_Ruc_RecruitmentAI_Settings
        //                    SET AutoScreening = @AutoScreening,
        //                        AutoMatching = @AutoMatching,
        //                         AutoParse = @AutoParse,
        //                        GenerateQuestions = @GenerateQuestions,
        //                        EmailNotifications = @EmailNotifications,
        //                        UpdatedOn = GETDATE()
        //                    WHERE CompanyID = @CompanyID AND IsActive = 1
        //                END
        //                ELSE
        //                BEGIN
        //                    INSERT INTO Tbl_Ruc_RecruitmentAI_Settings 
        //                    (CompanyID, AutoScreening, AutoMatching,AutoParse, GenerateQuestions, EmailNotifications, CreatedOn, UpdatedOn, IsActive)
        //                    VALUES 
        //                    (@CompanyID, @AutoScreening, @AutoMatching,@AutoParse, @GenerateQuestions, @EmailNotifications, GETDATE(), GETDATE(), 1)
        //                END";

        //        var parameters = new DynamicParameters();
        //        parameters.Add("@CompanyID", request.CompanyId);
        //        parameters.Add("@AutoScreening", request.Settings.AutoScreening);
        //        parameters.Add("@AutoMatching", request.Settings.AutoMatching);
        //        parameters.Add("@AutoParse", request.Settings.AutoParse);
        //        parameters.Add("@GenerateQuestions", request.Settings.GenerateQuestions);
        //        parameters.Add("@EmailNotifications", request.Settings.EmailNotifications);
        //        if (hasThresholdColumn)
        //        {
        //            parameters.Add("@AutoShortlistThreshold", request.Settings.AutoShortlistThreshold);
        //        }

        //        await _db.ExecuteAsync(sql, parameters);
        //        return (true, "Settings saved successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error saving settings for CompanyID: {CompanyID}", request.CompanyId);
        //        return (false, $"Error saving settings: {ex.Message}");
        //    }
        //}
        //arsalan-raza commited Query convert to Sp
        public async Task<(bool IsSuccess, string Message)> SaveSettingsAsync(SaveSettingsRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Deliberately NOT [ruc].[SP_Ruc_RecruitmentAI_Settings_Save] — that SP
                // is SaveApiKeySettingsAsync's, and requires @Provider/@ApiKey/@CreatedBy/
                // @UpdatedBy this method never had. This is the feature-toggle-only save
                // (auto screening/matching/parsing, threshold) for a company that has
                // already configured its provider; it must never touch Provider/ApiKey/
                // ApiEndpoint/Model, or a settings-panel toggle would silently wipe the
                // saved API key.
                var parameters = new DynamicParameters();

                parameters.Add("@CompanyID", request.CompanyId);
                parameters.Add("@AutoScreening", request.Settings.AutoScreening);
                parameters.Add("@AutoMatching", request.Settings.AutoMatching);
                parameters.Add("@AutoParse", request.Settings.AutoParse);
                parameters.Add("@GenerateQuestions", request.Settings.GenerateQuestions);
                parameters.Add("@EmailNotifications", request.Settings.EmailNotifications);
                parameters.Add("@AutoShortlistThreshold", request.Settings.AutoShortlistThreshold);

                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_RecruitmentAI_FeatureSettings_Save]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");

                return result switch
                {
                    1 => (true, "Settings saved successfully"),
                    0 => (false, "AI provider settings must be configured for this company before feature toggles can be saved."),
                    _ => (false, "Failed to save settings"),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                return (false, ex.Message);
            }
        }
        //arsalan-raza commited Query convert to Sp
        //public async Task<(int? Id, bool IsSuccess, string Message)> SaveResumeParsingAsync(
        //    int companyId,
        //    int? applicantId,
        //    int? applicationId,
        //    string? resumeFileName,
        //    string resumeFilePath,
        //    string? fileType,
        //    long? fileSize,
        //    string parsedDataJson,
        //    string parsedResumeText,
        //    string parsingMethod,
        //    string parsingProvider,
        //    string parsingModel,
        //    string parsingStatus,
        //    decimal? parsingConfidence,
        //    string? parsingErrors,
        //    int tokensUsed,
        //    int processingTime,
        //    string userId)
        //{
        //    try
        //    {
        //        if (_db.State != ConnectionState.Open)
        //            _db.Open();

        //        var sql = @"
        //            INSERT INTO [RUC].[Tbl_RecruitmentAI_ResumeParsing]
        //            ([CompanyID], [ApplicantID], [ApplicationID], [ResumeFileName], [ResumeFilePath], 
        //             [FileType], [FileSize], [ParsedData], [ParsedResumeText], 
        //             [ParsingMethod], [ParsingProvider], [ParsingModel], [ParsingStatus], 
        //             [ParsingConfidence], [ParsingErrors], [TokensUsed], [ProcessingTime], 
        //             [CreatedBy], [CreatedOn], [UpdatedBy], [UpdatedOn], [IsActive])
        //            VALUES 
        //            (@CompanyID, @ApplicantID, @ApplicationID, @ResumeFileName, @ResumeFilePath, 
        //             @FileType, @FileSize, @ParsedData, @ParsedResumeText, 
        //             @ParsingMethod, @ParsingProvider, @ParsingModel, @ParsingStatus, 
        //             @ParsingConfidence, @ParsingErrors, @TokensUsed, @ProcessingTime, 
        //             @CreatedBy, GETDATE(), @UpdatedBy, GETDATE(), 1)

        //            SELECT CAST(SCOPE_IDENTITY() AS INT)";

        //        var parameters = new DynamicParameters();
        //        parameters.Add("@CompanyID", companyId);
        //        parameters.Add("@ApplicantID", applicantId);
        //        parameters.Add("@ApplicationID", applicationId);
        //        parameters.Add("@ResumeFileName", resumeFileName);
        //        parameters.Add("@ResumeFilePath", resumeFilePath);
        //        parameters.Add("@FileType", fileType);
        //        parameters.Add("@FileSize", fileSize);
        //        parameters.Add("@ParsedData", parsedDataJson);
        //        parameters.Add("@ParsedResumeText", parsedResumeText);
        //        parameters.Add("@ParsingMethod", parsingMethod ?? "AI");
        //        parameters.Add("@ParsingProvider", parsingProvider);
        //        parameters.Add("@ParsingModel", parsingModel);
        //        parameters.Add("@ParsingStatus", parsingStatus ?? "Success");
        //        parameters.Add("@ParsingConfidence", parsingConfidence);
        //        parameters.Add("@ParsingErrors", parsingErrors);
        //        parameters.Add("@TokensUsed", tokensUsed);
        //        parameters.Add("@ProcessingTime", processingTime);
        //        parameters.Add("@CreatedBy", userId ?? "System");
        //        parameters.Add("@UpdatedBy", userId ?? "System");

        //        var id = await _db.QueryFirstOrDefaultAsync<int?>(sql, parameters);
        //        return (id, true, "Resume parsing saved successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error saving resume parsing for CompanyID: {CompanyID}", companyId);
        //        return (null, false, $"Error saving resume parsing: {ex.Message}");
        //    }
        //}
        //arsalan-raza commited Query convert to Sp
        public async Task<(int? Id, bool IsSuccess, string Message)> SaveResumeParsingAsync(
        int companyId,
        int? applicantId,
        int? applicationId,
        string? resumeFileName,
        string resumeFilePath,
        string? fileType,
        long? fileSize,
        string parsedDataJson,
        string parsedResumeText,
        string parsingMethod,
        string parsingProvider,
        string parsingModel,
        string parsingStatus,
        decimal? parsingConfidence,
        string? parsingErrors,
        int tokensUsed,
        int processingTime,
        string userId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();

                parameters.Add("@CompanyID", companyId);
                parameters.Add("@ApplicantID", applicantId);
                parameters.Add("@ApplicationID", applicationId);
                parameters.Add("@ResumeFileName", resumeFileName);
                parameters.Add("@ResumeFilePath", resumeFilePath);
                parameters.Add("@FileType", fileType);
                parameters.Add("@FileSize", fileSize);
                parameters.Add("@ParsedData", parsedDataJson);
                parameters.Add("@ParsedResumeText", parsedResumeText);
                parameters.Add("@ParsingMethod", parsingMethod ?? "AI");
                parameters.Add("@ParsingProvider", parsingProvider);
                parameters.Add("@ParsingModel", parsingModel);
                parameters.Add("@ParsingStatus", parsingStatus ?? "Success");
                parameters.Add("@ParsingConfidence", parsingConfidence);
                parameters.Add("@ParsingErrors", parsingErrors);
                parameters.Add("@TokensUsed", tokensUsed);
                parameters.Add("@ProcessingTime", processingTime);
                parameters.Add("@CreatedBy", userId ?? "System");
                parameters.Add("@UpdatedBy", userId ?? "System");

                parameters.Add("@Id", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[RUC].[SP_Ruc_RecruitmentAI_ResumeParsing_Save]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var id = parameters.Get<int?>("@Id");
                var result = parameters.Get<int>("@Result");

                return (id, result == 1, result == 1 ? "Resume parsing saved successfully" : "Failed to save resume parsing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving resume parsing for CompanyID: {CompanyID}", companyId);
                return (null, false, $"Error saving resume parsing: {ex.Message}");
            }
        }

        public async Task<(List<int> RankingIds, bool IsSuccess, string Message)> SaveCandidateRankingsAsync(
            int companyId,
            int requisitionId,
            List<CandidateRankingData> rankings,
            string rankingMethod,
            string rankingProvider,
            string rankingModel,
            string rankingBatchId,
            int totalCandidatesRanked,
            int tokensUsed,
            int processingTime,
            string userId)
        {
            var rankingIds = new List<int>();
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Insert each candidate ranking as a separate row
                foreach (var ranking in rankings)
                {
                    var sql = @"
                        INSERT INTO [RUC].[Tbl_RecruitmentAI_CandidateRanking]
                        ([CompanyID], [RequisitionID], [ApplicationID], [ApplicantID], [Rank], 
                         [OverallScore], [RankingData], [RankingMethod], [RankingProvider], [RankingModel], 
                         [RankingBatchID], [TotalCandidatesRanked], [Percentile], [TokensUsed], [ProcessingTime], 
                         [CreatedBy], [CreatedOn], [UpdatedBy], [UpdatedOn], [IsActive])
                        VALUES 
                        (@CompanyID, @RequisitionID, @ApplicationID, @ApplicantID, @Rank, 
                         @OverallScore, @RankingData, @RankingMethod, @RankingProvider, @RankingModel, 
                         @RankingBatchID, @TotalCandidatesRanked, @Percentile, @TokensUsed, @ProcessingTime, 
                         @CreatedBy, GETDATE(), @UpdatedBy, GETDATE(), 1)
                        
                        SELECT CAST(SCOPE_IDENTITY() AS INT)";

                    var parameters = new DynamicParameters();
                    parameters.Add("@CompanyID", companyId);
                    parameters.Add("@RequisitionID", requisitionId);
                    parameters.Add("@ApplicationID", ranking.ApplicationID);
                    parameters.Add("@ApplicantID", ranking.ApplicantID);
                    parameters.Add("@Rank", ranking.Rank);
                    parameters.Add("@OverallScore", ranking.OverallScore);
                    parameters.Add("@RankingData", ranking.RankingDataJson ?? (object)DBNull.Value);
                    parameters.Add("@RankingMethod", rankingMethod ?? "AI");
                    parameters.Add("@RankingProvider", rankingProvider ?? (object)DBNull.Value);
                    parameters.Add("@RankingModel", rankingModel ?? (object)DBNull.Value);
                    parameters.Add("@RankingBatchID", rankingBatchId);
                    parameters.Add("@TotalCandidatesRanked", totalCandidatesRanked);
                    parameters.Add("@Percentile", ranking.Percentile);
                    parameters.Add("@TokensUsed", tokensUsed);
                    parameters.Add("@ProcessingTime", processingTime);
                    parameters.Add("@CreatedBy", userId ?? "System");
                    parameters.Add("@UpdatedBy", userId ?? "System");

                    var id = await _db.QueryFirstOrDefaultAsync<int?>(sql, parameters);
                    if (id.HasValue)
                    {
                        rankingIds.Add(id.Value);
                    }
                }

                return (rankingIds, true, $"Successfully saved {rankingIds.Count} candidate rankings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving candidate rankings for CompanyID: {CompanyID}, RequisitionID: {RequisitionID}", companyId, requisitionId);
                return (rankingIds, false, $"Error saving candidate rankings: {ex.Message}");
            }
        }

        public async Task UpdateJobBankCandidateFromParsedData(int companyId,int candidateId,ParseResumeResponseDto parsed)
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            var skills = parsed.Skills != null
                ? string.Join(", ", parsed.Skills)
                : null;

            var experienceSummary = parsed.Experience != null
                ? string.Join(" | ", parsed.Experience.Select(e =>
                    $"{e.Role} at {e.Company} ({e.Duration})"))
                : null;

            var education = parsed.Education != null
                ? string.Join(", ", parsed.Education.Select(e =>
                    $"{e.Degree} - {e.Institution}"))
                : null;

            var sql = @"
                UPDATE ruc.JobBankCandidate
                SET
                    Skills = @Skills,
                    ExperienceYears = @ExperienceYears,
                    ExperienceSummary = @ExperienceSummary,
                    Education = @Education,
                    ModifiedOn = GETDATE()
                WHERE JobBankCandidateID = @CandidateID
                AND CompanyID = @CompanyID";

            var parameters = new
            {
                CompanyID = companyId,
                CandidateID = candidateId,
                Skills = skills,
                ExperienceYears = parsed.TotalYearsExperience,
                ExperienceSummary = experienceSummary,
                Education = education
            };

            await _db.ExecuteAsync(sql, parameters);
        }
        //arsalan-raza commited Query convert to Sp
        //public async Task SaveCandidateAIMatchAsync(int companyId,int requisitionId,int candidateId,MatchCandidateResponseDto result,string createdBy)
        //{
        //    if (_db.State != ConnectionState.Open)
        //        _db.Open();

        //    var matchedSkills = result.MatchedSkills != null
        //        ? string.Join(", ", result.MatchedSkills)
        //        : null;

        //    var missingSkills = result.MissingSkills != null
        //        ? string.Join(", ", result.MissingSkills)
        //        : null;

        //    var sql = @"

        //        IF EXISTS (
        //            SELECT 1 
        //            FROM ruc.JobBankCandidateAIMatch
        //            WHERE CompanyID = @CompanyID
        //            AND JobRequisitionID = @JobRequisitionID
        //            AND JobBankCandidateID = @CandidateID
        //        )

        //        BEGIN

        //            UPDATE ruc.JobBankCandidateAIMatch
        //            SET
        //                MatchScore = @MatchScore,
        //                MatchPercentage = @MatchPercentage,
        //                Recommendation = @Recommendation,
        //                MatchedSkills = @MatchedSkills,
        //                MissingSkills = @MissingSkills,
        //                MatchDetails = @MatchDetails,
        //                MatchedOn = @MatchedOn,
        //                CreatedOn = GETDATE(),
        //                CreatedBy = @CreatedBy

        //            WHERE CompanyID = @CompanyID
        //            AND JobRequisitionID = @JobRequisitionID
        //            AND JobBankCandidateID = @CandidateID

        //        END

        //        ELSE

        //        BEGIN

        //            INSERT INTO ruc.JobBankCandidateAIMatch
        //            (
        //                CompanyID,
        //                JobRequisitionID,
        //                JobBankCandidateID,
        //                MatchScore,
        //                MatchPercentage,
        //                Recommendation,
        //                MatchedSkills,
        //                MissingSkills,
        //                MatchDetails,
        //                MatchedOn,
        //                CreatedBy
        //            )
        //            VALUES
        //            (
        //                @CompanyID,
        //                @JobRequisitionID,
        //                @CandidateID,
        //                @MatchScore,
        //                @MatchPercentage,
        //                @Recommendation,
        //                @MatchedSkills,
        //                @MissingSkills,
        //                @MatchDetails,
        //                @MatchedOn,
        //                @CreatedBy
        //            )

        //        END
        //        ";

        //    await _db.ExecuteAsync(sql, new
        //    {
        //        CompanyID = companyId,
        //        JobRequisitionID = requisitionId,
        //        CandidateID = candidateId,
        //        MatchScore = result.MatchScore,
        //        MatchPercentage = result.MatchPercentage,
        //        Recommendation = result.Recommendation,
        //        MatchedSkills = matchedSkills,
        //        MissingSkills = missingSkills,
        //        MatchDetails = result.MatchDetails,
        //        MatchedOn = result.MatchedOn,
        //        CreatedBy = createdBy
        //    });
        //}
        //arsalan-raza commited Query convert to Sp
        public async Task SaveCandidateAIMatchAsync(int companyId,int requisitionId,int candidateId,MatchCandidateResponseDto result,string createdBy)
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            var matchedSkills = result.MatchedSkills != null
                ? string.Join(", ", result.MatchedSkills)
                : null;

            var missingSkills = result.MissingSkills != null
                ? string.Join(", ", result.MissingSkills)
                : null;

            var parameters = new DynamicParameters();

            parameters.Add("@CompanyID", companyId);
            parameters.Add("@JobRequisitionID", requisitionId);
            parameters.Add("@CandidateID", candidateId);
            parameters.Add("@MatchScore", result.MatchScore);
            parameters.Add("@MatchPercentage", result.MatchPercentage);
            parameters.Add("@Recommendation", result.Recommendation);
            parameters.Add("@MatchedSkills", matchedSkills);
            parameters.Add("@MissingSkills", missingSkills);
            parameters.Add("@MatchDetails", result.MatchDetails);
            parameters.Add("@MatchedOn", result.MatchedOn);
            parameters.Add("@CreatedBy", createdBy);

            parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _db.ExecuteAsync(
                "[RUC].[SP_Ruc_JobBankCandidateAIMatch_Save]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            var resultValue = parameters.Get<int>("@Result");

            if (resultValue != 1)
            {
                throw new Exception("Failed to save AI match result");
            }
        }

        public async Task<List<CandidateAIMatchDto>> GetAIMatchesByRequisitionAsync(int companyID,int jobRequisitionId)
        {
            if (_db.State != ConnectionState.Open) _db.Open();

            var sql = @"
                SELECT JobBankCandidateID, MatchScore, MatchPercentage, Recommendation, MatchedSkills, MissingSkills, MatchDetails
                FROM ruc.JobBankCandidateAIMatch
                WHERE JobRequisitionID = @JobRequisitionID and CompanyID = @companyID";

            return (await _db.QueryAsync<CandidateAIMatchDto>(sql, new { JobRequisitionID = jobRequisitionId, CompanyID = companyID })).ToList();
        }
    }
}

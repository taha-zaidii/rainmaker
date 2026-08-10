using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.hrm.module
{
    // API Key Status Response
    public class ApiKeyStatusResponseDto
    {
        public bool HasApiKey { get; set; }
        public string? Provider { get; set; }
        public bool IsValid { get; set; }
    }

    // Feature Settings
    public class FeatureSettingsDto
    {
        public bool AutoScreening { get; set; }
        public bool AutoParse { get; set; }
        public bool AutoMatching { get; set; }
        public bool GenerateQuestions { get; set; }
        public bool EmailNotifications { get; set; }
        /// <summary>
        /// Auto-shortlist threshold (0-100). Applications with match score >= this threshold will be auto-shortlisted.
        /// Default: 80
        /// </summary>
        public int AutoShortlistThreshold { get; set; }
    }

    // API Key Settings Response
    public class ApiKeySettingsResponseDto
    {
        public string? ApiKey { get; set; } // Masked or encrypted
        public string Provider { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public string? Model { get; set; }
        public int MaxTokens { get; set; }
        public decimal Temperature { get; set; }
        public int AutoShortlistThreshold { get; set; }
        public FeatureSettingsDto Settings { get; set; } = new FeatureSettingsDto();
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    // Save API Key Settings Request
    public class SaveApiKeySettingsRequestDto
    {
        public int CompanyId { get; set; }
        public string Provider { get; set; } = string.Empty; // "openai", "anthropic", "google", "custom"
        public string ApiKey { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
        public string Model { get; set; } = string.Empty;
        public int MaxTokens { get; set; } = 1000;
        public decimal Temperature { get; set; } = 0.7m;
        public FeatureSettingsDto? Settings { get; set; }
    }

    // Save API Key Settings Response
    public class SaveApiKeySettingsResponseDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public DateTime UpdatedOn { get; set; }
    }

    // Test API Key Request
    public class TestApiKeyRequestDto
    {
        public int CompanyId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string? ApiEndpoint { get; set; }
    }

    /// <summary>
    /// Outcomes of a key test. A boolean cannot carry the distinction that
    /// matters most here: "your key is wrong" and "we could not reach the AI
    /// service" look identical to a recruiter but need completely different
    /// actions, and conflating them has already cost real debugging time.
    /// </summary>
    public static class TestApiKeyStatus
    {
        /// <summary>Key accepted by the provider.</summary>
        public const string Valid = "valid";

        /// <summary>Provider answered, and rejected the key. Do not retry.</summary>
        public const string InvalidKey = "invalid_key";

        /// <summary>Provider could not be contacted at all — network, DNS, TLS or timeout.</summary>
        public const string Unreachable = "unreachable";

        /// <summary>Settings themselves are wrong (e.g. the endpoint is not a usable URL).</summary>
        public const string Misconfigured = "misconfigured";

        /// <summary>Provider is not one this backend knows how to test.</summary>
        public const string UnsupportedProvider = "unsupported_provider";
    }

    // Test API Key Response
    public class TestApiKeyResponseDto
    {
        public bool IsValid { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? Model { get; set; }
        public string? TestResponse { get; set; }
        public string? Error { get; set; }

        /// <summary>
        /// One of <see cref="TestApiKeyStatus"/>. Lets the settings page show
        /// "AI service unreachable" instead of the misleading "API Key Invalid"
        /// when the key was never actually the problem.
        /// </summary>
        public string? Status { get; set; }

        /// <summary>Reported by Multinet's in-house AI service. Null for other providers.</summary>
        public string? ServiceVersion { get; set; }

        /// <summary>ProfileSchema version the AI service is serving. Null for other providers.</summary>
        public string? SchemaVersion { get; set; }

        /// <summary>
        /// What the key is permitted to do, e.g. "recruitment.jobreq.generate".
        /// Drive the feature toggles from this rather than hard-coding them, so
        /// the portal follows the service as it gains features.
        /// </summary>
        public List<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// A usable-but-suspicious configuration worth showing the administrator,
        /// e.g. an endpoint that had to be corrected before the call could be made.
        /// Not an error: the test still ran.
        /// </summary>
        public string? ConfigurationWarning { get; set; }
    }

    // Dashboard Statistics
    // Dashboard Response (using DTOs from RecruitmentAdditionalDtos.cs)
    public class DashboardStatsResponseDto
    {
        public DashboardStatsDto Stats { get; set; } = new DashboardStatsDto();
        public List<RecentActivityDto> RecentActivity { get; set; } = new List<RecentActivityDto>();
    }

    /// <summary>Single row from sp_Dashboard_RecStats (Title + Value).</summary>
    public class RecDashboardRecStatsItemDto
    {
        public string Title { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    /// <summary>Response for recruitment dashboard stats (Job Candidates, Rejected, Total Jobs, Total Interviews).</summary>
    public class RecDashboardRecStatsResponseDto
    {
        public List<RecDashboardRecStatsItemDto> Stats { get; set; } = new List<RecDashboardRecStatsItemDto>();
    }

    // Generate Job Description Request
    public class GenerateJobDescriptionRequestDto
    {
        public int CompanyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Experience { get; set; }
        public string? Skills { get; set; }
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// The wizard already collects this; it simply had nowhere to go.
        /// Optional — existing callers that omit it behave exactly as before.
        /// </summary>
        public string? Designation { get; set; }

        /// <summary>
        /// The Job Category dropdown's allowed values. Send them and the AI snaps
        /// its answer to a real option so it always binds; omit them and you get
        /// free text the dropdown may reject.
        /// </summary>
        public List<string>? JobCategoryOptions { get; set; }
    }

    // ── AI-generated requisition draft (maps onto the 4-step wizard) ─────────
    //
    // Every field is a SUGGESTION for a human to edit. Nulls are meaningful:
    // they mark fields the AI is forbidden from filling because they are HR's
    // decision. Render them as empty and editable, never as a failure.

    /// <summary>A numeric range where either end may legitimately be unknown.</summary>
    public class AiJobDraftRangeDto
    {
        public int? Minimum { get; set; }
        public int? Maximum { get; set; }
    }

    /// <summary>Wizard step 1 — Basic Information.</summary>
    public class AiJobDraftBasicInfoDto
    {
        /// <summary>Verbatim echo of what was submitted — binds straight back to the field.</summary>
        public string? JobTitle { get; set; }

        /// <summary>Verbatim echo — binds straight back to the dropdown.</summary>
        public string? Department { get; set; }

        /// <summary>Verbatim echo — binds straight back to the dropdown.</summary>
        public string? Designation { get; set; }

        public string? JobSummary { get; set; }
        public string? JobCategory { get; set; }

        /// <summary>Always 1 — a starting value for the human to change.</summary>
        public int? Vacancies { get; set; }

        /// <summary>Null by design — HR's decision.</summary>
        public string? EmploymentType { get; set; }

        /// <summary>Null by design — HR's decision.</summary>
        public string? Grade { get; set; }
    }

    /// <summary>Wizard step 2 — Requirements.</summary>
    public class AiJobDraftRequirementsDto
    {
        public AiJobDraftRangeDto? ExperienceYears { get; set; }

        /// <summary>
        /// Always null, deliberately. Age is a protected attribute and an AI
        /// proposing an age band in a job advert is discriminatory. Do not bind
        /// an input to this, and never backfill it.
        /// </summary>
        public AiJobDraftRangeDto? AgeLimits { get; set; }

        public List<string> KeyResponsibilities { get; set; } = new List<string>();
        public List<string> Requirements { get; set; } = new List<string>();
        public List<string> Qualifications { get; set; } = new List<string>();
        public List<string> Skills { get; set; } = new List<string>();
    }

    /// <summary>Wizard step 3 — Compensation. Only location is AI-suggested.</summary>
    public class AiJobDraftCompensationDto
    {
        public string? Location { get; set; }

        /// <summary>Null by design.</summary>
        public string? Benefits { get; set; }

        /// <summary>Null by design.</summary>
        public string? BudgetType { get; set; }

        /// <summary>Null by design.</summary>
        public int? BudgetLineId { get; set; }
    }

    /// <summary>Wizard step 4 — Publishing. The AI never publishes.</summary>
    public class AiJobDraftPublishingDto
    {
        /// <summary>Null by design.</summary>
        public string? Justification { get; set; }

        /// <summary>Always false — a human publishes.</summary>
        public bool IsPublicJob { get; set; }

        /// <summary>Always "Draft".</summary>
        public string? Status { get; set; }

        /// <summary>Null by design.</summary>
        public string? ClosingDate { get; set; }
    }

    /// <summary>The full draft, one property per wizard step.</summary>
    public class AiJobDraftDto
    {
        public AiJobDraftBasicInfoDto BasicInfo { get; set; } = new AiJobDraftBasicInfoDto();
        public AiJobDraftRequirementsDto Requirements { get; set; } = new AiJobDraftRequirementsDto();
        public AiJobDraftCompensationDto Compensation { get; set; } = new AiJobDraftCompensationDto();
        public AiJobDraftPublishingDto Publishing { get; set; } = new AiJobDraftPublishingDto();
    }

    // Generate Job Description Response
    public class GenerateJobDescriptionResponseDto
    {
        /// <summary>
        /// Readable rendering of the whole draft. Kept so existing callers that
        /// only know about a single text blob keep working unchanged; new screens
        /// should bind <see cref="Draft"/> field by field instead.
        /// </summary>
        public string JobDescription { get; set; } = string.Empty;

        public DateTime GeneratedOn { get; set; }
        public int TokensUsed { get; set; }
        public string? Model { get; set; }

        /// <summary>
        /// The structured draft, when the company's provider returns one.
        /// Null for providers that only produce free text.
        /// </summary>
        public AiJobDraftDto? Draft { get; set; }

        /// <summary>
        /// Always true for AI-generated content. Drives the
        /// "AI-generated — please review" affordance. A human must edit and
        /// approve before anything is saved as a requisition of record.
        /// </summary>
        public bool ReviewRequired { get; set; }

        /// <summary>Server-side generation time. Useful when someone asks why a call took 30 seconds.</summary>
        public long? ExecutionTimeMs { get; set; }

        /// <summary>True when the AI service answered from its deterministic cache.</summary>
        public bool? CacheHit { get; set; }

        /// <summary>"parsed_from_request" when the AI used the experience range the user typed.</summary>
        public string? ExperienceSource { get; set; }

        /// <summary>"selected_from_options" when the category snapped to a real dropdown value.</summary>
        public string? JobCategorySource { get; set; }

        /// <summary>e.g. "Hybrid" — inferred, still a suggestion.</summary>
        public string? WorkMode { get; set; }

        /// <summary>
        /// Fields the AI deliberately left empty because they are HR's to decide.
        /// Show these as "for you to complete" rather than letting them read as a
        /// failed generation.
        /// </summary>
        public List<string> FieldsForHumanToComplete { get; set; } = new List<string>();
    }

    // Job Requirements
    public class JobRequirementsDto
    {
        public string? JobTitle { get; set; }
        public List<string> RequiredSkills { get; set; } = new List<string>();
        public string? Experience { get; set; }
        public string? Education { get; set; }
    }

    // Screen Resume Request
    public class ScreenResumeRequestDto
    {
        /// <summary>
        /// Company ID (Angular se "companyID" aayega)
        /// </summary>
        [Required]
        [System.Text.Json.Serialization.JsonPropertyName("companyID")]
        [Newtonsoft.Json.JsonProperty("companyID")]
        public int CompanyId { get; set; }
        
        /// <summary>
        /// Application ID (optional - for linking screening to application)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("applicationID")]
        [Newtonsoft.Json.JsonProperty("applicationID")]
        public int? ApplicationID { get; set; }
        
        /// <summary>
        /// Applicant ID (optional - for linking screening to applicant)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("applicantID")]
        [Newtonsoft.Json.JsonProperty("applicantID")]
        public int? ApplicantID { get; set; }
        
        /// <summary>
        /// Resume Parsing ID (optional - link to previous resume parsing result)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("resumeParsingID")]
        [Newtonsoft.Json.JsonProperty("resumeParsingID")]
        public int? ResumeParsingID { get; set; }
        
        /// <summary>
        /// Resume ID (optional - legacy support)
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("resumeId")]
        [Newtonsoft.Json.JsonProperty("resumeId")]
        public int? ResumeId { get; set; }
        
        /// <summary>
        /// Path to resume file (Angular se "resumePath" aayega)
        /// Required: Frontend se file path hi aayegi, backend file se text extract karega
        /// </summary>
        [Required(ErrorMessage = "resumePath is required")]
        [System.Text.Json.Serialization.JsonPropertyName("resumePath")]
        [Newtonsoft.Json.JsonProperty("resumePath")]
        public string ResumeFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional: Direct resume text (for testing or manual input)
        /// Note: Agar ResumeFilePath diya hai to ResumeText ignore ho jayega
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("resumeText")]
        [Newtonsoft.Json.JsonProperty("resumeText")]
        public string? ResumeText { get; set; }
        
        /// <summary>
        /// Job Requirements for screening
        /// </summary>
        [Required]
        [System.Text.Json.Serialization.JsonPropertyName("jobRequirements")]
        [Newtonsoft.Json.JsonProperty("jobRequirements")]
        public JobRequirementsDto JobRequirements { get; set; } = new JobRequirementsDto();
    }

    // Screen Resume Response
    public class ScreenResumeResponseDto
    {
        public int MatchScore { get; set; } // 0-100
        public string Recommendation { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
        public string ScreeningNotes { get; set; } = string.Empty;
        public DateTime ScreenedOn { get; set; }
        public bool Shortlisted { get; set; }
        public int ThresholdUsed { get; set; } = 80;
        public List<string> MatchedSkills { get; set; } = new List<string>();
        public List<string> MissingSkills { get; set; } = new List<string>();
        public List<ScreeningReasonDto> Reasons { get; set; } = new List<ScreeningReasonDto>();
    }

    public class ScreeningReasonDto
    {
        public string Kind { get; set; } = string.Empty; // "match" | "gap"
        public string Detail { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
    }


    // Candidate Profile
    public class CandidateProfileDto
    {
        public List<string> Skills { get; set; } = new List<string>();
        public string? Experience { get; set; }
        public string? Education { get; set; }
    }

    // Match Candidate Request
    public class MatchCandidateRequestDto
    {
        public int CompanyId { get; set; }
        public int? CandidateId { get; set; }
        public int? JobRequisitionId { get; set; }
        public CandidateProfileDto CandidateProfile { get; set; } = new CandidateProfileDto();
        public JobRequirementsDto JobRequirements { get; set; } = new JobRequirementsDto();
    }

    // Match Candidate Response
    public class MatchCandidateResponseDto
    {
        public int MatchScore { get; set; } // 0-100
        public int MatchPercentage { get; set; } // Same as MatchScore
        public string Recommendation { get; set; } = string.Empty;
        public List<string> MatchedSkills { get; set; } = new List<string>();
        public List<string> MissingSkills { get; set; } = new List<string>();
        public string MatchDetails { get; set; } = string.Empty;
        public DateTime MatchedOn { get; set; }
    }

    // Generate Interview Questions Request
    public class GenerateInterviewQuestionsRequestDto
    {
        public int CompanyId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? CandidateResume { get; set; }
        public JobRequirementsDto? JobRequirements { get; set; }
        public string QuestionType { get; set; } = "mixed"; // "technical", "behavioral", "mixed"
        public int NumberOfQuestions { get; set; } = 10;
    }

    // Interview Question
    public class InterviewQuestionDto
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "technical", "behavioral"
        public string Category { get; set; } = string.Empty;
        public string? ExpectedAnswer { get; set; }
    }

    // Generate Interview Questions Response
    public class GenerateInterviewQuestionsResponseDto
    {
        public List<InterviewQuestionDto> Questions { get; set; } = new List<InterviewQuestionDto>();
        public DateTime GeneratedOn { get; set; }
        public int TokensUsed { get; set; }
    }

    // Save Settings Request
    public class SaveSettingsRequestDto
    {
        public int CompanyId { get; set; }
        public FeatureSettingsDto Settings { get; set; } = new FeatureSettingsDto();
        public ApiKeySettingsDto? ApiKeySettings { get; set; }
    }

    public class ApiKeySettingsDto
    {
        public string? Provider { get; set; }
        public string? ApiKey { get; set; }
        public string? ApiEndpoint { get; set; }
        public string? Model { get; set; }
        public int? MaxTokens { get; set; }
        public decimal? Temperature { get; set; }
    }

    // Save Settings Response
    public class SaveSettingsResponseDto
    {
        public int CompanyId { get; set; }
        public FeatureSettingsDto Settings { get; set; } = new FeatureSettingsDto();
        public DateTime UpdatedOn { get; set; }
    }

    // Get Settings Response
    public class GetSettingsResponseDto
    {
        public int CompanyId { get; set; }
        public FeatureSettingsDto Settings { get; set; } = new FeatureSettingsDto();
    }

    // Save Job Description Request
    public class SaveJobDescriptionRequestDto
    {
        public int CompanyId { get; set; }
        public int? JobRequisitionId { get; set; }
        public string JobDescription { get; set; } = string.Empty;

        /// <summary>
        /// The job summary as its own field.
        ///
        /// Without this the backend had to scrape the summary back out of the
        /// rendered JobDescription blob, which only worked when that text
        /// happened to carry a "Job Summary" heading. The AI wizard holds the
        /// summary as a discrete value, so it should send it as one — the
        /// careers page had nothing under "About the role" otherwise.
        /// Optional: callers that omit it keep the previous extraction path.
        /// </summary>
        public string? JobSummary { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? EmploymentType { get; set; }
        public string? Grade { get; set; }
        public int? Vacancies { get; set; }
        public string? Experience { get; set; }
        public int? MinExperience { get; set; }
        public int? MaxExperience { get; set; }
        public decimal? MinSalary { get; set; }
        public decimal? MaxSalary { get; set; }
        public string? Location { get; set; }
        public string? Skills { get; set; }
        public string? KeyResponsibilities { get; set; }
        public string? Requirements { get; set; }
        public string? Qualifications { get; set; }
        public string? Benefits { get; set; }

        /// <summary>
        /// HR's stated reason for requisitioning the role, entered on the
        /// wizard's Step 4 (Publishing) — never AI-generated (see CLAUDE.md
        /// §10: justification is null-by-design out of the AI response).
        /// Optional: callers that omit it behave exactly as before.
        /// </summary>
        public string? Justification { get; set; }
        public string? AdditionalInfo { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? IsPublished { get; set; }
    }



    // Save Job Description Response
    public class SaveJobDescriptionResponseDto
    {
        public int Id { get; set; }
        public int? JobRequisitionId { get; set; }
        public bool IsUpdate { get; set; }
        public bool Saved { get; set; } = true;
    }

    // Parse Resume Request
    public class ParseResumeRequestDto
    {
        /// <summary>
        /// Company ID (supports both companyId and companyID from Angular)
        /// Primary property - Angular se "companyID" aayega to yahan map hoga
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("companyID")]
        [Newtonsoft.Json.JsonProperty("companyID")]
        public int CompanyId { get; set; }
        
        /// <summary>
        /// Path to resume file (relative path from storage or absolute path)
        /// Required: Frontend se file path hi aayegi, backend file se text extract karega
        /// Supports both resumeFilePath and resumePath (for Angular compatibility)
        /// Primary property - Angular se "resumePath" aayega to yahan map hoga
        /// </summary>
        private string _resumeFilePath = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("resumePath")]
        [Newtonsoft.Json.JsonProperty("resumePath")]
        public string ResumeFilePath
        {
            get => _resumeFilePath;
            set => _resumeFilePath = value;
        }

        [System.Text.Json.Serialization.JsonPropertyName("resumeFilePath")]
        [Newtonsoft.Json.JsonProperty("resumeFilePath")]
        public string ResumeFilePathAlias
        {
            get => _resumeFilePath;
            set { if (!string.IsNullOrWhiteSpace(value)) _resumeFilePath = value; }
        }
        
        /// <summary>
        /// Optional: Direct resume text (for testing or manual input)
        /// Note: Agar ResumeFilePath diya hai to ResumeText ignore ho jayega
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("resumeText")]
        [Newtonsoft.Json.JsonProperty("resumeText")]
        public string? ResumeText { get; set; }
        
        // Additional fields from Angular (optional, for logging/audit purposes)
        [System.Text.Json.Serialization.JsonPropertyName("applicationID")]
        [Newtonsoft.Json.JsonProperty("applicationID")]
        public int? ApplicationID { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("applicantID")]
        [Newtonsoft.Json.JsonProperty("applicantID")]
        public int? ApplicantID { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("resumeFileName")]
        [Newtonsoft.Json.JsonProperty("resumeFileName")]
        public string? ResumeFileName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("fileType")]
        [Newtonsoft.Json.JsonProperty("fileType")]
        public string? FileType { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("fileSize")]
        [Newtonsoft.Json.JsonProperty("fileSize")]
        public long? FileSize { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("isAutoProcessed")]
        [Newtonsoft.Json.JsonProperty("isAutoProcessed")]
        public bool? IsAutoProcessed { get; set; }
    }

    // Parse Resume Response
    public class ParseResumeResponseDto
    {
        public string? FullName { get; set; }
        public string? CandidateName { get => FullName; set => FullName = value; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? Summary { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public List<ResumeExperienceDto> Experience { get; set; } = new List<ResumeExperienceDto>();
        public List<ResumeEducationDto> Education { get; set; } = new List<ResumeEducationDto>();
        public List<ResumeProjectDto> Projects { get; set; } = new List<ResumeProjectDto>();
        public List<string> Certifications { get; set; } = new List<string>();
        public string? Languages { get; set; }
        public int? TotalYearsExperience { get; set; }
        public DateTime? ParsedOn { get; set; }
    }

    public class ResumeExperienceDto
    {
        public string? Company { get; set; }

        /// <summary>
        /// Named Role, not Position — the Angular ResumeExperience interface
        /// reads role/jobTitle, never position; the old name meant every parsed
        /// experience entry's title was silently blank in the review screen.
        /// </summary>
        public string? Role { get; set; }
        public string? Duration { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }

    public class ResumeEducationDto
    {
        public string? Institution { get; set; }
        public string? Degree { get; set; }

        /// <summary>
        /// Duration/Gpa, not Field/Year — matches Digi.Core.AI's EducationNode
        /// and the Angular ResumeEducation interface. The old fields didn't
        /// exist on the wire (Field was always hardcoded null; Year meant
        /// nothing to the frontend, which reads duration/gpa), so this data
        /// was always blank in the review screen despite the parser returning it.
        /// </summary>
        public string? Duration { get; set; }
        public string? Gpa { get; set; }
    }

    public class ResumeProjectDto
    {
        public string? Name { get; set; }
        public string? Technologies { get; set; }
        public string? Description { get; set; }
    }

    // Rank Candidates Request
    public class RankCandidatesRequestDto
    {
        public int CompanyId { get; set; }
        public int JobRequisitionId { get; set; }
        public List<int> CandidateIds { get; set; } = new List<int>();
    }

    // Rank Candidates Response
    public class RankCandidatesResponseDto
    {
        public List<RankedCandidateDto> RankedCandidates { get; set; } = new List<RankedCandidateDto>();
        public DateTime RankedOn { get; set; }
    }

    public class RankedCandidateDto
    {
        public int CandidateId { get; set; }
        public int Rank { get; set; }
        public int MatchScore { get; set; }
        public string? Recommendation { get; set; }
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
        public decimal Percentile { get; set; } // Calculated percentile (0-100)
    }

    // Get Interview Schedule Suggestions Request
    public class GetInterviewScheduleSuggestionsRequestDto
    {
        public int CompanyId { get; set; }
        public int JobRequisitionId { get; set; }
        public int CandidateId { get; set; }
        public DateTime? PreferredStartDate { get; set; }
        public DateTime? PreferredEndDate { get; set; }
        public int? InterviewDurationMinutes { get; set; }
        public List<int>? InterviewerIds { get; set; }
    }

    // Get Interview Schedule Suggestions Response
    public class GetInterviewScheduleSuggestionsResponseDto
    {
        public List<InterviewSlotDto> SuggestedSlots { get; set; } = new List<InterviewSlotDto>();
        public DateTime GeneratedOn { get; set; }
    }

    public class InterviewSlotDto
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<int> AvailableInterviewers { get; set; } = new List<int>();
        public string? Venue { get; set; }
        public int Priority { get; set; } // 1 = Highest priority
        public string? Reason { get; set; }
    }

    // Get Salary Recommendation Request
    public class GetSalaryRecommendationRequestDto
    {
        public int CompanyId { get; set; }
        public int JobRequisitionId { get; set; }
        public int? CandidateId { get; set; }
        public string? JobTitle { get; set; }
        public string? Location { get; set; }
        public int? YearsOfExperience { get; set; }
        public List<string>? Skills { get; set; }
        public string? EducationLevel { get; set; }
    }

    // Get Salary Recommendation Response
    public class GetSalaryRecommendationResponseDto
    {
        public decimal RecommendedMinSalary { get; set; }
        public decimal RecommendedMaxSalary { get; set; }
        public decimal RecommendedSalary { get; set; }
        public string? Currency { get; set; }
        public string? MarketRange { get; set; }
        public List<SalaryFactorDto> Factors { get; set; } = new List<SalaryFactorDto>();
        public DateTime GeneratedOn { get; set; }
    }

    public class SalaryFactorDto
    {
        public string Factor { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty; // "Positive", "Negative", "Neutral"
        public string Description { get; set; } = string.Empty;
    }

    // Auto Process Application Request
    public class AutoProcessRequestDto
    {
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int RequisitionID { get; set; }
        public string? ResumePath { get; set; }
        public string? ResumeFileName { get; set; }
        public bool EnableAutoParsing { get; set; } = true;
        public bool EnableAutoScreening { get; set; } = true;
        public int AutoShortlistThreshold { get; set; } = 80;
    }

    // Auto Process Application Response
    public class AutoProcessResponseDto
    {
        public int ApplicationID { get; set; }
        public bool ResumeParsed { get; set; }
        public int? ResumeParsingID { get; set; }
        public bool AIScreened { get; set; }
        public int? ScreeningID { get; set; }
        public int? AIScreeningScore { get; set; }
        public bool AutoShortlisted { get; set; }
        public int? NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
    }

    // Auto Parse Resume Request
    public class AutoParseResumeRequestDto
    {
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public string? ResumePath { get; set; }
        public string? ResumeFileName { get; set; }
        public bool IsAutoProcessed { get; set; } = true;
        public string? ParsedDataJson { get; set; } // Saved into Tbl_RecruitmentAI_ResumeParsing.ParsedData
    }

    // Auto Parse Resume Response
    public class AutoParseResumeResponseDto
    {
        public int ParsingID { get; set; }
        public object? ParsedData { get; set; }
        public bool IsAutoProcessed { get; set; }
    }

    // Auto Screen Resume Request
    public class AutoScreenResumeRequestDto
    {
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int RequisitionID { get; set; }
        public int ResumeParsingID { get; set; }
        public bool IsAutoProcessed { get; set; } = true;

        // AI outputs (persisted through SP_AI_AutoScreenResume)
        public int MatchScore { get; set; }
        public string? Recommendation { get; set; }
        public string? SkillsMatch { get; set; }
        public string? ExperienceMatch { get; set; }
        public string? QualificationsMatch { get; set; }
        public string? RedFlags { get; set; }
        public string ScreeningProvider { get; set; } = "AI";
        public string? ModelUsed { get; set; }
        public int TokensUsed { get; set; }
        public int ProcessingTime { get; set; }
        public int AutoShortlistThreshold { get; set; } = 80;
    }

    // Auto Screen Resume Response
    public class AutoScreenResumeResponseDto
    {
        public int ScreeningID { get; set; }
        public int MatchScore { get; set; }
        public string? Recommendation { get; set; }
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Weaknesses { get; set; } = new List<string>();
        public bool AutoShortlistTriggered { get; set; }
        public int? AutoShortlistScore { get; set; }
    }

    // Auto Shortlist Request
    public class AutoShortlistRequestDto
    {
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public decimal AIScreeningScore { get; set; }
        public int Threshold { get; set; } = 80;
    }

    // Auto Shortlist Response
    public class AutoShortlistResponseDto
    {
        public int ApplicationID { get; set; }
        public int? PreviousStatusID { get; set; }
        public string? PreviousStatusCode { get; set; }
        public int? NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
        public bool AutoShortlisted { get; set; }
        public DateTime? AutoShortlistDate { get; set; }
    }

    // Interview Round DTO
    public class InterviewRoundDto
    {
        public int ScheduleID { get; set; }
        public string? ScheduleCode { get; set; }
        public int RoundNumber { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public int? DurationMinutes { get; set; }
        public string? Venue { get; set; }
        public string? OnlineMeetingLink { get; set; }
        public string? Instructions { get; set; }
        public int? StatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
        public string? FeedbackSummary { get; set; }
        public List<InterviewRoundPanelMemberDto> PanelMembers { get; set; } = new List<InterviewRoundPanelMemberDto>();
        public int? EvaluationID { get; set; }
    }

    // Interview Panel Member DTO
    public class InterviewRoundPanelMemberDto
    {
        public int PanelID { get; set; }
        public int InterviewerID { get; set; }
        public string? InterviewerName { get; set; }
        public bool IsPanelHead { get; set; }
        public bool IsRequired { get; set; }
        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedOn { get; set; }
    }

    // Get Interview Rounds Response
    public class GetInterviewRoundsResponseDto
    {
        public int ApplicationID { get; set; }
        public int CurrentRound { get; set; }
        public int TotalRounds { get; set; }
        public List<InterviewRoundDto> Rounds { get; set; } = new List<InterviewRoundDto>();
    }

    // Schedule Interview Round Request
    public class ScheduleInterviewRoundRequestDto
    {
        public int CompanyID { get; set; }
        public int ApplicationID { get; set; }
        public int RoundNumber { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int DurationMinutes { get; set; }
        public string? Venue { get; set; }
        public string? OnlineMeetingLink { get; set; }
        public string? Instructions { get; set; }
        public List<int> PanelMembers { get; set; } = new List<int>();
        public int? InterviewTypeID { get; set; }
        public string? Comments { get; set; }
    }

    // Schedule Interview Round Response
    public class ScheduleInterviewRoundResponseDto
    {
        public int ScheduleID { get; set; }
        public string? ScheduleCode { get; set; }
        public int RoundNumber { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int? StatusID { get; set; }
        public string? StatusCode { get; set; }
        public ApplicationStatusUpdateDto? ApplicationUpdated { get; set; }
    }

    // Application Status Update DTO
    public class ApplicationStatusUpdateDto
    {
        public int ApplicationID { get; set; }
        public int? CurrentStatusID { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusName { get; set; }
    }

    // Complete Interview Round Request
    // CompleteInterviewRoundRequestDto and CompleteInterviewRoundResponseDto moved to RecruitmentAdditionalDtos.cs

    // Get Applications by Interview Status Request
    public class GetApplicationsByInterviewStatusRequestDto
    {
        public int CompanyID { get; set; }
        public string? Status { get; set; } // 'SCHEDULED', 'COMPLETED', 'CANCELLED'
        public int? RoundNumber { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // Application by Interview Status DTO
    public class ApplicationByInterviewStatusDto
    {
        public int ApplicationID { get; set; }
        public string? ApplicationCode { get; set; }
        public string? ApplicantName { get; set; }
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public string? JobTitle { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? StatusName { get; set; }
        public string? StatusCode { get; set; }
        public int? CurrentRound { get; set; }
        public DateTime? ScheduledDate { get; set; }
        public string? RoundStatus { get; set; }
        public decimal? ScreeningScore { get; set; }
    }

    // Get Applications by Interview Status Response
    public class GetApplicationsByInterviewStatusResponseDto
    {
        public List<ApplicationByInterviewStatusDto> Applications { get; set; } = new List<ApplicationByInterviewStatusDto>();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    // =============================================
    // WORKFLOW DTOs - Complete Recruitment Workflow
    // =============================================

    // Manual Shortlist Request
    public class ManualShortlistRequestDto
    {
        [Required]
        public int ApplicationID { get; set; }
        [Required]
        public int CompanyID { get; set; }
        public string? Remarks { get; set; }
    }

    // Manual Shortlist Response
    public class ManualShortlistResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
    }

    // Assign Panel Members Request
    public class AssignPanelMembersRequestDto
    {
        [Required]
        public int ScheduleID { get; set; }
        [Required]
        public int CompanyID { get; set; }
        [Required]
        public List<WorkflowPanelMemberDto> PanelMembers { get; set; } = new();
        public int? ApplicationID { get; set; }   

    }

    public class WorkflowPanelMemberDto
    {
        [Required]
        public int InterviewerID { get; set; }
        public bool IsPanelHead { get; set; } = false;
        public bool IsRequired { get; set; } = true;
    }

    // Assign Panel Members Response
    public class AssignPanelMembersResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PanelCount { get; set; }
    }

    // SubmitEvaluationRequestDto and SubmitEvaluationResponseDto moved to RecruitmentAdditionalDtos.cs

    // Mark as Hired Request
    public class MarkAsHiredRequestDto
    {
        [Required]
        public int ApplicationID { get; set; }
        [Required]
        public int CompanyID { get; set; }
        public bool OfferAccepted { get; set; } = true;
        public string? OfferLetterPath { get; set; }
        public string? Remarks { get; set; }

        public bool OfferLetterBit { get; set; }
        public bool OfferLetterEmailSendBit { get; set; }
        public DateTime? JoiningDate { get; set; }
        public DateTime? OfferDate { get; set; }
        public int? DepartmentID { get; set; }
        public int? DesignationID { get; set; }
        public decimal? Amount { get; set; }
    }

    // Mark as Hired Response
    public class MarkAsHiredResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NewStatusID { get; set; }
        public string? NewStatusCode { get; set; }
    }

    public class ParseJobBankResumeRequestDto
    {
        public int CompanyID { get; set; }
        public int JobBankCandidateID { get; set; }

        public string ResumePath { get; set; }
        public string ResumeFileName { get; set; }

        public string? FileType { get; set; }
        public long? FileSize { get; set; }
    }

    public class JobBankParseResumeResponseDto
    {
        public string? FullName { get; set; }

        public List<string>? Skills { get; set; }

        public decimal? TotalYearsExperience { get; set; }

        public string? CurrentDesignation { get; set; }

        public List<JobBankEducationDto>? Education { get; set; }

        public List<JobBankExperienceDto>? Experience { get; set; }
    }

    public class JobBankEducationDto
    {
        public string? Degree { get; set; }
        public string? Field { get; set; }
        public string? Institution { get; set; }
    }

    public class JobBankExperienceDto
    {
        public string? Company { get; set; }
        public string? Position { get; set; }
        public string? Duration { get; set; }
    }

    public class CandidateAIMatchDto
    {
        public int JobBankCandidateID { get; set; }
        public int MatchScore { get; set; }
        public int MatchPercentage { get; set; }
        public string Recommendation { get; set; } = "";
        public string MatchedSkills { get; set; } = "";
        public string MissingSkills { get; set; } = "";
        public string MatchDetails { get; set; } = "";
    }
}

using Digi.Recruitment.Module.Domain.Services.IServices;
using Digi.Shared.Attributes;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Digi.Recruitment.Module.Controllers
{
    [ApiController]
    [Route("recruitment/api/[controller]")]
    public class RecruitmentAIController : BaseController
    {
        private readonly IRecruitmentAIService _service;
        private readonly IRecruitmentService _recruitmentService;
        private readonly ILogger<RecruitmentAIController> _logger;

        public RecruitmentAIController(IRecruitmentAIService service, IRecruitmentService recruitmentService, ILogger<RecruitmentAIController> logger)
        {
            _service = service;
            _recruitmentService = recruitmentService;
            _logger = logger;
        }

        /// <summary>
        /// Check if API key is configured for the company
        /// </summary>
        [HttpGet("CheckApiKeyStatus/{companyId}")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyStatusResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckApiKeyStatus(int companyId)
        {
            try
            {
                var result = await _service.GetApiKeyStatusAsync(companyId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API key status for CompanyID: {CompanyID}", companyId);
                return StatusCode(500, ApiResponse<ApiKeyStatusResponseDto>.Fail("An error occurred while checking API key status"));
            }
        }

        /// <summary>
        /// Get API key configuration and settings
        /// </summary>
        [HttpGet("GetApiKeySettings/{companyId}")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeySettingsResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApiKeySettings(int companyId)
        {
            try
            {
                var result = await _service.GetApiKeySettingsAsync(companyId);
                if (!result.IsSuccess)
                {
                    return NotFound(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key settings for CompanyID: {CompanyID}", companyId);
                return StatusCode(500, ApiResponse<ApiKeySettingsResponseDto>.Fail("An error occurred while retrieving settings"));
            }
        }

        /// <summary>
        /// Save or update API key configuration
        /// </summary>
        [HttpPost("SaveApiKeySettings")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<SaveApiKeySettingsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveApiKeySettings([FromBody] SaveApiKeySettingsRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<SaveApiKeySettingsResponseDto>.Fail("Request body is required"));
                }

                var userId = GetCurrentUserId().ToString();
                var result = await _service.SaveApiKeySettingsAsync(request, userId);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving API key settings");
                return StatusCode(500, ApiResponse<SaveApiKeySettingsResponseDto>.Fail("An error occurred while saving API key settings"));
            }
        }

        /// <summary>
        /// Test if API key is valid and working
        /// </summary>
        [HttpPost("TestApiKey")]
        [ProducesResponseType(typeof(ApiResponse<TestApiKeyResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<TestApiKeyResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> TestApiKey([FromBody] TestApiKeyRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<TestApiKeyResponseDto>.Fail("Request body is required"));
                }

                var result = await _service.TestApiKeyAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API key");
                return StatusCode(500, ApiResponse<TestApiKeyResponseDto>.Fail("An error occurred while testing API key"));
            }
        }

        /// <summary>
        /// Delete API key configuration
        /// </summary>
        [HttpDelete("DeleteApiKey/{companyId}")]
        [AuditLog("RECRUITMENT", "Delete", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteApiKey(int companyId)
        {
            try
            {
                var result = await _service.DeleteApiKeyAsync(companyId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key for CompanyID: {CompanyID}", companyId);
                return StatusCode(500, ApiResponse<bool>.Fail("An error occurred while deleting API key"));
            }
        }

        /// <summary>
        /// Get recruitment dashboard stats (Job Candidates, Job Applications Rejected, Total Jobs, Total Interviews) from sp_Dashboard_RecStats.
        /// </summary>
        [HttpGet("Dashboard/RecStats")]
        [ProducesResponseType(typeof(ApiResponse<RecDashboardRecStatsResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardRecStats()
        {
            try
            {
                var result = await _recruitmentService.GetDashboardRecStatsAsync();
                if (!result.IsSuccess)
                    return BadRequest(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recruitment dashboard RecStats");
                return StatusCode(500, ApiResponse<RecDashboardRecStatsResponseDto>.Fail("An error occurred while retrieving recruitment dashboard stats"));
            }
        }

        /// <summary>
        /// Get AI dashboard statistics
        /// </summary>
        [HttpGet("GetDashboardStats/{companyId}")]
        [ProducesResponseType(typeof(ApiResponse<DashboardStatsResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardStats(int companyId)
        {
            try
            {
                var result = await _service.GetDashboardStatsAsync(companyId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats for CompanyID: {CompanyID}", companyId);
                return StatusCode(500, ApiResponse<DashboardStatsResponseDto>.Fail("An error occurred while retrieving statistics"));
            }
        }

        /// <summary>
        /// Generate job description using AI
        /// </summary>
        [HttpPost("GenerateJobDescription")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<GenerateJobDescriptionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateJobDescription([FromBody] GenerateJobDescriptionRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<GenerateJobDescriptionResponseDto>.Fail("Request body is required"));
                }

                if (string.IsNullOrWhiteSpace(request.JobTitle))
                {
                    return BadRequest(ApiResponse<GenerateJobDescriptionResponseDto>.Fail("JobTitle is required"));
                }

                var result = await _service.GenerateJobDescriptionAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating job description");
                return StatusCode(500, ApiResponse<GenerateJobDescriptionResponseDto>.Fail("An error occurred while generating job description"));
            }
        }

        /// <summary>
        /// Screen resume using AI against job requirements
        /// Use Case: Job application review screen mein candidate ki resume ko job requirements ke against screen karta hai
        /// Frontend Usage: 
        /// - Job Application Review Page: "Screen Resume" button click karne pe
        /// - Candidate Shortlisting: Auto-screening ke liye
        /// - Resume Comparison: Multiple candidates compare karne ke liye
        /// </summary>
        [HttpPost("ScreenResume")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<ScreenResumeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ScreenResume([FromBody] ScreenResumeRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<ScreenResumeResponseDto>.Fail("Request body is required"));
                }

                // ResumeFilePath is required - backend will extract text from file
                if (string.IsNullOrWhiteSpace(request.ResumeFilePath))
                {
                    return BadRequest(ApiResponse<ScreenResumeResponseDto>.Fail("ResumeFilePath is required"));
                }

                if (request.JobRequirements == null)
                {
                    return BadRequest(ApiResponse<ScreenResumeResponseDto>.Fail("JobRequirements is required"));
                }

                var result = await _service.ScreenResumeAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error screening resume");
                return StatusCode(500, ApiResponse<ScreenResumeResponseDto>.Fail("An error occurred while screening resume"));
            }
        }

        /// <summary>
        /// Match candidate to job requirements
        /// </summary>
        [HttpPost("MatchCandidate")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<MatchCandidateResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> MatchCandidate([FromBody] MatchCandidateRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<MatchCandidateResponseDto>.Fail("Request body is required"));
                }

                if (request.CandidateProfile == null)
                {
                    return BadRequest(ApiResponse<MatchCandidateResponseDto>.Fail("CandidateProfile is required"));
                }

                if (request.JobRequirements == null)
                {
                    return BadRequest(ApiResponse<MatchCandidateResponseDto>.Fail("JobRequirements is required"));
                }

                var result = await _service.MatchCandidateAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error matching candidate");
                return StatusCode(500, ApiResponse<MatchCandidateResponseDto>.Fail("An error occurred while matching candidate"));
            }
        }

        /// <summary>
        /// Save/Update job description
        /// </summary>
        [HttpPost("SaveJobDescription")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<SaveJobDescriptionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveJobDescription([FromBody] SaveJobDescriptionRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<SaveJobDescriptionResponseDto>.Fail("Request body is required"));
                }

                if (string.IsNullOrWhiteSpace(request.JobDescription))
                {
                    return BadRequest(ApiResponse<SaveJobDescriptionResponseDto>.Fail("Job description is required"));
                }

                var result = await _service.SaveJobDescriptionAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving job description");
                return StatusCode(500, ApiResponse<SaveJobDescriptionResponseDto>.Fail("An error occurred while saving job description"));
            }
        }

        /// <summary>
        /// Generate interview questions using AI
        /// </summary>
        [HttpPost("GenerateInterviewQuestions")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<GenerateInterviewQuestionsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateInterviewQuestions([FromBody] GenerateInterviewQuestionsRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("Request body is required"));
                }

                if (string.IsNullOrWhiteSpace(request.JobTitle))
                {
                    return BadRequest(ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("JobTitle is required"));
                }

                if (request.NumberOfQuestions <= 0 || request.NumberOfQuestions > 50)
                {
                    return BadRequest(ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("NumberOfQuestions must be between 1 and 50"));
                }

                var validQuestionTypes = new[] { "technical", "behavioral", "mixed" };
                if (!validQuestionTypes.Contains(request.QuestionType.ToLower()))
                {
                    return BadRequest(ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("QuestionType must be one of: technical, behavioral, mixed"));
                }

                var result = await _service.GenerateInterviewQuestionsAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating interview questions");
                return StatusCode(500, ApiResponse<GenerateInterviewQuestionsResponseDto>.Fail("An error occurred while generating interview questions"));
            }
        }

        /// <summary>
        /// Save AI feature settings
        /// </summary>
        [HttpPost("SaveSettings")]
        [AuditLog("RECRUITMENT", "Update", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<SaveSettingsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveSettings([FromBody] SaveSettingsRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<SaveSettingsResponseDto>.Fail("Request body is required"));
                }

                if (request.Settings == null)
                {
                    return BadRequest(ApiResponse<SaveSettingsResponseDto>.Fail("Settings is required"));
                }

                var result = await _service.SaveSettingsAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                return StatusCode(500, ApiResponse<SaveSettingsResponseDto>.Fail("An error occurred while saving settings"));
            }
        }

        /// <summary>
        /// Get AI feature settings
        /// </summary>
        [HttpGet("GetSettings/{companyId}")]
        [ProducesResponseType(typeof(ApiResponse<GetSettingsResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettings(int companyId)
        {
            try
            {
                var result = await _service.GetSettingsAsync(companyId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting settings for CompanyID: {CompanyID}", companyId);
                return StatusCode(500, ApiResponse<GetSettingsResponseDto>.Fail("An error occurred while retrieving settings"));
            }
        }

        /// <summary>
        /// Parse resume text and extract structured information
        /// </summary>
        [HttpPost("ParseResume")]
        [AllowAnonymous]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<ParseResumeResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ParseResume([FromBody] ParseResumeRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<ParseResumeResponseDto>.Fail("Request body is required"));
                }

                // Validation
                // JSON serializer automatically maps "companyID" -> CompanyId and "resumePath" -> ResumeFilePath
                if (string.IsNullOrWhiteSpace(request.ResumeFilePath))
                {
                    return BadRequest(ApiResponse<ParseResumeResponseDto>.Fail("resumePath is required"));
                }

                if (request.CompanyId <= 0)
                {
                    return BadRequest(ApiResponse<ParseResumeResponseDto>.Fail("companyID is required"));
                }

                var result = await _service.ParseResumeAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing resume");
                return StatusCode(500, ApiResponse<ParseResumeResponseDto>.Fail("An error occurred while parsing resume"));
            }
        }

        /// <summary>
        /// Rank candidates based on job requirements
        /// </summary>
        [HttpPost("RankCandidates")]
        [AuditLog("RECRUITMENT", "Create", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<RankCandidatesResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RankCandidates([FromBody] RankCandidatesRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<RankCandidatesResponseDto>.Fail("Request body is required"));
                }

                if (request.CandidateIds == null || !request.CandidateIds.Any())
                {
                    return BadRequest(ApiResponse<RankCandidatesResponseDto>.Fail("At least one Candidate ID is required"));
                }

                var result = await _service.RankCandidatesAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ranking candidates");
                return StatusCode(500, ApiResponse<RankCandidatesResponseDto>.Fail("An error occurred while ranking candidates"));
            }
        }

        /// <summary>
        /// Get interview schedule suggestions
        /// </summary>
        [HttpPost("GetInterviewScheduleSuggestions")]
        [AuditLog("RECRUITMENT", "Read", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<GetInterviewScheduleSuggestionsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetInterviewScheduleSuggestions([FromBody] GetInterviewScheduleSuggestionsRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("Request body is required"));
                }

                var result = await _service.GetInterviewScheduleSuggestionsAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview schedule suggestions");
                return StatusCode(500, ApiResponse<GetInterviewScheduleSuggestionsResponseDto>.Fail("An error occurred while generating suggestions"));
            }
        }

        /// <summary>
        /// Get salary recommendation for a position
        /// </summary>
        [HttpPost("GetSalaryRecommendation")]
        [AuditLog("RECRUITMENT", "Read", "RecruitmentAI")]
        [ProducesResponseType(typeof(ApiResponse<GetSalaryRecommendationResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetSalaryRecommendation([FromBody] GetSalaryRecommendationRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(ApiResponse<GetSalaryRecommendationResponseDto>.Fail("Request body is required"));
                }

                var result = await _service.GetSalaryRecommendationAsync(request);
                
                if (!result.IsSuccess)
                {
                    return BadRequest(result);
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting salary recommendation");
                return StatusCode(500, ApiResponse<GetSalaryRecommendationResponseDto>.Fail("An error occurred while generating salary recommendation"));
            }
        }

        [HttpPost("ParseJobBankResume")]
        [AuditLog("RECRUITMENT", "Create", "JobBankResumeParsing")]
        [ProducesResponseType(typeof(ApiResponse<ParseResumeResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ParseJobBankResume([FromBody] ParseJobBankResumeRequestDto request)
        {
            try
            {
                if (request == null)
                    return BadRequest(ApiResponse<string>.Fail("Request body required"));

                if (request.CompanyID <= 0)
                    return BadRequest(ApiResponse<string>.Fail("CompanyID required"));

                if (request.JobBankCandidateID <= 0)
                    return BadRequest(ApiResponse<string>.Fail("JobBankCandidateID required"));

                if (string.IsNullOrWhiteSpace(request.ResumePath))
                    return BadRequest(ApiResponse<string>.Fail("ResumePath required"));

                var result = await _service.ParseJobBankResumeAsync(request);

                if (!result.IsSuccess)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing job bank resume");
                return StatusCode(500, ApiResponse<string>.Fail("Internal server error"));
            }
        }

        [HttpGet("GetSavedAIMatches/{jobRequisitionId}")]
        [ProducesResponseType(typeof(ApiResponse<List<CandidateAIMatchDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSavedAIMatches(int jobRequisitionId)
        {
            if (jobRequisitionId <= 0)
                return BadRequest(ApiResponse<string>.Fail("JobRequisitionId required"));
            var companyID = GetCurrentCompanyId();
            var result = await _service.GetSavedAIMatchesAsync((int)companyID,jobRequisitionId);
            return Ok(result);
        }
    }
}

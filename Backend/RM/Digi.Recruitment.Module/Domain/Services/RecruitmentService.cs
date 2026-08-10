//using Digi.Admin.Module.Domain.Services.IServices;
using Digi.Recruitment.Module.Domain.Repositories;
using Digi.Recruitment.Module.Domain.Repositories.IRepositories;
using Digi.Recruitment.Module.Domain.Services.IServices;
using Digi.Shared.DTOs;
using Digi.Shared.DTOs.admin.module;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Digi.Shared.Services;
using Digi.Shared.SharedLibrary.Interfaces;
using iText.Html2pdf;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Digi.Recruitment.Module.Domain.Services
{
    public class RecruitmentService : IRecruitmentService
    {
        private readonly IRecruitmentRepository _repo;
        private readonly ILogger<RecruitmentService> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly IWorkflowService _workflowService;
        private readonly ICentralizedEmailService _emailService;
        private readonly IRecruitmentAIService _aiService;
        private readonly IFileService _fileService;
        //private readonly ICompanyRegistrationService  _companyService;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public RecruitmentService(IRecruitmentRepository repo, HttpClient httpClient, IConfiguration configuration, IFileService fileService, IFileStorageService fileStorageService, ILogger<RecruitmentService> logger, IWorkflowService workflowService, ICentralizedEmailService emailService, IRecruitmentAIService aiService)
        { //, ICompanyRegistrationService companyService 
            _repo = repo;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _workflowService = workflowService;
            _emailService = emailService;
            _aiService = aiService;
            _fileService = fileService;
            //_companyService = companyService;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        //public async Task<ApiResponse<RecruitmentRequisitionDto?>> SaveAsync(SaveRecruitmentRequisitionRequest request,int? employeeID, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        // 🔹 1. Validate required fields
        //        if (request.CompanyID <= 0)
        //            return ApiResponse<RecruitmentRequisitionDto?>.Fail("CompanyID is required.");

        //        if (string.IsNullOrWhiteSpace(request.EmployeeCode))
        //            return ApiResponse<RecruitmentRequisitionDto?>.Fail("EmployeeCode is required.");
        //        if (request.IsSystemDefault == true)
        //        {
        //            request.ApprovalStatus = "Published";
        //            request.PublishStatus = "Published";
        //            request.PublishedDate = DateTime.Now;
        //            request.PublishedBy = request.EmployeeCode;
        //        }
        //        // 🔹 2. Check approval flow ONLY when not system default
        //        if (!(request.IsSystemDefault ?? false))
        //        {
        //            bool isFlowConfigured = await _workflowService.IsApprovalFlowConfiguredAsync("Recruitment", (int)request.CompanyID!,employeeID);

        //            if (!isFlowConfigured)
        //                return ApiResponse<RecruitmentRequisitionDto?>
        //                    .Fail("Approval flow is not configured for Recruitment.");
        //        }

        //        // 🔹 3. Save recruitment request
        //        var (newId, isSuccess, message, row) =
        //            await _repo.SaveAsync(request, cancellationToken);

        //        if (!isSuccess)
        //            return ApiResponse<RecruitmentRequisitionDto?>.Fail(message);

        //        if (row == null && newId.HasValue)
        //            row = await _repo.GetByIdAsync(newId.Value, cancellationToken);

        //        // 🔹 4. Start approval workflow ONLY if not system default
        //        if (newId > 0 && !(request.IsSystemDefault ?? false))
        //        {
        //            await _workflowService.StartApprovalWorkflowAsync(
        //                "Recruitment",
        //                newId.Value,
        //                (int)request.EmployeeID!,
        //                (int)request.CompanyID!,
        //                request.EmployeeCode!
        //            );

        //            // 🔹 Send email notifications to approvers
        //            try
        //            {
        //                var approverEmails =
        //                    await _workflowService.GetApproverEmailsByWorkflowAsync(
        //                        "Recruitment",
        //                        newId.Value,
        //                        (int)request.CompanyID!
        //                    );

        //                if (approverEmails != null && approverEmails.Any())
        //                {
        //                    var subject = "New Recruitment Requisition - Approval Required";
        //                    var emailBody = $@"
        //            <html>
        //            <body>
        //                <h2>New Recruitment Requisition Submitted</h2>
        //                <p>A new recruitment requisition has been submitted and requires your approval.</p>
        //                <p><strong>Requisition ID:</strong> {newId}</p>
        //                <p><strong>Employee Code:</strong> {request.EmployeeCode}</p>
        //                <p><strong>Submitted On:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        //                <p>This is an automated notification from HRM Module.</p>
        //            </body>
        //            </html>";

        //                    foreach (var approver in approverEmails)
        //                    {
        //                        if (string.IsNullOrWhiteSpace(approver.Email))
        //                            continue;

        //                        try
        //                        {
        //                            var decryptedEmail = EncryptionHelper.DecryptText(approver.Email);
        //                            if (string.IsNullOrWhiteSpace(decryptedEmail) || !decryptedEmail.Contains("@"))
        //                                decryptedEmail = approver.Email;

        //                            await _emailService.SendEmailAsync(
        //                                (int)request.CompanyID!,
        //                                decryptedEmail,
        //                                subject,
        //                                emailBody,
        //                                isHtml: true
        //                            );
        //                        }
        //                        catch (Exception emailEx)
        //                        {
        //                            _logger.LogError(emailEx,
        //                                "Error sending email to {Email}", approver.Email);
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception emailEx)
        //            {
        //                _logger.LogError(emailEx,
        //                    "Error sending email notifications for recruitment requisition {FormID}", newId);
        //            }
        //        }

        //        return ApiResponse<RecruitmentRequisitionDto?>
        //            .Success(row, message ?? "Recruitment requisition created successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "RecruitmentService.SaveAsync error");
        //        return ApiResponse<RecruitmentRequisitionDto?>
        //            .Fail($"Error saving recruitment requisition: {ex.Message}");
        //    }
        //}




        
        //public async Task<ApiResponse<RecruitmentRequisitionDto?>> UpdateRecruitmentAsync(UpdateRecruitmentRequisitionRequest request,
        // CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        if (request.RecruitmentRequisitionID <= 0)
        //            return ApiResponse<RecruitmentRequisitionDto?>.Fail("RecruitmentRequisitionID is required.");


        //        var (isSuccess, message, row) = await _repo.UpdateAsync(request, cancellationToken);

        //        if (!isSuccess)
        //            return ApiResponse<RecruitmentRequisitionDto?>.Fail(message);

        //        return ApiResponse<RecruitmentRequisitionDto?>.Success(row, message);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "RecruitmentService.UpdateRecruitmentAsync error");
        //        return ApiResponse<RecruitmentRequisitionDto?>.Fail(ex.Message);
        //    }
        //}


        //public async Task<ApiResponse<RecruitmentRequisitionDto?>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var row = await _repo.GetByIdAsync(id, cancellationToken);
        //        if (row == null) return ApiResponse<RecruitmentRequisitionDto?>.Fail("Record not found.");
        //        return ApiResponse<RecruitmentRequisitionDto?>.Success(row);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "RecruitmentService.GetByIdAsync error");
        //        return ApiResponse<RecruitmentRequisitionDto?>.Fail("An unexpected error occurred.");
        //    }
        //}

        //public async Task<ApiResponse<IEnumerable<RecruitmentRequisitionGetDto>>> GetAllAsync(int? companyId, int? year, bool? isPublished, string? searchText, int pageNumber, int pageSize, string? employeeCode)
        //{
        //    try
        //    {
        //        var data = await _repo.GetAllAsync(companyId, year, isPublished, searchText, pageNumber, pageSize, employeeCode);

        //        return ApiResponse<IEnumerable<RecruitmentRequisitionGetDto>>.Success(data, "Data retrieved successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<RecruitmentRequisitionGetDto>>.Fail(ex.Message);
        //    }
        //}

        //public async Task<ApiResponse<RecruitmentRequisitionGetDto>> GetByIdAsync(int requisitionId)
        //{
        //    try
        //    {
        //        var data = await _repo.GetByIdAsync(requisitionId);
        //        if (data == null)
        //        {
        //            return ApiResponse<RecruitmentRequisitionGetDto>.Fail("Record not found");

        //        }

        //        return ApiResponse<RecruitmentRequisitionGetDto>.Success(data, "Data retrieved successfully");
        //    }
        //    catch (Exception ex)
        //    {

        //        return ApiResponse<RecruitmentRequisitionGetDto>.Fail(ex.Message);
        //    }


        //}

        //public async Task<ApiResponse<IEnumerable<RecruitmentSummaryDto>>> GetSummaryAsync(int? companyId, int? year)
        //{
        //    try
        //    {
        //        var data = await _repo.GetSummaryAsync(companyId, year);
        //        return ApiResponse<IEnumerable<RecruitmentSummaryDto>>.Success(data, "Summary retrieved successfully");
        //    }
        //    catch (Exception ex)
        //    {

        //        return ApiResponse<IEnumerable<RecruitmentSummaryDto>>.Fail(ex.Message);
        //    }


        //}

        //public async Task<ApiResponse<IEnumerable<CandidateAssignGroupDto>>> GetSummaryAsync(int? companyId, string? filterBy, int? year, string? searchText)
        //{
        //    try
        //    {
        //        var data = await _repo.GetSummaryAsync(companyId, filterBy, year, searchText);
        //        return ApiResponse<IEnumerable<CandidateAssignGroupDto>>.Success(data, data.Any() ? "Summary data retrieved successfully." : "No data found.");
        //    }
        //    catch (Exception ex)
        //    {

        //        return ApiResponse<IEnumerable<CandidateAssignGroupDto>>.Fail(ex.Message);
        //    }

        //}

        //public async Task<ApiResponse<RecruitmentJobDetailDto>> GetJobDetailsAsync(int recruitmentRequisitionId)
        //{
        //    var result =   await _repo.GetJobDetailsAsync(recruitmentRequisitionId);
        //    if (result == null)
        //    {
        //        return ApiResponse<RecruitmentJobDetailDto>.Fail("Job details not found.");
        //    }

        //    return ApiResponse<RecruitmentJobDetailDto>.Success(result, "Job details retrieved successfully.");
        //}
        //public async Task<ApiResponse<IEnumerable<JobApplicationDto>>> GetApplicationsByRequisitionAsync(int jobRequisitionId, int companyID)
        //{
        //    var result = await _repo.GetApplicationsByRequisitionAsync(jobRequisitionId, companyID);
        //    if (result == null || !result.Any())
        //    {
        //        return ApiResponse<IEnumerable<JobApplicationDto>>.Fail("No applications found for this requisition.");
        //    }
        //    foreach (var emp in result)
        //    {
        //        if (emp.ResumePath != null && emp.ResumePath.Any())
        //        {
        //            emp.ResumePath = _fileStorageService.GetFullUrl(emp.ResumePath);
        //        }
        //    }
        //    return ApiResponse<IEnumerable<JobApplicationDto>>.Success(result, "Applications retrieved successfully.");

        //}

        //public async Task<ApiResponse<IEnumerable<JobApplicationByShortListedDto>>> GetApplicationsByShortListedAsync(int jobRequisitionId, int companyID)
        //{
        //    var result = await _repo.GetApplicationsByShortListedAsync(jobRequisitionId, companyID);
        //    if (result == null || !result.Any())
        //    {
        //        return ApiResponse<IEnumerable<JobApplicationByShortListedDto>>.Fail("No applications found for this requisition.");
        //    }
        //    foreach (var emp in result)
        //    {
        //        if (emp.ResumePath != null && emp.ResumePath.Any())
        //        {
        //            emp.ResumePath = _fileStorageService.GetFullUrl(emp.ResumePath);
        //        }
        //    }
        //    return ApiResponse<IEnumerable<JobApplicationByShortListedDto>>.Success(result, "Applications retrieved successfully.");

        //}
        //public async Task<ApiResponse<bool>> UpdateApplicationStatusAsync(UpdateJobApplicationStatusDto dto)
        //{
        //    var result = await _repo.UpdateApplicationStatusAsync(dto);
        //    if (result == 0)
        //    {
        //        return ApiResponse<bool>.Fail("Failed to update application status. Please try again.");
        //    }
        //    return ApiResponse<bool>.Success(true, "Application status updated successfully.");
        //}
        //public async Task<ApiResponse<IEnumerable<RecruitmentRequisitionPublicDto>>> ManagePublicAsync(RecruitmentRequisitionActionDto dto)
        //{
        //    var result = await _repo.ManagePublicAsync(dto);
        //    if (result == null || !result.Any())
        //    {
        //        return ApiResponse<IEnumerable<RecruitmentRequisitionPublicDto>>.Fail("No public requisitions found.");
        //    }
        //    return ApiResponse<IEnumerable<RecruitmentRequisitionPublicDto>>.Success(result, "Public requisitions managed successfully.");
        //}

        //public async Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetInterviewStatusesAsync()
        //{
        //    try
        //    {
        //        var result = await _repo.GetInterviewStatusesAsync();
        //        if (result == null || !result.Any())
        //        {
        //            return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail("No interview statuses found.");
        //        }
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Success(result, "Interview statuses retrieved successfully.");    
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail($"Error retrieving interview statuses: {ex.Message}");
        //    }

        //}

        //public async Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetRecommendationAsync()
        //{
        //    try
        //    {
        //        var result = await _repo.GetRecommendationAsync();
        //        if (result == null || !result.Any())
        //        {
        //            return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail("No interview statuses found.");
        //        }
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Success(result, "Interview statuses retrieved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail($"Error retrieving interview statuses: {ex.Message}");
        //    }

        //}
        //public async Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetVenueAsync()
        //{
        //    try
        //    {
        //        var result = await _repo.GetVenueAsync();
        //        if (result == null || !result.Any())
        //        {
        //            return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail("No interview statuses found.");
        //        }
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Success(result, "Interview statuses retrieved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail($"Error retrieving interview statuses: {ex.Message}");
        //    }

        //}
        //public async Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetNotificationMethodAsync()
        //{
        //    try
        //    {
        //        var result = await _repo.GetNotificationMethodAsync();
        //        if (result == null || !result.Any())
        //        {
        //            return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail("No interview statuses found.");
        //        }
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Success(result, "Interview statuses retrieved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail($"Error retrieving interview statuses: {ex.Message}");
        //    }

        //}
        //public async Task<ApiResponse<IEnumerable<ApplicationStatusDto>>> GetOtherStageStatusesAsync()
        //{
        //    try
        //    {
        //        var result = await _repo.GetOtherStageStatusesAsync();
        //        if (result == null || !result.Any())
        //        {
        //            return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail("No other stage statuses found.");
        //        }
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Success(result, "Other stage statuses retrieved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<ApplicationStatusDto>>.Fail($"Error retrieving other stage statuses: {ex.Message}");
        //    }
        //}
        //public async Task<ApiResponse<IEnumerable<ShortlistedCandidateDto>>> GetAllShortlistedAsync(int CompanyID)
        //{
        //    try
        //    {
        //        var result = await _repo.GetAllShortlistedAsync(CompanyID);
        //        if (result == null || !result.Any())
        //        {
        //            return ApiResponse<IEnumerable<ShortlistedCandidateDto>>.Fail("No shortlisted candidates found.");
        //        }
        //        return ApiResponse<IEnumerable<ShortlistedCandidateDto>>.Success(result, "Shortlisted candidates retrieved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResponse<IEnumerable<ShortlistedCandidateDto>>.Fail($"Error retrieving shortlisted candidates: {ex.Message}");
        //    }
        //}
        //public async Task<ApiResponse<bool>> UpdateInterviewScheduleAsync(bool isHired, int jobApplicationID, int interviewStateID, int applicantID, int companyID, string empCode)
        //{
        //    try
        //    {
        //        await _repo.UpdateInterviewScheduleAsync(isHired,jobApplicationID,interviewStateID,applicantID,companyID,empCode);

        //        // Assuming SaveInterviewScheduleAsync returns void or throws an exception on failure
        //        // If it returns a result, you can check it here and return appropriate response
        //        return ApiResponse<bool>.Success(true, "Interview ishired successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error update interview schedule");
        //        return ApiResponse<bool>.Fail($"Failed to update interview schedule: {ex.Message}");
        //    }
        //}

        //public async Task<ApiResponse<bool>> SaveInterviewScheduleAsync(InterviewScheduleRequestDto request)
        //{
        //    try
        //    {
        //       await _repo.SaveInterviewScheduleAsync(request);

        //        // Assuming SaveInterviewScheduleAsync returns void or throws an exception on failure
        //        // If it returns a result, you can check it here and return appropriate response
        //        return ApiResponse<bool>.Success(true, "Interview schedule saved successfully.");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error saving interview schedule");
        //        return ApiResponse<bool>.Fail($"Failed to save interview schedule: {ex.Message}");
        //    }
        //}
        public async Task<ApiResponse<InterviewScheduleCollectionDto>> GetAllInterviewSchedulesAsync(int CompanyID)
        {
            try
            {
                var result = await _repo.GetAllInterviewSchedulesAsync(CompanyID);
                if (result == null)
                {
                    return ApiResponse<InterviewScheduleCollectionDto>.Fail("No interview schedules found.");
                }
                return ApiResponse<InterviewScheduleCollectionDto>.Success(result, "Interview schedules retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interview schedules");
                return ApiResponse<InterviewScheduleCollectionDto>.Fail($"Failed to retrieve interview schedules: {ex.Message}");
            }
        }
        public async Task<ApiResponse<IEnumerable<CandidateEvaluationDto>>> GetCandidateEvaluationsAsync(long? requisitionId, int? interviewRound)
        {
            try
            {
                var result = await _repo.GetCandidateEvaluationsAsync(requisitionId, interviewRound);
                if (result == null || !result.Any())
                {
                    return ApiResponse<IEnumerable<CandidateEvaluationDto>>.Fail("No candidate evaluations found.");
                }
                return ApiResponse<IEnumerable<CandidateEvaluationDto>>.Success(result, "Candidate evaluations retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving candidate evaluations");
                return ApiResponse<IEnumerable<CandidateEvaluationDto>>.Fail($"Failed to retrieve candidate evaluations: {ex.Message}");
            }            
        }
        //public async Task<EmployeeEmailDto> GetEmployeeEmailsAsync(int employeeIds)
        //{
        //    // Convert List<int> to comma-separated string
        //    //string ids = string.Join(",", employeeIds);
        //    return await _repo.GetEmployeeEmailsAsync(employeeIds);
        //}

        //public async Task<ApiResponse<bool>> DeleteRecruitmentRequisitionAsync(int recruitmentRequisitionID,string employeeCode,string? reasonToDelete)
        //{
        //    return await _repo.DeleteRecruitmentRequisitionAsync( recruitmentRequisitionID,employeeCode,reasonToDelete);
        //}

        // Auto Process Methods
        public async Task<ApiResponse<AutoProcessResponseDto>> AutoProcessApplicationAsync(AutoProcessRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var response = new AutoProcessResponseDto { ApplicationID = request.ApplicationID };

                // 1. Auto Parse Resume
                if (request.EnableAutoParsing)
                {
                    var parseRequest = new AutoParseResumeRequestDto
                    {
                        CompanyID = request.CompanyID,
                        ApplicationID = request.ApplicationID,
                        ApplicantID = request.ApplicantID,
                        ResumePath = request.ResumePath,
                        ResumeFileName = request.ResumeFileName,
                        IsAutoProcessed = true
                    };

                    var parseResult = await AutoParseResumeAsync(parseRequest);
                    if (parseResult.IsSuccess && parseResult.Data is AutoParseResumeResponseDto parseData)
                    {
                        response.ResumeParsed = true;
                        response.ResumeParsingID = parseData.ParsingID;
                    }
                }

                // 2. Auto Screen Resume
                if (request.EnableAutoScreening && response.ResumeParsingID.HasValue)
                {
                    var screenRequest = new AutoScreenResumeRequestDto
                    {
                        CompanyID = request.CompanyID,
                        ApplicationID = request.ApplicationID,
                        ApplicantID = request.ApplicantID,
                        RequisitionID = request.RequisitionID,
                        ResumeParsingID = response.ResumeParsingID.Value,
                        IsAutoProcessed = true,
                        AutoShortlistThreshold = request.AutoShortlistThreshold
                    };

                    var screenResult = await AutoScreenResumeAsync(screenRequest);
                    if (screenResult.IsSuccess && screenResult.Data is AutoScreenResumeResponseDto screenData)
                    {
                        response.AIScreened = true;
                        response.ScreeningID = screenData.ScreeningID;
                        response.AIScreeningScore = screenData.MatchScore;

                        // 3. Auto Shortlist if threshold met
                        if (screenData.MatchScore >= request.AutoShortlistThreshold)
                        {
                            var shortlistRequest = new AutoShortlistRequestDto
                            {
                                CompanyID = request.CompanyID,
                                ApplicationID = request.ApplicationID,
                                AIScreeningScore = screenData.MatchScore,
                                Threshold = request.AutoShortlistThreshold
                            };

                            var shortlistResult = await AutoShortlistCandidateAsync(shortlistRequest);
                            if (shortlistResult.IsSuccess && shortlistResult.Data is AutoShortlistResponseDto shortlistData)
                            {
                                response.AutoShortlisted = true;
                                response.NewStatusID = shortlistData.NewStatusID;
                                response.NewStatusCode = shortlistData.NewStatusCode;
                            }
                        }
                    }
                }

                return ApiResponse<AutoProcessResponseDto>.Success(response, "Application processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoProcessApplicationAsync");
                return ApiResponse<AutoProcessResponseDto>.Fail($"Error processing application: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AutoParseResumeResponseDto>> AutoParseResumeAsync(AutoParseResumeRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                
                // Call AI service to parse resume (first), then persist via SP
                var aiParseRequest = new ParseResumeRequestDto
                {
                    CompanyId = request.CompanyID,
                    ResumeText = "", // Will be read from file path
                    ResumeFilePath = request.ResumePath
                };

                var aiResult = await _aiService.ParseResumeAsync(aiParseRequest);
                if (!aiResult.IsSuccess)
                    return ApiResponse<AutoParseResumeResponseDto>.Fail($"AI parsing failed: {aiResult.Message}");

                if (aiResult.Data is not ParseResumeResponseDto parsedResume)
                    return ApiResponse<AutoParseResumeResponseDto>.Fail("AI parsing failed: invalid response format");

                request.ParsedDataJson = JsonConvert.SerializeObject(parsedResume);

                var (parsingID, isSuccess, message) = await _repo.AutoParseResumeAsync(request, employeeCode);
                if (!isSuccess)
                    return ApiResponse<AutoParseResumeResponseDto>.Fail(message);

                return ApiResponse<AutoParseResumeResponseDto>.Success(
                    new AutoParseResumeResponseDto
                    {
                        ParsingID = parsingID,
                        ParsedData = parsedResume,
                        IsAutoProcessed = request.IsAutoProcessed
                    },
                    "Resume parsed successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoParseResumeAsync");
                return ApiResponse<AutoParseResumeResponseDto>.Fail($"Error parsing resume: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AutoScreenResumeResponseDto>> AutoScreenResumeAsync(AutoScreenResumeRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();

                // Call AI service to screen resume
                var aiScreenRequest = new ScreenResumeRequestDto
                {
                    CompanyId = request.CompanyID,
                    ResumeId = request.ResumeParsingID,
                    ResumeText = "", // Optional: if you have extracted text, pass here
                    JobRequirements = new JobRequirementsDto()
                };

                var aiResult = await _aiService.ScreenResumeAsync(aiScreenRequest);
                if (!aiResult.IsSuccess)
                    return ApiResponse<AutoScreenResumeResponseDto>.Fail($"AI screening failed: {aiResult.Message}");

                if (aiResult.Data is not ScreenResumeResponseDto screened)
                    return ApiResponse<AutoScreenResumeResponseDto>.Fail("AI screening failed: invalid response format");

                var matchScore = screened.MatchScore;
                var recommendation = screened.Recommendation ?? "";

                request.MatchScore = matchScore;
                request.Recommendation = recommendation;
                request.SkillsMatch = JsonConvert.SerializeObject(screened.Strengths ?? new List<string>());
                request.RedFlags = JsonConvert.SerializeObject(screened.Weaknesses ?? new List<string>());
                request.ExperienceMatch ??= "";
                request.QualificationsMatch ??= "";

                // Save screening result via repository
                var (screeningID, autoShortlisted, isSuccess, message) = await _repo.AutoScreenResumeAsync(request, employeeCode);

                if (!isSuccess)
                    return ApiResponse<AutoScreenResumeResponseDto>.Fail(message);

                return ApiResponse<AutoScreenResumeResponseDto>.Success(
                    new AutoScreenResumeResponseDto
                    {
                        ScreeningID = screeningID,
                        MatchScore = matchScore,
                        Recommendation = recommendation,
                        Strengths = screened.Strengths ?? new List<string>(),
                        Weaknesses = screened.Weaknesses ?? new List<string>(),
                        AutoShortlistTriggered = autoShortlisted,
                        AutoShortlistScore = autoShortlisted ? matchScore : null
                    },
                    "Resume screened successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoScreenResumeAsync");
                return ApiResponse<AutoScreenResumeResponseDto>.Fail($"Error screening resume: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AutoShortlistResponseDto>> AutoShortlistCandidateAsync(AutoShortlistRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (isSuccess, message) = await _repo.AutoShortlistCandidateAsync(request, employeeCode);

                if (!isSuccess)
                    return ApiResponse<AutoShortlistResponseDto>.Fail(message);

                // Get updated application status
                var status = await _repo.GetApplicationStatusAsync(request.ApplicationID);

                return ApiResponse<AutoShortlistResponseDto>.Success(
                    new AutoShortlistResponseDto
                    {
                        ApplicationID = request.ApplicationID,
                        NewStatusID = status?.CurrentStatusID,
                        NewStatusCode = status?.StatusCode ?? "SHORTLISTED",
                        AutoShortlisted = true,
                        AutoShortlistDate = DateTime.UtcNow
                    },
                    "Candidate auto-shortlisted successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoShortlistCandidateAsync");
                return ApiResponse<AutoShortlistResponseDto>.Fail($"Error auto-shortlisting candidate: {ex.Message}");
            }
        }

        // Interview Rounds Methods
        public async Task<ApiResponse<GetInterviewRoundsResponseDto>> GetInterviewRoundsAsync(int companyID, int applicationID)
        {
            try
            {
                var rounds = await _repo.GetInterviewRoundsAsync(companyID, applicationID);
                var currentRound = rounds.Any() ? rounds.Max(r => r.RoundNumber) : 0;

                return ApiResponse<GetInterviewRoundsResponseDto>.Success(
                    new GetInterviewRoundsResponseDto
                    {
                        ApplicationID = applicationID,
                        CurrentRound = currentRound,
                        TotalRounds = rounds.Count,
                        Rounds = rounds
                    },
                    "Interview rounds retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewRoundsAsync");
                return ApiResponse<GetInterviewRoundsResponseDto>.Fail($"Error retrieving interview rounds: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ScheduleInterviewRoundResponseDto>> ScheduleInterviewRoundAsync(ScheduleInterviewRoundRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (scheduleID, scheduleCode, isSuccess, message) = await _repo.ScheduleInterviewRoundAsync(request, employeeCode);

                if (!isSuccess)
                    return ApiResponse<ScheduleInterviewRoundResponseDto>.Fail(message);

                // Get updated application status
                var status = await _repo.GetApplicationStatusAsync(request.ApplicationID);

                return ApiResponse<ScheduleInterviewRoundResponseDto>.Success(
                    new ScheduleInterviewRoundResponseDto
                    {
                        ScheduleID = scheduleID,
                        ScheduleCode = scheduleCode,
                        RoundNumber = request.RoundNumber,
                        ScheduledDate = request.ScheduledDate,
                        StatusID = null, // Will be retrieved from schedule
                        StatusCode = "SCHEDULED",
                        ApplicationUpdated = new ApplicationStatusUpdateDto
                        {
                            ApplicationID = request.ApplicationID,
                            CurrentStatusID = status?.CurrentStatusID,
                            StatusCode = status?.StatusCode ?? "INTERVIEW"
                        }
                    },
                    "Interview round scheduled successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ScheduleInterviewRoundAsync");
                return ApiResponse<ScheduleInterviewRoundResponseDto>.Fail($"Error scheduling interview round: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CompleteInterviewRoundResponseDto>> CompleteInterviewRoundAsync(CompleteInterviewRoundRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (isSuccess, message) = await _repo.CompleteInterviewRoundAsync(request, employeeCode);

                if (!isSuccess)
                    return ApiResponse<CompleteInterviewRoundResponseDto>.Fail(message);

                // Get updated application status
                var status = await _repo.GetApplicationStatusAsync(request.ApplicationID);

                // Get schedule details
                var schedule = await _repo.GetInterviewScheduleByIdAsync(request.ScheduleID);
                bool passed = request.Outcome.ToUpper() == "PASSED";

                return ApiResponse<CompleteInterviewRoundResponseDto>.Success(
                    new CompleteInterviewRoundResponseDto
                    {
                        ApplicationID = request.ApplicationID,
                        ScheduleID = request.ScheduleID,
                        Outcome = request.Outcome,
                        Passed = passed,
                        RoundStatus = "COMPLETED",
                        NextRound = request.NextRound,
                        TotalInterviewRounds = 0, // TODO: Get from schedule
                        CurrentInterviewRound = schedule?.InterviewRound ?? 0,
                        NewStatusID = status?.CurrentStatusID ?? 0,
                        NewStatusCode = status?.StatusCode ?? "COMPLETED",
                        NewStatusName = status?.StatusName ?? "Completed",
                        ApplicationUpdated = status,
                        CompletedDate = DateTime.Now,
                        CompletedBy = request.CompletedBy
                    },
                    "Interview round completed successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteInterviewRoundAsync");
                return ApiResponse<CompleteInterviewRoundResponseDto>.Fail($"Error completing interview round: {ex.Message}");
            }
        }

        public async Task<ApiResponse<GetApplicationsByInterviewStatusResponseDto>> GetApplicationsByInterviewStatusAsync(GetApplicationsByInterviewStatusRequestDto request)
        {
            try
            {
                var result = await _repo.GetApplicationsByInterviewStatusAsync(request);
                return ApiResponse<GetApplicationsByInterviewStatusResponseDto>.Success(result, "Applications retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationsByInterviewStatusAsync");
                return ApiResponse<GetApplicationsByInterviewStatusResponseDto>.Fail($"Error retrieving applications: {ex.Message}");
            }
        }

        private string GetCurrentUserEmployeeCode()
        {
            // TODO: Get from HttpContext or Claims
            return "SYSTEM";
        }


        // =============================================
        // CRUD OPERATIONS - APPLICANT
        // =============================================

        public async Task<ApiResponse<ApplicantResponseDto>> CreateApplicantAsync(ApplicantCreateRequestDto request, string createdBy)
        {
            try
            {
                var (applicantID, applicantCode, isSuccess, message) = await _repo.CreateApplicantAsync(request, createdBy);

                if (!isSuccess)
                    return ApiResponse<ApplicantResponseDto>.Fail(message);

                var result = await _repo.GetApplicantByIdAsync(applicantID);
                if (result == null)
                    return ApiResponse<ApplicantResponseDto>.Fail("Failed to retrieve created applicant");

                return ApiResponse<ApplicantResponseDto>.Success(result, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApplicantAsync");
                return ApiResponse<ApplicantResponseDto>.Fail($"Error creating applicant: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ApplicantResponseDto>> GetApplicantByIdAsync(int applicantID)
        {
            try
            {
                var result = await _repo.GetApplicantByIdAsync(applicantID);
                if (result == null)
                    return ApiResponse<ApplicantResponseDto>.Fail("Applicant not found");

                return ApiResponse<ApplicantResponseDto>.Success(result, "Applicant retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicantByIdAsync");
                return ApiResponse<ApplicantResponseDto>.Fail($"Error retrieving applicant: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ApplicantListResponseDto>> GetAllApplicantsAsync(ApplicantListRequestDto request)
        {
            try
            {
                var (applicants, totalCount) = await _repo.GetAllApplicantsAsync(request);

                var response = new ApplicantListResponseDto
                {
                    Applicants = applicants,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return ApiResponse<ApplicantListResponseDto>.Success(response, "Applicants retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllApplicantsAsync");
                return ApiResponse<ApplicantListResponseDto>.Fail($"Error retrieving applicants: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateApplicantAsync(ApplicantUpdateRequestDto request, string updatedBy)
        {
            try
            {
                var (isSuccess, message) = await _repo.UpdateApplicantAsync(request, updatedBy);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicantAsync");
                return ApiResponse<bool>.Fail($"Error updating applicant: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteApplicantAsync(int applicantID, string deletedBy)
        {
            try
            {
                
                var (isSuccess, message) = await _repo.DeleteApplicantAsync(applicantID, deletedBy);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteApplicantAsync");
                return ApiResponse<bool>.Fail($"Error deleting applicant: {ex.Message}");
            }
        }

        // =============================================
        // CRUD OPERATIONS - JOB REQUISITION
        // =============================================

        public async Task<ApiResponse<JobRequisitionResponseDto>> CreateJobRequisitionAsync(JobRequisitionCreateRequestDto request)
        {
            try
            {
              
                var (requisitionID, requisitionCode, isSuccess, message) = await _repo.CreateJobRequisitionAsync(request);

                if (!isSuccess)
                    return ApiResponse<JobRequisitionResponseDto>.Fail(message);

                if (request.HiringDetail != null)
                {
                    var (hireOk, hireMsg) = await _repo.UpsertHiringDetailAsync(
                        requisitionID,
                        request.CompanyID,
                        request.HiringDetail,
                        request.CreatedBy ?? "system");

                    if (!hireOk)
                        return ApiResponse<JobRequisitionResponseDto>.Fail(hireMsg);
                }

                var result = await _repo.GetJobRequisitionByIdAsync(requisitionID);
                if (result == null)
                    return ApiResponse<JobRequisitionResponseDto>.Fail("Failed to retrieve created job requisition");

                return ApiResponse<JobRequisitionResponseDto>.Success(result, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateJobRequisitionAsync");
                return ApiResponse<JobRequisitionResponseDto>.Fail($"Error creating job requisition: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobRequisitionResponseDto>> GetJobRequisitionByIdAsync(int requisitionID)
        {
            try
            {
                var result = await _repo.GetJobRequisitionByIdAsync(requisitionID);
                if (result == null)
                    return ApiResponse<JobRequisitionResponseDto>.Fail("Job requisition not found");

                return ApiResponse<JobRequisitionResponseDto>.Success(result, "Job requisition retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobRequisitionByIdAsync");
                return ApiResponse<JobRequisitionResponseDto>.Fail($"Error retrieving job requisition: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobRequisitionListResponseDto>> GetAllJobRequisitionsAsync(JobRequisitionListRequestDto request)
        {
            try
            {
                var (requisitions, totalCount) = await _repo.GetAllJobRequisitionsAsync(request);

                var response = new JobRequisitionListResponseDto
                {
                    Requisitions = requisitions,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return ApiResponse<JobRequisitionListResponseDto>.Success(response, "Job requisitions retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJobRequisitionsAsync");
                return ApiResponse<JobRequisitionListResponseDto>.Fail($"Error retrieving job requisitions: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateJobRequisitionAsync(JobRequisitionUpdateRequestDto request, string updatedBy)
        {
            try
            {
                var (isSuccess, message) = await _repo.UpdateJobRequisitionAsync(request, updatedBy);
                if (!isSuccess)
                    return ApiResponse<bool>.Fail(message);

                if (request.HiringDetail != null)
                {
                    var (hireOk, hireMsg) = await _repo.UpsertHiringDetailAsync(
                        request.RequisitionID,
                        request.CompanyID,
                        request.HiringDetail,
                        updatedBy);

                    if (!hireOk)
                        return ApiResponse<bool>.Fail(hireMsg);
                }

                return ApiResponse<bool>.Success(true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateJobRequisitionAsync");
                return ApiResponse<bool>.Fail($"Error updating job requisition: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteJobRequisitionAsync(int requisitionID, string deletedBy,int companyID)
        {
            try
            {
                
                
                var (isSuccess, message) = await _repo.DeleteJobRequisitionAsync(requisitionID, deletedBy, companyID);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteJobRequisitionAsync");
                return ApiResponse<bool>.Fail($"Error deleting job requisition: {ex.Message}");
            }
        }

        // =============================================
        // CRUD OPERATIONS - JOB APPLICATION
        // =============================================

        public async Task<ApiResponse<JobApplicationResponseDto>> CreateJobApplicationAsync(JobApplicationCreateRequestDto request, string createdBy)
        {
            try
            {
                var (applicationID, applicationCode, isSuccess, message) = await _repo.CreateJobApplicationAsync(request, createdBy);

                if (!isSuccess)
                    return ApiResponse<JobApplicationResponseDto>.Fail(message);

                var result = await _repo.GetJobApplicationByIdAsync(applicationID);
                if (result == null)
                    return ApiResponse<JobApplicationResponseDto>.Fail("Failed to retrieve created job application");

              

                return ApiResponse<JobApplicationResponseDto>.Success(result, message);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateJobApplicationAsync");
                return ApiResponse<JobApplicationResponseDto>.Fail($"Error creating job application: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobApplicationResponseDto>> GetJobApplicationByIdAsync(int applicationID)
        {
            try
            {
                var result = await _repo.GetJobApplicationByIdAsync(applicationID);
                if (result == null)
                    return ApiResponse<JobApplicationResponseDto>.Fail("Job application not found");

                var resumeData = result;
                var resumePath = !string.IsNullOrEmpty(resumeData?.ResumePath)
                    ? _fileStorageService.GetFullUrl(resumeData.ResumePath)
                    : null;
                resumeData.ResumePath = resumePath;

                return ApiResponse<JobApplicationResponseDto>.Success(result, "Job application retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobApplicationByIdAsync");
                return ApiResponse<JobApplicationResponseDto>.Fail($"Error retrieving job application: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobApplicationListResponseDto>> GetAllJobApplicationsAsync(JobApplicationListRequestDto request)
        {
            try
            {
                var (applications, totalCount) = await _repo.GetAllJobApplicationsAsync(request);

                var response = new JobApplicationListResponseDto
                {
                    Applications = applications,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return ApiResponse<JobApplicationListResponseDto>.Success(response, "Job applications retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJobApplicationsAsync");
                return ApiResponse<JobApplicationListResponseDto>.Fail($"Error retrieving job applications: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateJobApplicationAsync(JobApplicationUpdateRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (isSuccess, message) = await _repo.UpdateJobApplicationAsync(request, employeeCode);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateJobApplicationAsync");
                return ApiResponse<bool>.Fail($"Error updating job application: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteJobApplicationAsync(int applicationID)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (isSuccess, message) = await _repo.DeleteJobApplicationAsync(applicationID, employeeCode);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteJobApplicationAsync");
                return ApiResponse<bool>.Fail($"Error deleting job application: {ex.Message}");
            }
        }

        // =============================================
        // CRUD OPERATIONS - INTERVIEW SCHEDULE
        // =============================================


        private async Task<CompanyResponseDto?> GetCompanyViaApi(int companyId)
        {
            try
            {
                //var baseUrl = _configuration["Services:CompanyServiceUrl"];
                //var url = $"{baseUrl}/admin/api/company?companyId={companyId}&isActive=true";

                var baseUrl = _configuration["AppSettings:BaseUrl"];
                var url = $"{baseUrl}/admin/api/company?companyId={companyId}&isActive=true";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();

                var apiResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse<object>>(content);

                // 🔥 IMPORTANT FIX: JToken handling
                if (apiResponse?.Data is Newtonsoft.Json.Linq.JArray dataArray)
                {
                    var companies = dataArray.ToObject<List<CompanyResponseDto>>();
                    return companies?.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching company via API");
                return null;
            }
        }

        public async Task<ApiResponse<InterviewScheduleResponseDto>> CreateInterviewScheduleAsync(
            InterviewScheduleCreateRequestDto request,
            string createdBy,
            int companyID)
        {
            try
            {
                var (scheduleID, scheduleCode, isSuccess, message) = await _repo.CreateInterviewScheduleAsync(request, createdBy);

                if (!isSuccess)
                    return ApiResponse<InterviewScheduleResponseDto>.Fail(message);

                // Assign Panel Members
                if (request.PanelMembers != null && request.PanelMembers.Any())
                {
                    var assignRequest = new AssignPanelMembersRequestDto
                    {
                        ApplicationID = request.ApplicationID,
                        ScheduleID = scheduleID,
                        CompanyID = companyID,
                        PanelMembers = request.PanelMembers
                    };

                    var (panelCount, panelSuccess, panelMessage) = await _repo.AssignPanelMembersAsync(assignRequest, createdBy);

                    if (!panelSuccess)
                        return ApiResponse<InterviewScheduleResponseDto>.Fail(panelMessage);
                }

                var result = await _repo.GetInterviewScheduleByIdAsync(scheduleID);
                if (result == null)
                    return ApiResponse<InterviewScheduleResponseDto>.Fail("Failed to retrieve created interview schedule");

                // ✅ 🔥 COMPANY FETCH (FIX APPLIED HERE)
                //CompanyResponseDto? company = null;

                var company = await GetCompanyViaApi(companyID);
                //var companyResponse = await _companyService.GetAllCompaniesAsync(companyID, true);

                //if (companyResponse.IsSuccess && companyResponse.Data != null)
                //{
                //    var companies = companyResponse.Data as IEnumerable<CompanyResponseDto>;
                //    company = companies?.FirstOrDefault();
                //}

                // ✅ Send Email with Company
                if (result.PanelMembers != null && result.PanelMembers.Any())
                {
                    await SendInterviewScheduleEmailsAsync(result, companyID, company);
                }

                return ApiResponse<InterviewScheduleResponseDto>.Success(result, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateInterviewScheduleAsync");
                return ApiResponse<InterviewScheduleResponseDto>.Fail($"Error creating interview schedule: {ex.Message}");
            }
        }


        private async Task SendInterviewScheduleEmailsAsync(InterviewScheduleResponseDto schedule, int companyId, CompanyResponseDto? company)
        {
            var roundText = GetInterviewRoundText(schedule.InterviewRound);

            // 📌 Applicant Email Body
            //var applicantBody = $@"
            //    <div style='font-family: ""Segoe UI"", Arial, sans-serif; line-height: 1.6; max-width: 600px; margin: 0 auto; color: #333;'>

            //        <!-- Header -->
            //        <div style='background-color: #f8f9fa; padding: 20px; text-align: center; border-bottom: 1px solid #e9ecef;'>
            //            <img src='https://yourcompanylogo.com/logo.png' alt='Company Logo' style='max-height: 50px;'>
            //        </div>

            //        <!-- Content -->
            //        <div style='padding: 30px;'>

            //            <h2 style='color: #2c3e50; margin-top: 0;'>Interview Scheduled</h2>

            //            <p>Dear {schedule.FirstName} {schedule.LastName},</p>

            //            <p>Your interview has been scheduled. Please find the details below:</p>

            //            <div style='background-color: #f1f8fe; border-left: 4px solid #2e86c1; padding: 15px; margin: 20px 0;'>

            //                <p style='margin: 5px 0;'><b>Position:</b> {schedule.JobTitle}</p>
            //                <p style='margin: 5px 0;'><b>Interview Round:</b> {roundText}</p>
            //                <p style='margin: 5px 0;'><b>Date:</b> {schedule.ScheduledDate:dd MMM yyyy}</p>
            //                <p style='margin: 5px 0;'><b>Time:</b> {schedule.ScheduledDate:hh:mm tt}</p>
            //                <p style='margin: 5px 0;'><b>Duration:</b> {schedule.DurationMinutes} Minutes</p>
            //                <p style='margin: 5px 0;'><b>Meeting Link:</b> {schedule.OnlineMeetingLink}</p>

            //            </div>

            //            <p>Please join the meeting on time. Contact us if you have any questions.</p>

            //            <p>Best regards,<br/>HR Team</p>

            //        </div>

            //        <!-- Footer -->
            //        <div style='background-color: #f8f9fa; padding: 15px; text-align: center;
            //                    font-size: 12px; color: #7f8c8d; border-top: 1px solid #e9ecef;'>
            //            <p>© {DateTime.Now.Year} DigiSoft. All rights reserved.</p>
            //        </div>

            //    </div>";

            // 📌 Send email to Applicant
            //await _emailService.SendEmailAsync(companyId, schedule.Email, $"Interview Scheduled - {schedule.JobTitle}", applicantBody, true);


            // 📌 Panel Members Email Body
            var logoUrl = !string.IsNullOrEmpty(company?.CompanyLogo)
             ? _fileStorageService.GetFullUrl(company.CompanyLogo)
             : "https://via.placeholder.com/150x50?text=Company+Logo";


            var panelBody = $@"
                <div style='font-family: ""Segoe UI"", Arial, sans-serif; line-height: 1.6; max-width: 600px; margin: 0 auto; color: #333;'>

                    <div style='background:#f8f9fa; padding:20px; text-align:center; border-bottom:1px solid #e9ecef;'>
                            <img src='{logoUrl}' alt='Logo' style='max-height:50px;'>
                     </div>


                    <div style='padding: 30px;'>

                        <h2 style='color: #2c3e50; margin-top: 0;'>Interview Panel Notification</h2>

                        <p>Dear Panel Member,</p>

                        <p>You have been assigned to interview the following candidate:</p>

                        <div style='background-color: #f1f8fe; border-left: 4px solid #2e86c1; padding: 15px; margin: 20px 0;'>

                            <p style='margin: 5px 0;'><b>Applicant:</b> {schedule.FirstName} {schedule.LastName}</p>
                            <p style='margin: 5px 0;'><b>Position:</b> {schedule.JobTitle}</p>
                            <p style='margin: 5px 0;'><b>Interview Round:</b> {roundText}</p>
                            <p style='margin: 5px 0;'><b>Date:</b> {schedule.ScheduledDate:dd MMM yyyy}</p>
                            <p style='margin: 5px 0;'><b>Time:</b> {schedule.ScheduledDate:hh:mm tt}</p>
                            <p style='margin: 5px 0;'><b>Duration:</b> {schedule.DurationMinutes} Minutes</p>
                            <p style='margin: 5px 0;'><b>Meeting Link:</b> {schedule.OnlineMeetingLink}</p>

                        </div>

                        <p>Please be prepared and join the meeting on time.</p>

                       <p>Regards,<br/>{company?.CompanyName} HR Team</p>

                    </div>

                     <div style='background:#f8f9fa; padding:15px; text-align:center; font-size:12px;'>

                        <p>{company?.Address}</p>
                        <p>{company?.CompanyMobile}</p>
                        <p>{company?.Website}</p>

                        <p>© {DateTime.Now.Year} {company?.CompanyName}</p>

                    </div>

                </div>";

            // 📌 Send email to each Panel Member
            foreach (var panel in schedule.PanelMembers)
            {
                await _emailService.SendEmailAsync(companyId, panel.InterviewerEmail, $"Interview Scheduled - {schedule.JobTitle}", panelBody, true);
            }
        }


        public async Task<ApiResponse<InterviewScheduleResponseDto>> GetInterviewScheduleByIdAsync(int scheduleID)
        {
            try
            {
                var result = await _repo.GetInterviewScheduleByIdAsync(scheduleID);
                if (result == null)
                    return ApiResponse<InterviewScheduleResponseDto>.Fail("Interview schedule not found");

                return ApiResponse<InterviewScheduleResponseDto>.Success(result, "Interview schedule retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewScheduleByIdAsync");
                return ApiResponse<InterviewScheduleResponseDto>.Fail($"Error retrieving interview schedule: {ex.Message}");
            }
        }

        public async Task<ApiResponse<InterviewScheduleListResponseDto>> GetAllInterviewSchedulesAsync(InterviewScheduleListRequestDto request)
        {
            try
            {
                var (schedules, totalCount) = await _repo.GetAllInterviewSchedulesAsync(request);

                var response = new InterviewScheduleListResponseDto
                {
                    Schedules = schedules,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return ApiResponse<InterviewScheduleListResponseDto>.Success(response, "Interview schedules retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllInterviewSchedulesAsync");
                return ApiResponse<InterviewScheduleListResponseDto>.Fail($"Error retrieving interview schedules: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateInterviewScheduleAsync(InterviewScheduleUpdateRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (isSuccess, message) = await _repo.UpdateInterviewScheduleAsync(request, employeeCode);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateInterviewScheduleAsync");
                return ApiResponse<bool>.Fail($"Error updating interview schedule: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteInterviewScheduleAsync(int scheduleID)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (isSuccess, message) = await _repo.DeleteInterviewScheduleAsync(scheduleID, employeeCode);

                return isSuccess
                    ? ApiResponse<bool>.Success(true, message)
                    : ApiResponse<bool>.Fail(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteInterviewScheduleAsync");
                return ApiResponse<bool>.Fail($"Error deleting interview schedule: {ex.Message}");
            }
        }

        // =============================================
        // STATUS MANAGEMENT
        // =============================================

        public async Task<ApiResponse<List<StatusResponseDto>>> GetAllStatusesAsync(string? statusTypeCode = null, bool isActive = true)
        {
            try
            {
                var result = await _repo.GetAllStatusesAsync(statusTypeCode, isActive);
                return ApiResponse<List<StatusResponseDto>>.Success(result, "Statuses retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllStatusesAsync");
                return ApiResponse<List<StatusResponseDto>>.Fail($"Error retrieving statuses: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<StatusTypeResponseDto>>> GetAllStatusTypesAsync(bool isActive = true)
        {
            try
            {
                var result = await _repo.GetAllStatusTypesAsync(isActive);
                return ApiResponse<List<StatusTypeResponseDto>>.Success(result, "Status types retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllStatusTypesAsync");
                return ApiResponse<List<StatusTypeResponseDto>>.Fail($"Error retrieving status types: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<StatusResponseDto>>> GetStatusesByTypeAsync(string statusTypeCode, int companyID)
        {
            try
            {
                var result = await _repo.GetStatusesByTypeAsync(statusTypeCode, companyID);
                return ApiResponse<List<StatusResponseDto>>.Success(result, "Statuses retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetStatusesByTypeAsync");
                return ApiResponse<List<StatusResponseDto>>.Fail($"Error retrieving statuses: {ex.Message}");
            }
        }

        // =============================================
        // MANUAL PROCESSING
        // =============================================

        public async Task<ApiResponse<ManualProcessResponseDto>> ManualProcessApplicationAsync(ManualProcessRequestDto request)
        {
            try
            {
                var result = await _repo.ManualProcessApplicationAsync(request);
                return ApiResponse<ManualProcessResponseDto>.Success(result, "Application processed manually successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualProcessApplicationAsync");
                return ApiResponse<ManualProcessResponseDto>.Fail($"Error processing application: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ManualParseResumeResponseDto>> ManualParseResumeAsync(ManualParseResumeRequestDto request)
        {
            try
            {
                var result = await _repo.ManualParseResumeAsync(request);
                return ApiResponse<ManualParseResumeResponseDto>.Success(result, "Resume parsed manually successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualParseResumeAsync");
                return ApiResponse<ManualParseResumeResponseDto>.Fail($"Error parsing resume: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ManualScreenResumeResponseDto>> ManualScreenResumeAsync(ManualScreenResumeRequestDto request)
        {
            try
            {
                var result = await _repo.ManualScreenResumeAsync(request);
                return ApiResponse<ManualScreenResumeResponseDto>.Success(result, "Resume screened manually successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualScreenResumeAsync");
                return ApiResponse<ManualScreenResumeResponseDto>.Fail($"Error screening resume: {ex.Message}");
            }
        }


        // =============================================
        // WORKFLOW ACTIONS
        // =============================================

        public async Task<ApiResponse<ManualShortlistResponseDto>> ManualShortlistAsync(ManualShortlistRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                var (newStatusID, newStatusCode, isSuccess, message) = await _repo.ManualShortlistAsync(request, employeeCode);

                return ApiResponse<ManualShortlistResponseDto>.Success(
                    new ManualShortlistResponseDto
                    {
                        IsSuccess = isSuccess,
                        Message = message,
                        NewStatusID = newStatusID,
                        NewStatusCode = newStatusCode
                    },
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualShortlistAsync");
                return ApiResponse<ManualShortlistResponseDto>.Fail($"Error manually shortlisting candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ShortlistCandidateResponseDto>> ShortlistCandidateAsync(int applicationID, ShortlistCandidateRequestDto request)
        {
            try
            {
                var result = await _repo.ShortlistCandidateAsync(applicationID, request);
                return ApiResponse<ShortlistCandidateResponseDto>.Success(result, "Candidate shortlisted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShortlistCandidateAsync");
                return ApiResponse<ShortlistCandidateResponseDto>.Fail($"Error shortlisting candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RejectApplicationResponseDto>> RejectApplicationAsync(int applicationID, RejectApplicationRequestDto request)
        {
            try
            {
                var result = await _repo.RejectApplicationAsync(applicationID, request);
                return ApiResponse<RejectApplicationResponseDto>.Success(result, "Application rejected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectApplicationAsync");
                return ApiResponse<RejectApplicationResponseDto>.Fail($"Error rejecting application: {ex.Message}");
            }
        }

        public async Task<ApiResponse<HireCandidateResponseDto>> HireCandidateAsync(int applicationID, HireCandidateRequestDto request)
        {
            try
            {
                // 1️⃣ Hire candidate (existing)
                var result = await _repo.HireCandidateAsync(applicationID, request);

                // 2️⃣ Get application data
                var app = await _repo.GetJobApplicationByIdAsync(applicationID);

                if (app == null)
                    return ApiResponse<HireCandidateResponseDto>.Fail("Application not found");

                byte[] pdfBytes = null;
                string pdfFileName = null;
                string filePath = null;

                // 3️⃣ Generate PDF (🔥 SAME AS PURCHASE FLOW)
                if (request.OfferLetterBit)
                {
                    var (bytes, fileName) = GenerateOfferLetterPDF(app, request);
                    pdfBytes = bytes;
                    pdfFileName = fileName;

                    if (pdfBytes != null && pdfBytes.Length > 0)
                    {
                        var safeCompany = request.CompanyID.ToString();
                        filePath = await _fileService.SaveFilePurchaseRequestAsync(
                            pdfBytes,
                            pdfFileName,
                            safeCompany,
                            "recruitment",
                            "offer-letters",
                            "pdf"
                        );
                    }
                }

                // 4️⃣ Send Email (🔥 WITH ATTACHMENT)
                if (request.OfferLetterEmailSendBit)
                {
                    await SendOfferLetterEmailAsync(app, request, pdfBytes, pdfFileName);
                }

                // 5️⃣ Response
                result.OfferLetterPath = filePath;

                return ApiResponse<HireCandidateResponseDto>.Success(result, "Candidate hired successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HireCandidateAsync");
                return ApiResponse<HireCandidateResponseDto>.Fail($"Error hiring candidate: {ex.Message}");
            }
        }

        private (byte[] PdfBytes, string FileName) GenerateOfferLetterPDF(dynamic app, HireCandidateRequestDto request)
        {
            try
            {
                var fileName = $"OfferLetter_{app.ApplicationCode}.pdf";

                var html = $@"
        <html>
        <head>
            <style>
                body {{
                    font-family: Arial;
                    font-size: 12px;
                    color: #333;
                    padding: 20px;
                }}

                .header {{
                    border-bottom: 2px solid #c80050;
                    padding-bottom: 10px;
                    margin-bottom: 20px;
                }}

                .title {{
                    text-align: center;
                    font-size: 16px;
                    font-weight: bold;
                    margin: 20px 0;
                }}

                .section {{
                    margin-top: 15px;
                }}

                .footer {{
                    position: fixed;
                    bottom: 10px;
                    width: 100%;
                    text-align: center;
                    font-size: 10px;
                    color: #777;
                }}

                ul {{
                    margin-left: 20px;
                }}
            </style>
        </head>

        <body>

            <!-- HEADER -->
            <div class='header'>
                <h3>Digisoft Transformation Solutions</h3>
                <div>Ref: {app.ApplicationCode}</div>
                <div>Date: {DateTime.Now:dd MMM yyyy}</div>
            </div>

            <!-- TITLE -->
            <div class='title'>OFFER FOR EMPLOYMENT</div>

            <!-- BODY -->
            <div class='section'>
                Dear {app.FullName},
            </div>

            <div class='section'>
                With reference to your application and subsequent interviews, 
                we are pleased to offer you the position of 
                <b>{app.RequisitionJobTitle}</b> 
                {(request.DepartmentID != null ? "in department" : "")}.
            </div>

            <div class='section'>
                <ul>
                    <li>Salary: PKR {request.Amount}</li>
                    <li>Joining Date: {request.JoiningDate:dd MMM yyyy}</li>
                    <li>Probation Period: 3 Months</li>
                </ul>
            </div>

            <div class='section'>
                The company will run a verification process and confirmation will be subject to background clearance.
            </div>

            <div class='section'>
                Sincerely,<br/>
                Human Resource Department<br/>
                <b>Digisoft Transformation Solutions</b>
            </div>

            <!-- FOOTER -->
            <div class='footer'>
                Digisoft Transformation Solutions | Office #201-202, Karachi
            </div>

        </body>
        </html>";



                using var ms = new MemoryStream();

                // 🔥 SAME LIBRARY AS YOUR PURCHASE REQUEST
                HtmlConverter.ConvertToPdf(html, ms);

                return (ms.ToArray(), fileName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Offer Letter PDF");
                return (Array.Empty<byte>(), string.Empty);
            }
        }

        private async Task SendOfferLetterEmailAsync(dynamic app, HireCandidateRequestDto request, byte[] pdfBytes, string pdfFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(app.Email))
                    return;

                var subject = $"Job Offer - {app.RequisitionJobTitle}";

                var body = $@"
        <html>
        <body>
            <h3>Job Offer</h3>

            <p>Dear {app.FullName},</p>

            <p>We are pleased to offer you the position of <b>{app.RequisitionJobTitle}</b>.</p>

            <p><b>Joining Date:</b> {request.JoiningDate:dd MMM yyyy}</p>
            <p><b>Salary:</b> PKR {request.Amount}</p>

            <p>Please find the attached offer letter.</p>

            <br/>
            <p>Regards,<br/>HR Department</p>
        </body>
        </html>";

                await _emailService.SendEmaiwithAttachmentlAsync(
                    request.CompanyID,
                    app.Email,
                    subject,
                    body,
                    isHtml: true,
                    attachments: new List<EmailAttachment>
                    {
                new EmailAttachment
                {
                    FileName = pdfFileName,
                    FileBytes = pdfBytes,
                    ContentType = "application/pdf"
                }
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending Offer Letter email");
            }
        }

        public async Task<ApiResponse<PublishRequisitionResponseDto>> PublishRequisitionAsync(int requisitionID, PublishRequisitionRequestDto request)
        {
            try
            {
                var result = await _repo.PublishRequisitionAsync(requisitionID, request);
                return ApiResponse<PublishRequisitionResponseDto>.Success(result, "Requisition published successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PublishRequisitionAsync");
                return ApiResponse<PublishRequisitionResponseDto>.Fail($"Error publishing requisition: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<JobRequisitionResponseDto>>> GetPublicRequisitionsAsync(int companyID, string? searchText, int? departmentID, string? location)
        {
            try
            {
                var result = await _repo.GetPublicRequisitionsAsync(companyID, searchText, departmentID, location);
                return ApiResponse<List<JobRequisitionResponseDto>>.Success(result, "Public requisitions retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPublicRequisitionsAsync");
                return ApiResponse<List<JobRequisitionResponseDto>>.Fail($"Error retrieving public requisitions: {ex.Message}");
            }
        }

        public async Task<ApiResponse<UpdateApplicationStatusResponseDto>> UpdateApplicationStatusAsync(int applicationID, UpdateApplicationStatusRequestDto request)
        {
            try
            {
                var result = await _repo.UpdateApplicationStatusAsync(applicationID, request);
                return ApiResponse<UpdateApplicationStatusResponseDto>.Success(result, "Application status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicationStatusAsync");
                return ApiResponse<UpdateApplicationStatusResponseDto>.Fail($"Error updating application status: {ex.Message}");
            }
        }

        public async Task<ApiResponse<CancelInterviewScheduleResponseDto>> CancelInterviewScheduleAsync(int scheduleID, CancelInterviewScheduleRequestDto request)
        {
            try
            {
                var result = await _repo.CancelInterviewScheduleAsync(scheduleID, request);
                return ApiResponse<CancelInterviewScheduleResponseDto>.Success(result, "Interview schedule cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelInterviewScheduleAsync");
                return ApiResponse<CancelInterviewScheduleResponseDto>.Fail($"Error cancelling interview schedule: {ex.Message}");
            }
        }

        // =============================================
        // EVALUATION
        // =============================================

        public async Task<ApiResponse<List<EvaluationCriteriaDto>>> GetEvaluationCriteriaAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetEvaluationCriteriaAsync(companyID);
                return ApiResponse<List<EvaluationCriteriaDto>>.Success(result, "Evaluation criteria retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationCriteriaAsync");
                return ApiResponse<List<EvaluationCriteriaDto>>.Fail($"Error retrieving evaluation criteria: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<RatingScaleDto>>> GetRatingScalesAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetRatingScalesAsync(companyID);
                return ApiResponse<List<RatingScaleDto>>.Success(result, "Rating scales retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRatingScalesAsync");
                return ApiResponse<List<RatingScaleDto>>.Fail($"Error retrieving rating scales: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SubmitEvaluationResponseDto>> SubmitEvaluationAsync(SubmitEvaluationRequestDto request)
        {
            try
            {
                var result = await _repo.SubmitEvaluationAsync(request);
                return ApiResponse<SubmitEvaluationResponseDto>.Success(result, "Evaluation submitted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitEvaluationAsync");
                return ApiResponse<SubmitEvaluationResponseDto>.Fail($"Error submitting evaluation: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<EvaluationDto>>> GetEvaluationsByScheduleAsync(int scheduleID)
        {
            try
            {
                var result = await _repo.GetEvaluationsByScheduleAsync(scheduleID);
                return ApiResponse<List<EvaluationDto>>.Success(result, "Evaluations retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationsByScheduleAsync");
                return ApiResponse<List<EvaluationDto>>.Fail($"Error retrieving evaluations: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<EvaluationDto>>> GetEvaluationsByApplicationAsync(int applicationID)
        {
            try
            {
                var result = await _repo.GetEvaluationsByApplicationAsync(applicationID);
                return ApiResponse<List<EvaluationDto>>.Success(result, "Evaluations retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationsByApplicationAsync");
                return ApiResponse<List<EvaluationDto>>.Fail($"Error retrieving evaluations: {ex.Message}");
            }
        }

        // =============================================
        // MASTER DATA
        // =============================================

        public async Task<ApiResponse<List<ApplicationSourceDto>>> GetApplicationSourcesAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetApplicationSourcesAsync(companyID);
                return ApiResponse<List<ApplicationSourceDto>>.Success(result, "Application sources retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationSourcesAsync");
                return ApiResponse<List<ApplicationSourceDto>>.Fail($"Error retrieving application sources: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<InterviewTypeDto>>> GetInterviewTypesAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetInterviewTypesAsync(companyID);
                return ApiResponse<List<InterviewTypeDto>>.Success(result, "Interview types retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewTypesAsync");
                return ApiResponse<List<InterviewTypeDto>>.Fail($"Error retrieving interview types: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<VenueDto>>> GetVenuesAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetVenuesAsync(companyID);
                return ApiResponse<List<VenueDto>>.Success(result, "Venues retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVenuesAsync");
                return ApiResponse<List<VenueDto>>.Fail($"Error retrieving venues: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<NotificationMethodDto>>> GetNotificationMethodsAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetNotificationMethodsAsync(companyID);
                return ApiResponse<List<NotificationMethodDto>>.Success(result, "Notification methods retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotificationMethodsAsync");
                return ApiResponse<List<NotificationMethodDto>>.Fail($"Error retrieving notification methods: {ex.Message}");
            }
        }

        // =============================================
        // DASHBOARD
        // =============================================

        public async Task<ApiResponse<DashboardResponseDto>> GetDashboardStatisticsAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetDashboardStatisticsAsync(companyID);
                return ApiResponse<DashboardResponseDto>.Success(result, "Dashboard statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardStatisticsAsync");
                return ApiResponse<DashboardResponseDto>.Fail($"Error retrieving dashboard statistics: {ex.Message}");
            }
        }

        public async Task<DbOperationResult<bool>> SubmitJobRequisitionForApprovalAsync(int requisitionId,int companyId, int employeeId,string actor)
        {
            // 1️⃣ Load Requisition
            var requisition = await _repo.GetJobRequisitionByIdAsync(requisitionId);

            if (requisition == null)
                return DbOperationResult<bool>.Fail("Job Requisition not found");

            // 2️⃣ Only Draft Allowed
            if (!string.Equals(requisition.StatusName, "Draft", StringComparison.OrdinalIgnoreCase))
                return DbOperationResult<bool>.Fail("Only Draft requisitions can be submitted");

            // 3️⃣ Check Approval Flow
            var hasFlow = await _workflowService.IsApprovalFlowConfiguredAsync("Job Requisitions", companyId, employeeId);

            if (!hasFlow)
                return DbOperationResult<bool>.Fail("Approval flow is not configured for Job Requisition.");

          
            var statusesResult = await _repo.GetStatusesByTypeAsync("REQUISITION", companyId);

            if (statusesResult == null || !statusesResult.Any())
                return DbOperationResult<bool>.Fail("Requisition statuses not found");
           
            // 4️⃣ Get PENDING_APPROVAL StatusID
            var pendingStatus = statusesResult.FirstOrDefault(x => x.StatusCode == "PENDING_APPROVAL" && x.IsActive);

            if (pendingStatus == null)
                return DbOperationResult<bool>.Fail("Pending Approval status not configured");

            var updateRequest = new JobRequisitionUpdateRequestDto
            {
                RequisitionID = requisition.RequisitionID,
                CompanyID = companyId,
                JobTitle = requisition.JobTitle,
                JobSummary = requisition.JobSummary,
                DepartmentID = requisition.DepartmentID,
                DesignationID = requisition.DesignationID,
                EmploymentTypeID = requisition.EmploymentTypeID,
                GradeID = requisition.GradeID,
                Vacancies = requisition.Vacancies,
                MinExperience = requisition.MinExperience,
                MaxExperience = requisition.MaxExperience,
                MinAge = requisition.MinAge,
                MaxAge = requisition.MaxAge,
                MinSalary = requisition.MinSalary,
                MaxSalary = requisition.MaxSalary,
                Location = requisition.Location,
                ReportingTo = requisition.ReportingTo,
                KeyResponsibilities = requisition.KeyResponsibilities,
                Requirements = requisition.Requirements,
                Qualifications = requisition.Qualifications,
                Skills = requisition.Skills,
                Benefits = requisition.Benefits,
                IsPublished = requisition.IsPublished,
                PublishedDate = requisition.PublishedDate,
                ClosingDate = requisition.ClosingDate,
                JobCategoryID = requisition.JobCategoryID,
                Isbudget = requisition.Isbudget,
                IsNonBudget = requisition.IsNonBudget,
                SalaryRecommendationID = requisition.SalaryRecommendationID,
                IsActive = requisition.IsActive,

                //  Only change this
                StatusID = pendingStatus.StatusID
            };

            // 6️⃣ Call existing Update method
            var (isSuccess, message) = await _repo.UpdateJobRequisitionAsync(updateRequest, actor);

            if (!isSuccess)
                return DbOperationResult<bool>.Fail(message);

            // 7️⃣ Start Workflow
            await _workflowService.StartApprovalWorkflowAsync("Job Requisitions", requisitionId, employeeId,companyId,actor);

            // 8️⃣ Send Emails
            await SendRequisitionApprovalEmailsAsync(requisition);

            return DbOperationResult<bool>.Success(true);
        }

        private async Task SendRequisitionApprovalEmailsAsync(JobRequisitionResponseDto requisition)
        {
            var approvers = await _workflowService.GetApproverEmailsByWorkflowAsync("Job Requisitions", requisition.RequisitionID, requisition.CompanyID);

            if (approvers == null || !approvers.Any())
                return;

            var subject = "Job Requisition Approval Required";

            var body = $@"
                <h3>Job Requisition Approval</h3>
                <p>A job requisition is awaiting your approval.</p>
                <p><b>Job Title:</b> {requisition.JobTitle}</p>
                <p><b>Vacancies:</b> {requisition.Vacancies}</p>
                <p><b>Closing Date:</b> {requisition.ClosingDate:dd MMM yyyy}</p>";

            foreach (var a in approvers)
            {
                var email = EncryptionHelper.DecryptText(a.Email);
                await _emailService.SendEmailAsync(requisition.CompanyID,email,subject,body,true);
            }
        }

        // =============================================
        // PANEL MEMBER EVALUATION
        // =============================================

        public async Task<ApiResponse<PanelMemberScheduleListResponseDto>> GetPanelMemberSchedulesAsync(int interviewerID, int companyID, int? statusID, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var result = await _repo.GetPanelMemberSchedulesAsync(interviewerID, companyID, statusID, startDate, endDate);
                return ApiResponse<PanelMemberScheduleListResponseDto>.Success(result, "Panel member schedules retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPanelMemberSchedulesAsync");
                return ApiResponse<PanelMemberScheduleListResponseDto>.Fail($"Error retrieving panel member schedules: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PanelEvaluationResponseDto>> GetPanelEvaluationAsync(int scheduleID, int interviewerID)
        {
            try
            {
                var result = await _repo.GetPanelEvaluationAsync(scheduleID, interviewerID);
                if (result == null)
                    return ApiResponse<PanelEvaluationResponseDto>.Fail("Evaluation not found");

                return ApiResponse<PanelEvaluationResponseDto>.Success(result, "Evaluation retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPanelEvaluationAsync");
                return ApiResponse<PanelEvaluationResponseDto>.Fail($"Error retrieving evaluation: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PanelEvaluationResponseDto>> SavePanelEvaluationAsync(PanelEvaluationRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                request.CreatedBy ??= employeeCode;

                var (evaluationID, isSuccess, message) = await _repo.SavePanelEvaluationAsync(request);

                if (!isSuccess)
                    return ApiResponse<PanelEvaluationResponseDto>.Fail(message);

                var result = await _repo.GetPanelEvaluationAsync(request.ScheduleID, request.InterviewerID);
                if (result == null)
                    return ApiResponse<PanelEvaluationResponseDto>.Fail("Failed to retrieve saved evaluation");

                return ApiResponse<PanelEvaluationResponseDto>.Success(result, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavePanelEvaluationAsync");
                return ApiResponse<PanelEvaluationResponseDto>.Fail($"Error saving evaluation: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ConfirmPanelAttendanceResponseDto>> ConfirmPanelAttendanceAsync(int panelID, ConfirmPanelAttendanceRequestDto request)
        {
            try
            {
                var employeeCode = GetCurrentUserEmployeeCode();
                request.ConfirmedBy ??= employeeCode;

                var (isSuccess, message) = await _repo.ConfirmPanelAttendanceAsync(panelID, request.ConfirmedBy);

                if (!isSuccess)
                    return ApiResponse<ConfirmPanelAttendanceResponseDto>.Fail(message);

                return ApiResponse<ConfirmPanelAttendanceResponseDto>.Success(
                    new ConfirmPanelAttendanceResponseDto
                    {
                        PanelID = panelID,
                        IsConfirmed = true,
                        ConfirmedOn = DateTime.UtcNow
                    },
                    message
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmPanelAttendanceAsync");
                return ApiResponse<ConfirmPanelAttendanceResponseDto>.Fail($"Error confirming attendance: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<RecommendationDto>>> GetRecommendationsAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetRecommendationsAsync(companyID);
                return ApiResponse<List<RecommendationDto>>.Success(result, "Recommendations retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendationsAsync");
                return ApiResponse<List<RecommendationDto>>.Fail($"Error retrieving recommendations: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<EvaluationCriteriaWithRatingsDto>>> GetEvaluationCriteriaWithRatingsAsync(int companyID)
        {
            try
            {
                var result = await _repo.GetEvaluationCriteriaWithRatingsAsync(companyID);
                return ApiResponse<List<EvaluationCriteriaWithRatingsDto>>.Success(result, "Evaluation criteria with ratings retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationCriteriaWithRatingsAsync");
                return ApiResponse<List<EvaluationCriteriaWithRatingsDto>>.Fail($"Error retrieving evaluation criteria: {ex.Message}");
            }
        }

        public async Task<ApiResponse<PanelMemberScheduleListResponseDto>> GetConfirmedHeadSchedulesAsync(int companyID, int? statusID, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var result = await _repo.GetConfirmedHeadSchedulesAsync(companyID, statusID, startDate, endDate);
                return ApiResponse<PanelMemberScheduleListResponseDto>
                    .Success(result, "Confirmed head schedules retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetConfirmedHeadSchedulesAsync");
                return ApiResponse<PanelMemberScheduleListResponseDto>
                    .Fail($"Error retrieving schedules: {ex.Message}");
            }
        }


        public async Task<ApiResponse<ApplicationAIStatusDto>> GetApplicationAIStatusAsync(int applicationID, int companyID)
        {
            try
            {
                var result = await _repo.GetApplicationAIStatusAsync(applicationID, companyID);

                return ApiResponse<ApplicationAIStatusDto>
                    .Success(result, "AI status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationAIStatusAsync");
                return ApiResponse<ApplicationAIStatusDto>
                    .Fail($"Error retrieving AI status: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RecDashboardRecStatsResponseDto>> GetDashboardRecStatsAsync()
        {
            try
            {
                var list = await _repo.GetDashboardRecStatsAsync();
                var data = new RecDashboardRecStatsResponseDto { Stats = list };
                return ApiResponse<RecDashboardRecStatsResponseDto>.Success(data, "Recruitment dashboard stats retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardRecStatsAsync");
                return ApiResponse<RecDashboardRecStatsResponseDto>.Fail($"Error retrieving recruitment dashboard stats: {ex.Message}");
            }
        }

        // =============================================
        // JOB BANK
        // =============================================

        public async Task<ApiResponse<JobBankCandidateInsertResponseDto>> JobBankCandidateInsertAsync(JobBankCandidateInsertRequestDto request, string? createdBy)
        {
            try
            {
                if (request.CreatedBy == null && createdBy != null)
                    request.CreatedBy = createdBy;

                var (jobBankCandidateID, isSuccess, message) = await _repo.JobBankCandidateInsertAsync(request);
                if (!isSuccess)
                    return ApiResponse<JobBankCandidateInsertResponseDto>.Fail(message);

                return ApiResponse<JobBankCandidateInsertResponseDto>.Success(
                    new JobBankCandidateInsertResponseDto { JobBankCandidateID = jobBankCandidateID },
                    message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateInsertAsync");
                return ApiResponse<JobBankCandidateInsertResponseDto>.Fail($"Error registering candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> JobBankCandidateUpdateAsync(JobBankCandidateUpdateRequestDto request)
        {
            try
            {
                var (isSuccess, message) = await _repo.JobBankCandidateUpdateAsync(request);
                if (!isSuccess)
                    return ApiResponse<bool>.Fail(message);
                return ApiResponse<bool>.Success(true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateUpdateAsync");
                return ApiResponse<bool>.Fail($"Error updating candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobBankCandidateResponseDto>> JobBankCandidateGetByIdAsync(int id, int companyID)
        {
            try
            {
                var result = await _repo.JobBankCandidateGetByIdAsync(id, companyID);
                if (result == null)
                    return ApiResponse<JobBankCandidateResponseDto>.Fail("Candidate not found");
                return ApiResponse<JobBankCandidateResponseDto>.Success(result, "Candidate retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateGetByIdAsync");
                return ApiResponse<JobBankCandidateResponseDto>.Fail($"Error retrieving candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobBankCandidateSearchResponseDto>> JobBankCandidateSearchAsync(JobBankCandidateSearchRequestDto request)
        {
            try
            {
                var (candidates, totalRecords) = await _repo.JobBankCandidateSearchAsync(request);

                foreach (var candidate in candidates)
                {
                    if (!string.IsNullOrEmpty(candidate.ResumeFilePath))
                    {
                        candidate.ResumeFilePath = _fileStorageService.GetFullUrl(candidate.ResumeFilePath);
                    }
                }

                var data = new JobBankCandidateSearchResponseDto
                {
                    Candidates = candidates,
                    TotalRecords = totalRecords
                };
                return ApiResponse<JobBankCandidateSearchResponseDto>.Success(data, "Search completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateSearchAsync");
                return ApiResponse<JobBankCandidateSearchResponseDto>.Fail($"Error searching candidates: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobBankCandidateSearchResponseDto>> JobBankCandidateGetListAsync(JobBankCandidateListRequestDto request)
        {
            try
            {
                var (candidates, totalRecords) = await _repo.JobBankCandidateGetListAsync(request);
                var data = new JobBankCandidateSearchResponseDto
                {
                    Candidates = candidates,
                    TotalRecords = totalRecords
                };
                return ApiResponse<JobBankCandidateSearchResponseDto>.Success(data, "List retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateGetListAsync");
                return ApiResponse<JobBankCandidateSearchResponseDto>.Fail($"Error retrieving list: {ex.Message}");
            }
        }

        public async Task<ApiResponse<JobBankShortlistInsertResponseDto>> JobBankShortlistInsertAsync(JobBankShortlistInsertRequestDto request)
        {
            try
            {
                var (jobBankShortlistID, isSuccess, message) = await _repo.JobBankShortlistInsertAsync(request);
                if (!isSuccess)
                    return ApiResponse<JobBankShortlistInsertResponseDto>.Fail(message);
                return ApiResponse<JobBankShortlistInsertResponseDto>.Success(
                    new JobBankShortlistInsertResponseDto { JobBankShortlistID = jobBankShortlistID },
                    message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlistInsertAsync");
                return ApiResponse<JobBankShortlistInsertResponseDto>.Fail($"Error shortlisting candidate: {ex.Message}");
            }
        }

        public async Task<ApiResponse<object>> ConvertJobBankCandidateAsync(ConvertRequestDto request)
        {
            var result = await _repo.ConvertJobBankCandidateAsync(request);

            if (!result.IsSuccess)
                return ApiResponse<object>.Fail(result.Message);

            return ApiResponse<object>.Success(null, result.Message);
        }

        public async Task<ApiResponse<List<JobBankShortlistByRequisitionDto>>> JobBankShortlistGetByRequisitionAsync(int requisitionID, int companyID)
        {
            try
            {
                var result = await _repo.JobBankShortlistGetByRequisitionAsync(requisitionID, companyID);
                return ApiResponse<List<JobBankShortlistByRequisitionDto>>.Success(result, "Shortlisted candidates retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlistGetByRequisitionAsync");
                return ApiResponse<List<JobBankShortlistByRequisitionDto>>.Fail($"Error retrieving shortlist: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> JobBankShortlistRemoveAsync(int jobBankShortlistID, int companyID)
        {
            try
            {
                var (isSuccess, message) = await _repo.JobBankShortlistRemoveAsync(jobBankShortlistID, companyID);
                if (!isSuccess)
                    return ApiResponse<bool>.Fail(message);
                return ApiResponse<bool>.Success(true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlistRemoveAsync");
                return ApiResponse<bool>.Fail($"Error removing shortlist: {ex.Message}");
            }
        }

        public async Task<DbOperationResult<bool>> CloseJobRequisitionAsync(int requisitionId,int companyId,string actor)
        {
            // 1️⃣ Load Requisition
            var requisition = await _repo.GetJobRequisitionByIdAsync(requisitionId);

            if (requisition == null)
                return DbOperationResult<bool>.Fail("Job Requisition not found");

            // 2️⃣ Only Published Allowed
            if (!string.Equals(requisition.StatusName, "Published", StringComparison.OrdinalIgnoreCase))
                return DbOperationResult<bool>.Fail("Only Published requisitions can be closed");

            // 3️⃣ Get REQUISITION statuses
            var statusesResult = await _repo.GetStatusesByTypeAsync("REQUISITION", companyId);

            if (statusesResult == null || !statusesResult.Any())
                return DbOperationResult<bool>.Fail("Requisition statuses not found");

            // 4️⃣ Get CLOSED Status
            var closedStatus = statusesResult
                .FirstOrDefault(x => x.StatusCode == "CLOSED" && x.IsActive);

            if (closedStatus == null)
                return DbOperationResult<bool>.Fail("Closed status not configured");

            // 5️⃣ Prepare update DTO (same pattern as Submit)
            var updateRequest = new JobRequisitionUpdateRequestDto
            {
                RequisitionID = requisition.RequisitionID,
                CompanyID = companyId,

                JobTitle = requisition.JobTitle,
                JobSummary = requisition.JobSummary,
                DepartmentID = requisition.DepartmentID,
                DesignationID = requisition.DesignationID,
                EmploymentTypeID = requisition.EmploymentTypeID,
                GradeID = requisition.GradeID,
                Vacancies = requisition.Vacancies,
                MinExperience = requisition.MinExperience,
                MaxExperience = requisition.MaxExperience,
                MinAge = requisition.MinAge,
                MaxAge = requisition.MaxAge,
                MinSalary = requisition.MinSalary,
                MaxSalary = requisition.MaxSalary,
                Location = requisition.Location,
                ReportingTo = requisition.ReportingTo,
                KeyResponsibilities = requisition.KeyResponsibilities,
                Requirements = requisition.Requirements,
                Qualifications = requisition.Qualifications,
                Skills = requisition.Skills,
                Benefits = requisition.Benefits,
                IsPublished = false, // optional
                PublishedDate = requisition.PublishedDate,
                ClosingDate = requisition.ClosingDate,
                JobCategoryID = requisition.JobCategoryID,
                Isbudget = requisition.Isbudget,
                IsNonBudget = requisition.IsNonBudget,
                SalaryRecommendationID = requisition.SalaryRecommendationID,
                IsActive = false, // optional if you want to deactivate

                // 🔥 Only change
                StatusID = closedStatus.StatusID
            };

            var (isSuccess, message) =
                await _repo.UpdateJobRequisitionAsync(updateRequest, actor);

            if (!isSuccess)
                return DbOperationResult<bool>.Fail(message);

            return DbOperationResult<bool>.Success(true);
        }

        public async Task<DbOperationResult<bool>> SendInterviewNotificationAsync(
    int scheduleId,
    int companyId,
    string actor,
    string? formUrl)
        {
            var schedule = await _repo.GetInterviewScheduleByIdAsync(scheduleId);

            if (schedule == null)
                return DbOperationResult<bool>.Fail("Interview schedule not found");

            if (schedule.IsNotified)
                return DbOperationResult<bool>.Fail("Notification already sent");

            if (string.IsNullOrEmpty(schedule.Email))
                return DbOperationResult<bool>.Fail("Candidate email not found");

            //CompanyResponseDto? company = null;

            //var companyResponse = await _companyService.GetAllCompaniesAsync(companyId, true);

            //if (companyResponse.IsSuccess && companyResponse.Data != null)
            //{
            //    var companies = companyResponse.Data as IEnumerable<CompanyResponseDto>;
            //    company = companies?.FirstOrDefault();
            //}

            var company = await GetCompanyViaApi(companyId);

            await SendInterviewEmailAsync(schedule, companyId, formUrl, company);

            var updated = await _repo.MarkInterviewAsNotifiedAsync(scheduleId, actor);

            return DbOperationResult<bool>.Success(true);
        }


        private async Task SendInterviewEmailAsync(InterviewScheduleResponseDto schedule,int companyId,string? formUrl, CompanyResponseDto? company)
        {
            var subject = $"Interview Scheduled - {schedule.JobTitle}";
            var interviewDate = schedule.ScheduledDate.ToString("dd MMM yyyy");
            var interviewTime = schedule.ScheduledDate.ToString("hh:mm tt");
            var roundText = GetInterviewRoundText(schedule.InterviewRound);

            string formSection = "";

            //< br />< br />
            //< p >< a href = '{formUrl}' > Direct link: { formUrl}</ a ></ p >
            if (!string.IsNullOrWhiteSpace(formUrl))
            {
                formSection = $@"
                    <div style='margin-top:20px;'>
                        <p>Please fill the <strong>Google Form</strong> before interview:</p>

                        <a href='{formUrl}' target='_blank'
                           style='display:inline-block;
                                  padding:10px 15px;
                                  background:#2e86c1;
                                  color:#fff;
                                  text-decoration:none;
                                  border-radius:5px;'>
                            Fill Google Form
                        </a>

                    </div>";
            }

            var logoUrl = !string.IsNullOrEmpty(company?.CompanyLogo)
             ? _fileStorageService.GetFullUrl(company.CompanyLogo)
             : "https://via.placeholder.com/150x50?text=Company+Logo";

            var body = $@"
                <div style='font-family: ""Segoe UI"", Arial, sans-serif; line-height: 1.6; max-width: 600px; margin: 0 auto; color: #333;'>

                      <h2 style='color: #2c3e50; margin-top: 0;'>{company?.CompanyName}</h2>

                    <!-- Header -->
                    <div style='background:#f8f9fa; padding:20px; text-align:center; border-bottom:1px solid #e9ecef;'>
                            <img src='{logoUrl}' alt='Logo' style='max-height:50px;'>
                     </div>

                    <!-- Content -->
                    <div style='padding: 30px;'>

                        <h2 style='color: #2c3e50; margin-top: 0;'>Interview Invitation</h2>

                        <p>Dear {schedule.FirstName} {schedule.LastName},</p>

                        <p>We are pleased to inform you that your interview has been scheduled. Please find the details below:</p>

                        <div style='background-color: #f1f8fe; border-left: 4px solid #2e86c1; padding: 15px; margin: 20px 0;'>

                            <p style='margin: 5px 0;'><b>Position:</b> {schedule.JobTitle}</p>
                            <p style='margin: 5px 0;'><b>Interview Round:</b> {roundText}</p>
                            <p style='margin: 5px 0;'><b>Date:</b> {interviewDate}</p>
                            <p style='margin: 5px 0;'><b>Time:</b> {interviewTime}</p>
                            <p style='margin: 5px 0;'><b>Duration:</b> {schedule.DurationMinutes} Minutes</p>
                            <p style='margin: 5px 0;'><b>Meeting Link:</b> {schedule.OnlineMeetingLink}</p>

                        </div>

                       {formSection}

                        <p>Please join the meeting on time. If you have any questions, feel free to contact us.</p>

                        <p>Best of luck!</p>

                    </div>

                     <!-- Footer -->
                        <div style='background:#f8f9fa; padding:15px; text-align:center; font-size:12px; color:#7f8c8d; border-top:1px solid #e9ecef;'>

                            <p>{company?.Address}</p>
                            <p>{company?.CompanyMobile}</p>
                            <p>{company?.Website}</p>

                            <p>© {DateTime.Now.Year} {company?.CompanyName}</p>

                        </div>

                </div>";

            await _emailService.SendEmailAsync(companyId, schedule.Email, subject, body, true);
        }

        private string GetInterviewRoundText(int round)
        {
            return round switch
            {
                1 => "First Interview",
                2 => "Second Interview",
                3 => "Third Interview",
                4 => "Fourth Interview",
                5 => "Fifth Interview",
                _ => $"{round}th Interview"
            };
        }

        public async Task<ApiResponse<EvaluationResponseDtos>> GetEvaluationByScheduleAsync(int scheduleId)
        {
            try
            {
                var evaluation = await _repo.GetEvaluationByScheduleAsync(scheduleId);

                if (evaluation == null)
                    return ApiResponse<EvaluationResponseDtos>.Fail("Evaluation not found");

                return ApiResponse<EvaluationResponseDtos>.Success(evaluation, "Evaluation retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationByScheduleAsync");
                return ApiResponse<EvaluationResponseDtos>
                    .Fail($"Error retrieving evaluation: {ex.Message}");
            }
        }

        public async Task<bool> HireCandidateStatus(int applicationID, HireCandidateDto dto)
        {
            return await _repo.HireCandidateStatusAsync(applicationID, dto);
        }
    }
}

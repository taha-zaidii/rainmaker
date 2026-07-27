using Digi.Recruitment.Module.Domain.Services;
using Digi.Recruitment.Module.Domain.Services.IServices;
using Digi.Shared.Attributes;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Digi.Shared.Services;
using Digi.Shared.SharedLibrary.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data;
using System.IO;
using System.Security.Cryptography;

namespace Digi.Recruitment.Module.Controllers
{
    [ApiController]
    [Route("recruitment/api/[controller]")]
    public class RecruitmentController : BaseController
    {
        private readonly IRecruitmentService _service;
        private readonly ICentralizedEmailService _centralizedEmailService;
        private readonly ILogger<RecruitmentController> _logger;
        private readonly IFileService _fileService;
        private readonly IFileStorageService _fileStorageService;
        public RecruitmentController(IRecruitmentService service, IFileStorageService fileStorageService, IFileService fileService, ILogger<RecruitmentController> logger, ICentralizedEmailService centralizedEmailService)
        {
            _service = service;
            _fileService = fileService;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _centralizedEmailService = centralizedEmailService;
        }

        [HttpPost("Applications/AutoProcess")]
        [AuditLog("RECRUITMENT", "AutoProcess", "RecruitmentApplication")]
        public async Task<IActionResult> AutoProcessApplication([FromBody] AutoProcessRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.AutoProcessApplicationAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoProcessApplication");
                return StatusCode(500, ApiResponse<AutoProcessResponseDto>.Fail($"Error processing application: {ex.Message}"));
            }
        }

        [HttpPost("Applications/{applicationID}/AutoShortlist")]
        [AuditLog("RECRUITMENT", "AutoShortlist", "RecruitmentApplication")]
        public async Task<IActionResult> AutoShortlistCandidate(int applicationID, [FromBody] AutoShortlistRequestDto request)
        {
            try
            {
                request.ApplicationID = applicationID;
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.AutoShortlistCandidateAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoShortlistCandidate");
                return StatusCode(500, ApiResponse<AutoShortlistResponseDto>.Fail($"Error auto-shortlisting candidate: {ex.Message}"));
            }
        }

        // Interview Rounds APIs
        [HttpGet("Applications/{applicationID}/InterviewRounds")]
        public async Task<IActionResult> GetInterviewRounds(int applicationID, [FromQuery] int companyID)
        {
            try
            {
                var result = await _service.GetInterviewRoundsAsync(companyID, applicationID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewRounds");
                return StatusCode(500, ApiResponse<GetInterviewRoundsResponseDto>.Fail($"Error retrieving interview rounds: {ex.Message}"));
            }
        }

        [HttpPost("Applications/{applicationID}/InterviewRounds/Schedule")]
        [AuditLog("RECRUITMENT", "Schedule", "InterviewRound")]
        public async Task<IActionResult> ScheduleInterviewRound(int applicationID, [FromBody] ScheduleInterviewRoundRequestDto request)
        {
            try
            {
                request.ApplicationID = applicationID;
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.ScheduleInterviewRoundAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ScheduleInterviewRound");
                return StatusCode(500, ApiResponse<ScheduleInterviewRoundResponseDto>.Fail($"Error scheduling interview round: {ex.Message}"));
            }
        }

        [HttpPost("Applications/{applicationID}/InterviewRounds/{scheduleID}/Complete")]
        [AuditLog("RECRUITMENT", "Complete", "InterviewRound")]
        public async Task<IActionResult> CompleteInterviewRound(int applicationID, int scheduleID, [FromBody] CompleteInterviewRoundRequestDto request)
        {
            try
            {
                request.ApplicationID = applicationID;
                request.ScheduleID = scheduleID;
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.CompleteInterviewRoundAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteInterviewRound");
                return StatusCode(500, ApiResponse<CompleteInterviewRoundResponseDto>.Fail($"Error completing interview round: {ex.Message}"));
            }
        }

        [HttpGet("Applications/ByInterviewStatus")]
        public async Task<IActionResult> GetApplicationsByInterviewStatus([FromQuery] GetApplicationsByInterviewStatusRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.GetApplicationsByInterviewStatusAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationsByInterviewStatus");
                return StatusCode(500, ApiResponse<GetApplicationsByInterviewStatusResponseDto>.Fail($"Error retrieving applications: {ex.Message}"));
            }
        }

        // =============================================
        // CRUD OPERATIONS - APPLICANT
        // =============================================

        [HttpPost("Applicants")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateApplicant([FromBody] ApplicantCreateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var employeeCode = GetCurrentEmployeeCode();
                
                var result = await _service.CreateApplicantAsync(request, employeeCode);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApplicant");
                return StatusCode(500, ApiResponse<ApplicantResponseDto>.Fail($"Error creating applicant: {ex.Message}"));
            }
        }

        [HttpGet("Applicants/{applicantID}")]
        public async Task<IActionResult> GetApplicantById(int applicantID)
        {
            try
            {
                var result = await _service.GetApplicantByIdAsync(applicantID);
                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicantById");
                return StatusCode(500, ApiResponse<ApplicantResponseDto>.Fail($"Error retrieving applicant: {ex.Message}"));
            }
        }

        [HttpGet("Applicants")]
        public async Task<IActionResult> GetAllApplicants([FromQuery] ApplicantListRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.GetAllApplicantsAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllApplicants");
                return StatusCode(500, ApiResponse<ApplicantListResponseDto>.Fail($"Error retrieving applicants: {ex.Message}"));
            }
        }

        [HttpPut("Applicants")]
        public async Task<IActionResult> UpdateApplicant([FromBody] ApplicantUpdateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var employeeCode = GetCurrentEmployeeCode();
                var result = await _service.UpdateApplicantAsync(request,employeeCode);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicant");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error updating applicant: {ex.Message}"));
            }
        }

        [HttpDelete("Applicants/{applicantID}")]
        public async Task<IActionResult> DeleteApplicant(int applicantID)
        {
            try
            {
                var employeeCode = GetCurrentEmployeeCode();
                var result = await _service.DeleteApplicantAsync(applicantID,employeeCode);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteApplicant");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error deleting applicant: {ex.Message}"));
            }
        }

        // =============================================
        // CRUD OPERATIONS - JOB REQUISITION
        // =============================================

        [HttpPost("Requisitions")]
        public async Task<IActionResult> CreateJobRequisition([FromBody] JobRequisitionCreateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var employeeCode = GetCurrentEmployeeCode();
                request.CreatedBy = employeeCode;
                
                var result = await _service.CreateJobRequisitionAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateJobRequisition");
                return StatusCode(500, ApiResponse<JobRequisitionResponseDto>.Fail($"Error creating job requisition: {ex.Message}"));
            }
        }

        [HttpGet("Requisitions/Public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicRequisitions([FromQuery] int companyID, [FromQuery] string? searchText, [FromQuery] int? departmentID, [FromQuery] string? location)
        {
            try
            {
                var result = await _service.GetPublicRequisitionsAsync(companyID, searchText, departmentID, location);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPublicRequisitions");
                return StatusCode(500, ApiResponse<List<JobRequisitionResponseDto>>.Fail($"Error retrieving public requisitions: {ex.Message}"));
            }
        }

        [HttpGet("Requisitions/{requisitionID}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetJobRequisitionById(int requisitionID)
        {
            try
            {
                var result = await _service.GetJobRequisitionByIdAsync(requisitionID);
                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobRequisitionById");
                return StatusCode(500, ApiResponse<JobRequisitionResponseDto>.Fail($"Error retrieving job requisition: {ex.Message}"));
            }
        }

        //[HttpGet("Requisitions")]
        //[AllowAnonymous]
        //public async Task<IActionResult> GetAllJobRequisitions([FromQuery] JobRequisitionListRequestDto request)
        //{
        //    try
        //    {
        //        if (!ModelState.IsValid)
        //            return BadRequest(ModelState);

        //        var result = await _service.GetAllJobRequisitionsAsync(request);

        //        if (result != null && result.Data != null)
        //        {
        //            var data = (JobRequisitionListResponseDto)result.Data;

        //            if (data.Requisitions != null && data.Requisitions.Any())
        //            {
        //                foreach (var req in data.Requisitions)
        //                {
        //                    if (!string.IsNullOrEmpty(req.FilePath))
        //                    {
        //                        req.FilePath =_fileStorageService.GetFullUrl(req.FilePath);
        //                    }
        //                }
        //            }
        //        }

        //        return result.IsSuccess ? Ok(result) : BadRequest(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in GetAllJobRequisitions");
        //        return StatusCode(
        //            500,
        //            ApiResponse<JobRequisitionListResponseDto>.Fail(
        //                $"Error retrieving job requisitions: {ex.Message}"
        //            )
        //        );
        //    }
        //}
        [HttpGet("Requisitions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllJobRequisitions([FromQuery] JobRequisitionListRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // ✅ Check if current user has admin permission for recruitment
                bool hasAdminPerm = User.HasClaim("Permission", "ADMIN_RECRUITMENT");

                // Agar admin permission nahi hai, to sirf apna (created) data dikhao
                if (!hasAdminPerm)
                {
                    request.CreatedBy = GetCurrentEmployeeCode();
                }
                // Admin ho to request.CreatedBy null hi rahega → sab data milega

                var result = await _service.GetAllJobRequisitionsAsync(request);

                if (result != null && result.Data != null)
                {
                    var data = (JobRequisitionListResponseDto)result.Data;
                    if (data.Requisitions != null && data.Requisitions.Any())
                    {
                        foreach (var req in data.Requisitions)
                        {
                            if (!string.IsNullOrEmpty(req.FilePath))
                            {
                                req.FilePath = _fileStorageService.GetFullUrl(req.FilePath);
                            }
                        }
                    }
                }

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJobRequisitions");
                return StatusCode(500, ApiResponse<JobRequisitionListResponseDto>.Fail($"Error retrieving job requisitions: {ex.Message}"));
            }
        }



        [HttpPut("Requisitions")]
        public async Task<IActionResult> UpdateJobRequisition([FromBody] JobRequisitionUpdateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var employeeCode = GetCurrentEmployeeCode();
               
                var result = await _service.UpdateJobRequisitionAsync(request, employeeCode);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateJobRequisition");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error updating job requisition: {ex.Message}"));
            }
        }

        [HttpDelete("Requisitions/{requisitionID}")]
        public async Task<IActionResult> DeleteJobRequisition(int requisitionID)
        {
            try
            {
                var employeeCode = GetCurrentEmployeeCode();
                var companyID = (int)GetCurrentCompanyId();
                var result = await _service.DeleteJobRequisitionAsync(requisitionID, employeeCode,companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteJobRequisition");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error deleting job requisition: {ex.Message}"));
            }
        }

        // =============================================
        // CRUD OPERATIONS - JOB APPLICATION
        // =============================================

        [HttpPost("Applications")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateJobApplication([FromBody] JobApplicationCreateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var employeeCode = GetCurrentEmployeeCode();
                var result = await _service.CreateJobApplicationAsync(request, employeeCode);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateJobApplication");
                return StatusCode(500, ApiResponse<JobApplicationResponseDto>.Fail($"Error creating job application: {ex.Message}"));
            }
        }

        [HttpGet("Applications/List")]
        public async Task<IActionResult> GetAllJobApplications([FromQuery] JobApplicationListRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.GetAllJobApplicationsAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJobApplications");
                return StatusCode(500, ApiResponse<JobApplicationListResponseDto>.Fail($"Error retrieving job applications: {ex.Message}"));
            }
        }

        [HttpGet("Applications/{applicationID}")]
        public async Task<IActionResult> GetJobApplicationById(int applicationID)
        {
            try
            {
                var result = await _service.GetJobApplicationByIdAsync(applicationID);
                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobApplicationById");
                return StatusCode(500, ApiResponse<JobApplicationResponseDto>.Fail($"Error retrieving job application: {ex.Message}"));
            }
        }

        [HttpPut("Applications")]
        public async Task<IActionResult> UpdateJobApplication([FromBody] JobApplicationUpdateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.UpdateJobApplicationAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateJobApplication");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error updating job application: {ex.Message}"));
            }
        }

        [HttpDelete("Applications/{applicationID}")]
        public async Task<IActionResult> DeleteJobApplication(int applicationID)
        {
            try
            {
                var result = await _service.DeleteJobApplicationAsync(applicationID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteJobApplication");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error deleting job application: {ex.Message}"));
            }
        }

        // =============================================
        // CRUD OPERATIONS - INTERVIEW SCHEDULE
        // =============================================

        [HttpPost("InterviewSchedules")]
        public async Task<IActionResult> CreateInterviewSchedule([FromBody] InterviewScheduleCreateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var employeeCode = GetCurrentEmployeeCode();
                var companyID = GetCurrentCompanyId();
                var result = await _service.CreateInterviewScheduleAsync(request,employeeCode,(int)companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateInterviewSchedule");
                return StatusCode(500, ApiResponse<InterviewScheduleResponseDto>.Fail($"Error creating interview schedule: {ex.Message}"));
            }
        }

        [HttpGet("InterviewSchedules/{scheduleID}")]
        public async Task<IActionResult> GetInterviewScheduleById(int scheduleID)
        {
            try
            {
                var result = await _service.GetInterviewScheduleByIdAsync(scheduleID);
                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewScheduleById");
                return StatusCode(500, ApiResponse<InterviewScheduleResponseDto>.Fail($"Error retrieving interview schedule: {ex.Message}"));
            }
        }

        [HttpGet("InterviewSchedules")]
        public async Task<IActionResult> GetAllInterviewSchedules([FromQuery] InterviewScheduleListRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.GetAllInterviewSchedulesAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllInterviewSchedules");
                return StatusCode(500, ApiResponse<InterviewScheduleListResponseDto>.Fail($"Error retrieving interview schedules: {ex.Message}"));
            }
        }

        [HttpPut("InterviewSchedules")]
        public async Task<IActionResult> UpdateInterviewSchedule([FromBody] InterviewScheduleUpdateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.UpdateInterviewScheduleAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateInterviewSchedule");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error updating interview schedule: {ex.Message}"));
            }
        }

        [HttpDelete("InterviewSchedules/{scheduleID}")]
        public async Task<IActionResult> DeleteInterviewSchedule(int scheduleID)
        {
            try
            {
                var result = await _service.DeleteInterviewScheduleAsync(scheduleID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteInterviewSchedule");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error deleting interview schedule: {ex.Message}"));
            }
        }

        // =============================================
        // STATUS MANAGEMENT
        // =============================================

        [HttpGet("Statuses")]
        public async Task<IActionResult> GetAllStatuses([FromQuery] string? statusTypeCode = null, [FromQuery] bool isActive = true)
        {
            try
            {
                var result = await _service.GetAllStatusesAsync(statusTypeCode, isActive);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllStatuses");
                return StatusCode(500, ApiResponse<List<StatusResponseDto>>.Fail($"Error retrieving statuses: {ex.Message}"));
            }
        }

        [HttpGet("StatusTypes")]
        public async Task<IActionResult> GetAllStatusTypes([FromQuery] bool isActive = true)
        {
            try
            {
                var result = await _service.GetAllStatusTypesAsync(isActive);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllStatusTypes");
                return StatusCode(500, ApiResponse<List<StatusTypeResponseDto>>.Fail($"Error retrieving status types: {ex.Message}"));
            }
        }

        // =============================================
        // MANUAL PROCESSING APIs
        // =============================================

        [HttpPost("Applications/ManualProcess")]
        [AuditLog("RECRUITMENT", "ManualProcess", "RecruitmentApplication")]
        public async Task<IActionResult> ManualProcessApplication([FromBody] ManualProcessRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.ManualProcessApplicationAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualProcessApplication");
                return StatusCode(500, ApiResponse<ManualProcessResponseDto>.Fail($"Error processing application manually: {ex.Message}"));
            }
        }

        [HttpPost("Resume/ManualParse")]
        [AuditLog("RECRUITMENT", "ManualParse", "Resume")]
        public async Task<IActionResult> ManualParseResume([FromBody] ManualParseResumeRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.ManualParseResumeAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualParseResume");
                return StatusCode(500, ApiResponse<ManualParseResumeResponseDto>.Fail($"Error parsing resume manually: {ex.Message}"));
            }
        }

        [HttpPost("Resume/ManualScreen")]
        [AuditLog("RECRUITMENT", "ManualScreen", "Resume")]
        public async Task<IActionResult> ManualScreenResume([FromBody] ManualScreenResumeRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.ManualScreenResumeAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualScreenResume");
                return StatusCode(500, ApiResponse<ManualScreenResumeResponseDto>.Fail($"Error screening resume manually: {ex.Message}"));
            }
        }

        [HttpPost("Applications/{applicationID}/ManualShortlist")]
        [AuditLog("RECRUITMENT", "ManualShortlist", "RecruitmentApplication")]
        public async Task<IActionResult> ManualShortlistCandidate(int applicationID, [FromBody] ManualShortlistRequestDto request)
        {
            try
            {
                request.ApplicationID = applicationID;
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.ManualShortlistAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualShortlistCandidate");
                return StatusCode(500, ApiResponse<ManualShortlistResponseDto>.Fail($"Error manually shortlisting candidate: {ex.Message}"));
            }
        }

        // =============================================
        // WORKFLOW ACTION APIs
        // =============================================

        [HttpPost("Applications/{applicationID}/Shortlist")]
        [AuditLog("RECRUITMENT", "Shortlist", "RecruitmentApplication")]
        public async Task<IActionResult> ShortlistCandidate(int applicationID, [FromBody] ShortlistCandidateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.ShortlistCandidateAsync(applicationID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShortlistCandidate");
                return StatusCode(500, ApiResponse<ShortlistCandidateResponseDto>.Fail($"Error shortlisting candidate: {ex.Message}"));
            }
        }

        [HttpPost("Applications/{applicationID}/Reject")]
        [AuditLog("RECRUITMENT", "Reject", "RecruitmentApplication")]
        public async Task<IActionResult> RejectApplication(int applicationID, [FromBody] RejectApplicationRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.RejectApplicationAsync(applicationID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectApplication");
                return StatusCode(500, ApiResponse<RejectApplicationResponseDto>.Fail($"Error rejecting application: {ex.Message}"));
            }
        }

        [HttpPost("Applications/{applicationID}/Hire")]
        [AuditLog("RECRUITMENT", "Hire", "RecruitmentApplication")]
        public async Task<IActionResult> HireCandidate(int applicationID, [FromBody] HireCandidateRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.HireCandidateAsync(applicationID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HireCandidate");
                return StatusCode(500, ApiResponse<HireCandidateResponseDto>.Fail($"Error hiring candidate: {ex.Message}"));
            }
        }

        [HttpPost("Requisitions/{requisitionID}/Publish")]
        [AuditLog("RECRUITMENT", "Publish", "JobRequisition")]
        public async Task<IActionResult> PublishRequisition(int requisitionID, [FromBody] PublishRequisitionRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var CompanyID = GetCurrentCompanyId();
                request.CompanyID = (int)CompanyID; 
                var result = await _service.PublishRequisitionAsync(requisitionID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PublishRequisition");
                return StatusCode(500, ApiResponse<PublishRequisitionResponseDto>.Fail($"Error publishing requisition: {ex.Message}"));
            }
        }

        [HttpPut("Applications/{applicationID}/Status")]
        [AuditLog("RECRUITMENT", "UpdateStatus", "RecruitmentApplication")]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationID, [FromBody] UpdateApplicationStatusRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.UpdateApplicationStatusAsync(applicationID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicationStatus");
                return StatusCode(500, ApiResponse<UpdateApplicationStatusResponseDto>.Fail($"Error updating application status: {ex.Message}"));
            }
        }

        [HttpPost("InterviewSchedules/{scheduleID}/Cancel")]
        [AuditLog("RECRUITMENT", "Cancel", "InterviewSchedule")]
        public async Task<IActionResult> CancelInterviewSchedule(int scheduleID, [FromBody] CancelInterviewScheduleRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.CancelInterviewScheduleAsync(scheduleID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelInterviewSchedule");
                return StatusCode(500, ApiResponse<CancelInterviewScheduleResponseDto>.Fail($"Error cancelling interview schedule: {ex.Message}"));
            }
        }

        // =============================================
        // EVALUATION APIs
        // =============================================

        [HttpGet("EvaluationCriteria/{companyID}")]
        public async Task<IActionResult> GetEvaluationCriteria(int companyID)
        {
            try
            {
                var result = await _service.GetEvaluationCriteriaAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationCriteria");
                return StatusCode(500, ApiResponse<List<EvaluationCriteriaDto>>.Fail($"Error retrieving evaluation criteria: {ex.Message}"));
            }
        }

        [HttpGet("RatingScales/{companyID}")]
        public async Task<IActionResult> GetRatingScales(int companyID)
        {
            try
            {
                var result = await _service.GetRatingScalesAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRatingScales");
                return StatusCode(500, ApiResponse<List<RatingScaleDto>>.Fail($"Error retrieving rating scales: {ex.Message}"));
            }
        }

        [HttpPost("Evaluations")]
        [AuditLog("RECRUITMENT", "SubmitEvaluation", "Evaluation")]
        public async Task<IActionResult> SubmitEvaluation([FromBody] SubmitEvaluationRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _service.SubmitEvaluationAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitEvaluation");
                return StatusCode(500, ApiResponse<SubmitEvaluationResponseDto>.Fail($"Error submitting evaluation: {ex.Message}"));
            }
        }

        [HttpGet("Evaluations/Schedule/{scheduleID}")]
        public async Task<IActionResult> GetEvaluationsBySchedule(int scheduleID)
        {
            try
            {
                var result = await _service.GetEvaluationsByScheduleAsync(scheduleID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationsBySchedule");
                return StatusCode(500, ApiResponse<List<EvaluationDto>>.Fail($"Error retrieving evaluations: {ex.Message}"));
            }
        }

        [HttpGet("Evaluations/Application/{applicationID}")]
        public async Task<IActionResult> GetEvaluationsByApplication(int applicationID)
        {
            try
            {
                var result = await _service.GetEvaluationsByApplicationAsync(applicationID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationsByApplication");
                return StatusCode(500, ApiResponse<List<EvaluationDto>>.Fail($"Error retrieving evaluations: {ex.Message}"));
            }
        }

        // =============================================
        // MASTER DATA APIs
        // =============================================

        [HttpGet("ApplicationSources/{companyID}")]
        public async Task<IActionResult> GetApplicationSources(int companyID)
        {
            try
            {
                var result = await _service.GetApplicationSourcesAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationSources");
                return StatusCode(500, ApiResponse<List<ApplicationSourceDto>>.Fail($"Error retrieving application sources: {ex.Message}"));
            }
        }

        [HttpGet("InterviewTypes/{companyID}")]
        public async Task<IActionResult> GetInterviewTypes(int companyID)
        {
            try
            {
                var result = await _service.GetInterviewTypesAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewTypes");
                return StatusCode(500, ApiResponse<List<InterviewTypeDto>>.Fail($"Error retrieving interview types: {ex.Message}"));
            }
        }

        [HttpGet("Venues/{companyID}")]
        public async Task<IActionResult> GetVenues(int companyID)
        {
            try
            {
                var result = await _service.GetVenuesAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVenues");
                return StatusCode(500, ApiResponse<List<VenueDto>>.Fail($"Error retrieving venues: {ex.Message}"));
            }
        }

        [HttpGet("NotificationMethods/{companyID}")]
        public async Task<IActionResult> GetNotificationMethods(int companyID)
        {
            try
            {
                var result = await _service.GetNotificationMethodsAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNotificationMethods");
                return StatusCode(500, ApiResponse<List<NotificationMethodDto>>.Fail($"Error retrieving notification methods: {ex.Message}"));
            }
        }

        [HttpGet("Statuses/Application/{companyID}")]
        public async Task<IActionResult> GetApplicationStatuses(int companyID)
        {
            try
            {
                var result = await _service.GetStatusesByTypeAsync("APPLICATION", companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationStatuses");
                return StatusCode(500, ApiResponse<List<StatusResponseDto>>.Fail($"Error retrieving application statuses: {ex.Message}"));
            }
        }

        [HttpGet("Statuses/Requisition/{companyID}")]
        public async Task<IActionResult> GetRequisitionStatuses(int companyID)
        {
            try
            {
                var result = await _service.GetStatusesByTypeAsync("REQUISITION", companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRequisitionStatuses");
                return StatusCode(500, ApiResponse<List<StatusResponseDto>>.Fail($"Error retrieving requisition statuses: {ex.Message}"));
            }
        }

        [HttpGet("Statuses/Schedule/{companyID}")]
        public async Task<IActionResult> GetScheduleStatuses(int companyID)
        {
            try
            {
                var result = await _service.GetStatusesByTypeAsync("SCHEDULE", companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetScheduleStatuses");
                return StatusCode(500, ApiResponse<List<StatusResponseDto>>.Fail($"Error retrieving schedule statuses: {ex.Message}"));
            }
        }

        // =============================================
        // DASHBOARD APIs
        // =============================================

        [HttpGet("Dashboard/Stats/{companyID}")]
        public async Task<IActionResult> GetDashboardStatistics(int companyID)
        {
            try
            {
                var result = await _service.GetDashboardStatisticsAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardStatistics");
                return StatusCode(500, ApiResponse<DashboardResponseDto>.Fail($"Error retrieving dashboard statistics: {ex.Message}"));
            }
        }

        // =============================================
        // FILE UPLOAD APIs
        // =============================================


        [HttpPost("Resume/Upload")]
        [AllowAnonymous]
        [AuditLog("RECRUITMENT", "Upload", "Resume")]
        [Consumes("multipart/form-data")]
        // NOTE: no [FromForm] on the IFormFile — Swashbuckle throws
        // SwaggerGeneratorException for that combination, which made the whole
        // /swagger/v1/swagger.json document fail with a 500. IFormFile already
        // binds from multipart form data by default, so the wire contract is
        // unchanged. See github.com/domaindrivendev/Swashbuckle.AspNetCore#handle-forms-and-file-uploads
        public async Task<IActionResult> UploadResume(IFormFile file, [FromForm] int companyID)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<UploadResumeResponseDto>.Fail("File is required"));

                // Validate file type
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(ApiResponse<UploadResumeResponseDto>.Fail("Invalid file type. Only PDF, DOC, and DOCX files are allowed"));

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                    return BadRequest(ApiResponse<UploadResumeResponseDto>.Fail("File size exceeds maximum limit of 10MB"));

                // Generate unique filename
                //var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                //var safeFileName = Path.GetFileNameWithoutExtension(file.FileName)
                //    .Replace(" ", "_")
                //    .Replace("(", "")
                //    .Replace(")", "");
                //var uniqueFileName = $"{safeFileName}_{timestamp}{fileExtension}";

                // Save file
                var companyName = GetCurrentCompanyName();
                var safeCompanyName = companyName?.Replace(" ", "") ?? string.Empty;
                //var safeCompanyName = $"company_{companyID}";
                var filePath = await _fileService.SaveFileAsync(file, safeCompanyName, "recruitment", "resumes");

                if (string.IsNullOrEmpty(filePath))
                    return StatusCode(500, ApiResponse<UploadResumeResponseDto>.Fail("Failed to save file"));

                var response = new UploadResumeResponseDto
                {
                    RelativePath = filePath,
                    Url = _fileStorageService.GetFullUrl(filePath),
                    FileName = filePath,
                    FileSize = file.Length,
                    FileType = fileExtension
                };

                return Ok(ApiResponse<UploadResumeResponseDto>.Success(response, "Resume uploaded successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UploadResume");
                return StatusCode(500, ApiResponse<UploadResumeResponseDto>.Fail($"Error uploading resume: {ex.Message}"));
            }
        }

        [HttpPost("submit-for-approval")]
        [AuditLog("RECRUITMENT", "POST", "Recruitment")]
        public async Task<IActionResult> SubmitJobRequisitionForApproval([FromQuery] int requisitionId)
        {
            var companyId = GetCurrentCompanyId();
            var actor = GetCurrentEmployeeCode();
            var employeeId = GetCurrentEmployeeId();

            if (companyId == null)
                return BadRequest("Company required");

            var result = await _service.SubmitJobRequisitionForApprovalAsync(requisitionId,companyId.Value,(int)employeeId,actor);

            return HandleServiceResult(result, "Job Requisition submitted for approval");
        }

        // =============================================
        // PANEL MEMBER EVALUATION APIs
        // =============================================

        [HttpGet("InterviewSchedules/PanelMember/{interviewerID}")]
        public async Task<IActionResult> GetPanelMemberSchedules(
            int interviewerID,
            [FromQuery] int companyID,
            [FromQuery] int? statusID = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _service.GetPanelMemberSchedulesAsync(interviewerID, companyID, statusID, startDate, endDate);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPanelMemberSchedules");
                return StatusCode(500, ApiResponse<PanelMemberScheduleListResponseDto>.Fail($"Error retrieving panel member schedules: {ex.Message}"));
            }
        }

        [HttpGet("PanelEvaluations/{scheduleID}/{interviewerID}")]
        public async Task<IActionResult> GetPanelEvaluation(int scheduleID, int interviewerID)
        {
            try
            {
                var result = await _service.GetPanelEvaluationAsync(scheduleID, interviewerID);
                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPanelEvaluation");
                return StatusCode(500, ApiResponse<PanelEvaluationResponseDto>.Fail($"Error retrieving evaluation: {ex.Message}"));
            }
        }

        [HttpPost("PanelEvaluations")]
        [AuditLog("RECRUITMENT", "SubmitEvaluation", "PanelEvaluation")]
        public async Task<IActionResult> SavePanelEvaluation([FromBody] PanelEvaluationRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var employeeCode = GetCurrentEmployeeCode();
                request.CreatedBy ??= employeeCode;

                var result = await _service.SavePanelEvaluationAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavePanelEvaluation");
                return StatusCode(500, ApiResponse<PanelEvaluationResponseDto>.Fail($"Error saving evaluation: {ex.Message}"));
            }
        }

        [HttpPost("InterviewPanels/{panelID}/Confirm")]
        [AuditLog("RECRUITMENT", "ConfirmAttendance", "InterviewPanel")]
        public async Task<IActionResult> ConfirmPanelAttendance(int panelID, [FromBody] ConfirmPanelAttendanceRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var employeeCode = GetCurrentEmployeeCode();
                request.ConfirmedBy ??= employeeCode;

                var result = await _service.ConfirmPanelAttendanceAsync(panelID, request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmPanelAttendance");
                return StatusCode(500, ApiResponse<ConfirmPanelAttendanceResponseDto>.Fail($"Error confirming attendance: {ex.Message}"));
            }
        }

        [HttpGet("Recommendations/{companyID}")]
        public async Task<IActionResult> GetRecommendations(int companyID)
        {
            try
            {
                var result = await _service.GetRecommendationsAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendations");
                return StatusCode(500, ApiResponse<List<RecommendationDto>>.Fail($"Error retrieving recommendations: {ex.Message}"));
            }
        }

        [HttpGet("EvaluationCriteria/{companyID}/WithRatings")]
        public async Task<IActionResult> GetEvaluationCriteriaWithRatings(int companyID)
        {
            try
            {
                var result = await _service.GetEvaluationCriteriaWithRatingsAsync(companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationCriteriaWithRatings");
                return StatusCode(500, ApiResponse<List<EvaluationCriteriaWithRatingsDto>>.Fail($"Error retrieving evaluation criteria: {ex.Message}"));
            }
        }

        [HttpGet("InterviewSchedules/ConfirmedHeads")]
        public async Task<IActionResult> GetConfirmedHeadSchedules([FromQuery] int companyID,[FromQuery] int? statusID = null,[FromQuery] DateTime? startDate = null,[FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _service.GetConfirmedHeadSchedulesAsync(companyID, statusID, startDate, endDate);

                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetConfirmedHeadSchedules");
                return StatusCode(500,
                    ApiResponse<PanelMemberScheduleListResponseDto>
                        .Fail($"Error retrieving schedules: {ex.Message}"));
            }
        }


        [HttpGet("Applications/AIStatus/{applicationID}")]
        public async Task<IActionResult> GetApplicationAIStatus(int applicationID,[FromQuery] int companyID)
        {
            try
            {
                var result = await _service.GetApplicationAIStatusAsync(applicationID, companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationAIStatus");
                return StatusCode(500,
                    ApiResponse<ApplicationAIStatusDto>
                    .Fail($"Error retrieving AI status: {ex.Message}"));
            }
        }

        // =============================================
        // JOB BANK APIs
        // =============================================

        /// <summary>Register a candidate in the job bank. Use Resume/Upload first and pass relativePath as resumeFilePath.</summary>
        [HttpPost("JobBank/Candidates")]
        [AuditLog("RECRUITMENT", "JobBank", "JobBankCandidate")]
        [AllowAnonymous]
        public async Task<IActionResult> JobBankRegisterCandidate([FromBody] JobBankCandidateInsertRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && request.CompanyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<JobBankCandidateInsertResponseDto>.Fail("CompanyID does not match authenticated user's company."));
                var createdBy = GetCurrentEmployeeCode();
                var result = await _service.JobBankCandidateInsertAsync(request, createdBy);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankRegisterCandidate");
                return StatusCode(500, ApiResponse<JobBankCandidateInsertResponseDto>.Fail($"Error registering candidate: {ex.Message}"));
            }
        }

        /// <summary>Update job bank candidate profile. companyID from token is used for validation.</summary>
        [HttpPut("JobBank/Candidates/{id}")]
        [AuditLog("RECRUITMENT", "JobBank", "JobBankCandidate")]
        public async Task<IActionResult> JobBankUpdateCandidate(int id, [FromBody] JobBankCandidateUpdateRequestDto request)
        {
            try
            {
                if (request.JobBankCandidateID != id)
                    return BadRequest(ApiResponse<bool>.Fail("ID in URL does not match request body."));
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var result = await _service.JobBankCandidateUpdateAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankUpdateCandidate");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error updating candidate: {ex.Message}"));
            }
        }

        /// <summary>Search job bank with filters (skills, experience, education, city, pagination).</summary>
        [HttpGet("JobBank/Candidates/Search")]
        public async Task<IActionResult> JobBankSearchCandidates([FromQuery] JobBankCandidateSearchRequestDto request)
        {
            try
            {
                if (request.CompanyID <= 0)
                    return BadRequest(ApiResponse<JobBankCandidateSearchResponseDto>.Fail("companyID is required."));
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && request.CompanyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<JobBankCandidateSearchResponseDto>.Fail("CompanyID does not match authenticated user's company."));
                var result = await _service.JobBankCandidateSearchAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankSearchCandidates");
                return StatusCode(500, ApiResponse<JobBankCandidateSearchResponseDto>.Fail($"Error searching candidates: {ex.Message}"));
            }
        }

        /// <summary>Get job bank candidates list (admin). companyID, searchText, pageNumber, pageSize.</summary>
        [HttpGet("JobBank/Candidates/List")]
        public async Task<IActionResult> JobBankGetCandidatesList([FromQuery] JobBankCandidateListRequestDto request)
        {
            try
            {
                if (request.CompanyID <= 0)
                    return BadRequest(ApiResponse<JobBankCandidateSearchResponseDto>.Fail("companyID is required."));
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && request.CompanyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<JobBankCandidateSearchResponseDto>.Fail("CompanyID does not match authenticated user's company."));
                var result = await _service.JobBankCandidateGetListAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankGetCandidatesList");
                return StatusCode(500, ApiResponse<JobBankCandidateSearchResponseDto>.Fail($"Error retrieving list: {ex.Message}"));
            }
        }

        /// <summary>Get single job bank candidate by ID. companyID required for validation.</summary>
        [HttpGet("JobBank/Candidates/{id}")]
        public async Task<IActionResult> JobBankGetCandidateById(int id, [FromQuery] int companyID)
        {
            try
            {
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && companyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<JobBankCandidateResponseDto>.Fail("CompanyID does not match authenticated user's company."));
                var result = await _service.JobBankCandidateGetByIdAsync(id, companyID);
                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankGetCandidateById");
                return StatusCode(500, ApiResponse<JobBankCandidateResponseDto>.Fail($"Error retrieving candidate: {ex.Message}"));
            }
        }

        /// <summary>Shortlist a job bank candidate for a requisition.</summary>
        [HttpPost("JobBank/Shortlist")]
        [AuditLog("RECRUITMENT", "JobBank", "JobBankShortlist")]
        public async Task<IActionResult> JobBankShortlist([FromBody] JobBankShortlistInsertRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && request.CompanyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<JobBankShortlistInsertResponseDto>.Fail("CompanyID does not match authenticated user's company."));
                var shortlistedBy = GetCurrentEmployeeCode();
                if (request.ShortlistedBy == null && shortlistedBy != null)
                    request.ShortlistedBy = shortlistedBy;
                var result = await _service.JobBankShortlistInsertAsync(request);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlist");
                return StatusCode(500, ApiResponse<JobBankShortlistInsertResponseDto>.Fail($"Error shortlisting: {ex.Message}"));
            }
        }

        [HttpPost("JobBank/ConvertToApplication")]
        public async Task<IActionResult> ConvertToApplication([FromBody] ConvertRequestDto request)
        {
            var employeeCode = GetCurrentEmployeeCode();
            request.CreatedBy = employeeCode;
            var result = await _service.ConvertJobBankCandidateAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        /// <summary>Get shortlisted candidates for a requisition.</summary>
        [HttpGet("JobBank/Shortlist/ByRequisition/{requisitionID}")]
        public async Task<IActionResult> JobBankGetShortlistByRequisition(int requisitionID, [FromQuery] int companyID)
        {
            try
            {
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && companyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<List<JobBankShortlistByRequisitionDto>>.Fail("CompanyID does not match authenticated user's company."));
                var result = await _service.JobBankShortlistGetByRequisitionAsync(requisitionID, companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankGetShortlistByRequisition");
                return StatusCode(500, ApiResponse<List<JobBankShortlistByRequisitionDto>>.Fail($"Error retrieving shortlist: {ex.Message}"));
            }
        }

        /// <summary>Remove a candidate from shortlist for a requisition.</summary>
        [HttpDelete("JobBank/Shortlist/{jobBankShortlistID}")]
        [AuditLog("RECRUITMENT", "JobBank", "JobBankShortlist")]
        public async Task<IActionResult> JobBankRemoveShortlist(int jobBankShortlistID, [FromQuery] int companyID)
        {
            try
            {
                var userCompanyId = GetCurrentCompanyId();
                if (userCompanyId.HasValue && companyID != userCompanyId.Value)
                    return BadRequest(ApiResponse<bool>.Fail("CompanyID does not match authenticated user's company."));
                var result = await _service.JobBankShortlistRemoveAsync(jobBankShortlistID, companyID);
                return result.IsSuccess ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankRemoveShortlist");
                return StatusCode(500, ApiResponse<bool>.Fail($"Error removing shortlist: {ex.Message}"));
            }
        }

        [HttpPost("close")]
        [AuditLog("RECRUITMENT", "POST", "Recruitment")]
        public async Task<IActionResult> CloseJobRequisition([FromQuery] int requisitionId)
        {
            var companyId = GetCurrentCompanyId();
            var actor = GetCurrentEmployeeCode();

            if (companyId == null)
                return BadRequest("Company required");

            var result = await _service.CloseJobRequisitionAsync(requisitionId,companyId.Value,actor);

            return HandleServiceResult(result, "Job Requisition closed successfully");
        }

        [HttpPost("send-notification")]
        [AuditLog("RECRUITMENT", "POST", "Recruitment")]
        public async Task<IActionResult> SendInterviewNotification([FromQuery] int scheduleId, [FromQuery] string? formUrl)
        {
            var companyId = GetCurrentCompanyId();
            var actor = GetCurrentEmployeeCode();

            if (companyId == null)
                return BadRequest("Company required");

            var result = await _service.SendInterviewNotificationAsync(scheduleId, companyId.Value, actor, formUrl);

            return HandleServiceResult(result, "Interview notification sent successfully");
        }

        [HttpGet("Evaluation")]
        public async Task<IActionResult> GetEvaluationBySchedule([FromQuery] int scheduleId)
        {
            try
            {
                if (scheduleId <= 0)
                    return BadRequest("Invalid ScheduleID");

                var result = await _service.GetEvaluationByScheduleAsync(scheduleId);

                return result.IsSuccess ? Ok(result) : NotFound(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationBySchedule");
                return StatusCode(500, ApiResponse<EvaluationResponseDtos>
                    .Fail($"Error retrieving evaluation: {ex.Message}"));
            }
        }


        [HttpPost("HireCandidateStatus/{applicationID}")]
        public async Task<IActionResult> HireCandidateStatus(int applicationID, [FromBody] HireCandidateDto dto)
        {
            try
            {
                if (applicationID <= 0)
                    return BadRequest("Invalid ApplicationID");

                var result = await _service.HireCandidateStatus(applicationID, dto);

                return Ok(new
                {
                    isSuccess = result,
                    message = result ? "Candidate hired successfully" : "Failed to hire candidate"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}

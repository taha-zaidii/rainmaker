using Dapper;
using Digi.Recruitment.Module.Domain.Repositories.IRepositories;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Data.Common;

namespace Digi.Recruitment.Module.Domain.Repositories
{
    public class RecruitmentRepository : IRecruitmentRepository
    {
        private readonly IDbConnection _db;
        private readonly ILogger<RecruitmentRepository> _logger;

        public RecruitmentRepository(IDbConnection db, ILogger<RecruitmentRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(int? NewId, bool IsSuccess, string Message, RecruitmentRequisitionDto? Row)> SaveAsync(SaveRecruitmentRequisitionRequest req,CancellationToken cancellationToken = default)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                string? attachmentJson = null;
                if (req.attachments != null)
                {
                    attachmentJson = JsonConvert.SerializeObject(new
                    {
                        Attachments = req.attachments
                    });
                }

                var p = new DynamicParameters();

                // Inputs
                p.Add("@CompanyID", req.CompanyID);
                p.Add("@EmployeeID", req.EmployeeID);
                //p.Add("@ModuleId", req.ModuleId);
               // p.Add("@ObjectId", req.ObjectId);
                p.Add("@RecruitmentRequisitionName", req.RecruitmentRequisitionName);
                // The job summary was never passed, so the AI's "About the role"
                // text was dropped on save and the careers page had nothing to show.
                p.Add("@JobSummary", req.JobSummary);
                p.Add("@BudgetPeriodId", req.BudgetPeriodId);
                p.Add("@IsSystemDefault", req.IsSystemDefault);
                p.Add("@Location", req.Location);
               // p.Add("@ClusterId", req.ClusterId);
                p.Add("@JobCategoryID", req.JobCategoryID);
                p.Add("@DesignationID", req.DesignationID);
               // p.Add("@JdId", req.JdId);
                p.Add("@Vacancies", req.Vacancies);
               // p.Add("@Replacement", req.Replacement);
               // p.Add("@ReportingPersonCode", req.ReportingPersonCode);
                p.Add("@CommenceWorkOn", req.CommenceWorkOn);
                p.Add("@EmploymentTypeID", req.EmploymentTypeID);
                p.Add("@GradeID", req.GradeID);
                p.Add("@AgeText", req.AgeText);
                p.Add("@ExperienceYears", req.ExperienceYears);
                p.Add("@QualificationsEntryRequirments", req.QualificationsEntryRequirments);
                p.Add("@Exposure", req.Exposure);
                p.Add("@SkillsRequired", req.SkillsRequired);
                p.Add("@SpecialAttributes", req.SpecialAttributes);
                p.Add("@Comments", req.Comments);
                p.Add("@KeyResponsibilities", req.KeyResponsibilities);
                p.Add("@KeyDeliverables", req.KeyDeliverables);
                p.Add("@OtherRequirments", req.OtherRequirments);
                p.Add("@TechnicalCompetencies", req.TechnicalCompetencies);
                p.Add("@EducationalQualifications", req.EducationalQualifications);
                p.Add("@EducationalQualificationsDesirable", req.EducationalQualificationsDesirable);
                p.Add("@RequiredExperiences", req.RequiredExperiences);
                p.Add("@RequiredExperiencesDesirable", req.RequiredExperiencesDesirable);
                p.Add("@RequiredTrainings", req.RequiredTrainings);
                p.Add("@RequiredTrainingsDesirable", req.RequiredTrainingsDesirable);
                // Was dropped silently: HR's Step 4 justification (why this role is
                // being requisitioned) never reached the SP despite the wizard
                // collecting it and the SP accepting it — see SaveJobDescriptionRequestDto.
                p.Add("@Justification", req.Justification);
                //p.Add("@JustificationBy", req.JustificationBy);
                //p.Add("@JustificationDate", req.JustificationDate);
                //p.Add("@ToInternal", req.ToInternal);
               // p.Add("@ToExternal", req.ToExternal);
                //p.Add("@ToThirdParty", req.ToThirdParty);
                p.Add("@AlwaysPublished", req.AlwaysPublished);
                p.Add("@PublishStatus", req.PublishStatus);
                p.Add("@RecruitmentRequisitionDate", req.RecruitmentRequisitionDate);
                p.Add("@RecruitmentRequisitionClosingDate", req.RecruitmentRequisitionClosingDate);
                //p.Add("@NewPublishNotifiedToAll", req.NewPublishNotifiedToAll);
                p.Add("@PublishedBy", req.PublishedBy);
                p.Add("@PublishedDate", req.PublishedDate);
                //p.Add("@RequestId", req.RequestId);
                p.Add("@ApprovalStatus", req.ApprovalStatus);
                p.Add("@IsClosed", req.IsClosed);
                //p.Add("@ReplacementEmpType", req.ReplacementEmpType);
                p.Add("@AttachedDocument", req.AttachedDocument);  // SP has this parameter

                p.Add("@DepartmentID", req.DepartmentID);
                p.Add("@Salary", req.Salary);
               // p.Add("@Status", req.Status);

                // Required user id for audit
                p.Add("@EmployeeCode", req.EmployeeCode);

                // Attachment JSON (optional param in SP)
                p.Add("@AttachmentURL",
                    value: string.IsNullOrWhiteSpace(attachmentJson) ? null : attachmentJson,
                    dbType: DbType.String,
                    direction: ParameterDirection.Input);

                // Outputs
                p.Add("@NewID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                p.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                // Execute stored proc — SP returns the row via SELECT at the end
                using var multi = await _db.QueryMultipleAsync(
                    "ruc.SP_Ruc_JobRequisition_Create",
                    p,
                    commandType: CommandType.StoredProcedure);

                RecruitmentRequisitionDto? row = null;
                try
                {
                    row = await multi.ReadFirstOrDefaultAsync<RecruitmentRequisitionDto>();
                }
                catch
                {
                    // no row returned — fine
                }

                var newId = p.Get<int?>("@NewID");
                var isSuccess = p.Get<bool>("@IsSuccess");
                var message = p.Get<string>("@Message") ?? string.Empty;

                return (newId, isSuccess, message, row);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing [sp_Hr_Ruc_RecruitmentRequisition_Insert]");
                throw;
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                    _db.Close();
            }
        }


        public async Task<(bool IsSuccess, string Message, RecruitmentRequisitionDto? Row)> UpdateAsync(UpdateRecruitmentRequisitionRequest req, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                string? attachmentJson = null;
                if (req.attachments != null)
                {
                    attachmentJson = JsonConvert.SerializeObject(new
                    {
                        Attachments = req.attachments
                    });
                }

                var p = new DynamicParameters();

                // Required
                p.Add("@RecruitmentRequisitionID", req.RecruitmentRequisitionID);
                p.Add("@EmployeeCode", req.EmployeeCode);

                // Optionals (same mapping as Insert)
                p.Add("@CompanyID", req.CompanyID);
                p.Add("@EmployeeID", req.EmployeeID);
                p.Add("@RecruitmentRequisitionName", req.RecruitmentRequisitionName);
                p.Add("@BudgetPeriodId", req.BudgetPeriodId);
                p.Add("@Location", req.Location);
                p.Add("@JobCategoryID", req.JobCategoryID);
                p.Add("@DesignationID", req.DesignationID);
                p.Add("@Vacancies", req.Vacancies);
                p.Add("@CommenceWorkOn", req.CommenceWorkOn);
                p.Add("@EmploymentTypeID", req.EmploymentTypeID);
                p.Add("@GradeID", req.GradeID);
                p.Add("@AgeText", req.AgeText);
                p.Add("@ExperienceYears", req.ExperienceYears);
                p.Add("@QualificationsEntryRequirments", req.QualificationsEntryRequirments);
                p.Add("@Exposure", req.Exposure);
                p.Add("@SkillsRequired", req.SkillsRequired);
                p.Add("@SpecialAttributes", req.SpecialAttributes);
                p.Add("@Comments", req.Comments);
                p.Add("@KeyResponsibilities", req.KeyResponsibilities);
                p.Add("@KeyDeliverables", req.KeyDeliverables);
                p.Add("@OtherRequirments", req.OtherRequirments);
                p.Add("@TechnicalCompetencies", req.TechnicalCompetencies);
                p.Add("@EducationalQualifications", req.EducationalQualifications);
                p.Add("@EducationalQualificationsDesirable", req.EducationalQualificationsDesirable);
                p.Add("@RequiredExperiences", req.RequiredExperiences);
                p.Add("@RequiredExperiencesDesirable", req.RequiredExperiencesDesirable);
                p.Add("@RequiredTrainings", req.RequiredTrainings);
                p.Add("@RequiredTrainingsDesirable", req.RequiredTrainingsDesirable);
                p.Add("@AlwaysPublished", req.AlwaysPublished);
                p.Add("@PublishStatus", req.PublishStatus);
                p.Add("@RecruitmentRequisitionDate", req.RecruitmentRequisitionDate);
                p.Add("@RecruitmentRequisitionClosingDate", req.RecruitmentRequisitionClosingDate);
                p.Add("@PublishedBy", req.PublishedBy);
                p.Add("@PublishedDate", req.PublishedDate);
                p.Add("@ApprovalStatus", req.ApprovalStatus);
                p.Add("@IsClosed", req.IsClosed);
                //p.Add("@AttachedDocument", req.AttachedDocument);
                p.Add("@DepartmentID", req.DepartmentID);
                p.Add("@Salary", req.Salary);

               // Attachments JSON
                p.Add("@AttachmentURL",
                    value: string.IsNullOrWhiteSpace(attachmentJson) ? null : attachmentJson,
                    dbType: DbType.String,
                    direction: ParameterDirection.Input);

                // Outputs
                p.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                p.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                // Call stored procedure
                using var multi = await _db.QueryMultipleAsync(
                    "sp_Hr_Ruc_RecruitmentRequisition_Update",
                    p,
                    commandType: CommandType.StoredProcedure);

                RecruitmentRequisitionDto? row = null;
                try
                {
                    row = await multi.ReadFirstOrDefaultAsync<RecruitmentRequisitionDto>();
                }
                catch
                {
                    // no row returned
                }

                var isSuccess = p.Get<bool>("@IsSuccess");
                var message = p.Get<string>("@Message") ?? string.Empty;

                return (isSuccess, message, row);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing [sp_Hr_Ruc_RecruitmentRequisition_Update]");
                throw;
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                    _db.Close();
            }
        }


        public async Task<RecruitmentRequisitionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_db.State != ConnectionState.Open) _db.Open();

                const string sql = @"
                    SELECT TOP(1) *
                    FROM dbo.Tbl_Hr_Ruc_RecruitmentRequisition
                    WHERE RecruitmentRequisitionID = @Id;
                ";

                return await _db.QueryFirstOrDefaultAsync<RecruitmentRequisitionDto>(sql, new { Id = id });
            }
            finally
            {
                if (_db.State == ConnectionState.Open)
                    _db.Close();
            }
        }

        public async Task<IEnumerable<RecruitmentRequisitionGetDto>> GetAllAsync(int? companyId, int? year, bool? isPublished, string? searchText, int pageNumber, int pageSize, string? employeeCode)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Action", "GETALL");
            parameters.Add("@CompanyID", companyId);
            parameters.Add("@Year", year);
            parameters.Add("@IsPublished", isPublished);
            parameters.Add("@SearchText", searchText);
            parameters.Add("@PageNumber", pageNumber);
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@EmployeeCode", employeeCode);

            return await _db.QueryAsync<RecruitmentRequisitionGetDto>(
                "sp_HR_Ruc_RecruitmentRequisition_GetAllInformation",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<RecruitmentRequisitionGetDto> GetByIdAsync(int requisitionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Action", "GETBYID");
            parameters.Add("@RecruitmentRequisitionID", requisitionId);

            return await _db.QueryFirstOrDefaultAsync<RecruitmentRequisitionGetDto>(
                "sp_HR_Ruc_RecruitmentRequisition_Get",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<RecruitmentSummaryDto>> GetSummaryAsync(int? companyId, int? year)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Action", "SUMMARY");
            parameters.Add("@CompanyID", companyId);
            parameters.Add("@Year", year);

            var result = await _db.QueryAsync<RecruitmentSummaryDto>(
                "sp_HR_Ruc_RecruitmentRequisition_Get",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<CandidateAssignGroupDto>> GetSummaryAsync(int? companyId, string? filterBy, int? year, string? searchText)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompanyID", companyId);
            parameters.Add("@FilterBy", filterBy);
            parameters.Add("@Year", year);
            parameters.Add("@SearchText", searchText);

            return await _db.QueryAsync<CandidateAssignGroupDto>(
                "sp_HR_Ruc_CandidateAssignGroup",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<RecruitmentJobDetailDto> GetJobDetailsAsync(int recruitmentRequisitionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RecruitmentRequisitionID", recruitmentRequisitionId);

            var result = await _db.QueryFirstOrDefaultAsync<RecruitmentJobDetailDto>(
                "sp_Hr_Ruc_GetRecruitmentRequisitionJobDetails",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<JobApplicationDto>> GetApplicationsByRequisitionAsync(int jobRequisitionId, int companyID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@JobRequisitionID", jobRequisitionId);
            parameters.Add("@CompanyID", companyID);

            var result = await _db.QueryAsync<JobApplicationDto>(
                "sp_Hr_Ruc_GetJobApplicationsByRequisition",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public async Task<IEnumerable<JobApplicationByShortListedDto>> GetApplicationsByShortListedAsync(int jobRequisitionId, int companyID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@JobRequisitionID", jobRequisitionId);
            parameters.Add("@CompanyID", companyID);

            var result = await _db.QueryAsync<JobApplicationByShortListedDto>(
                "sp_Hr_Ruc_GetJobApplicationsShortlisted",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public async Task<int> UpdateApplicationStatusAsync(UpdateJobApplicationStatusDto dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@JobApplicationID", dto.JobApplicationID);
            parameters.Add("@ScheduleStageID", dto.ScheduleStageID);
            parameters.Add("@Remarks", dto.Remarks);
            parameters.Add("@ScreeningScore", dto.ScreeningScore);
            parameters.Add("@IsShortlisted", dto.IsShortlisted);
            parameters.Add("@IsRejected", dto.IsRejected);
            parameters.Add("@UpdatedBy", dto.UpdatedBy);
            parameters.Add("@InterviewStateID", dto.InterviewStateID);
            //parameters.Add("@IsHired", dto.IsHired);
            //parameters.Add("@InterviewFeedback", dto.InterviewFeedback);
           // parameters.Add("@ApplicationStateID", dto.ApplicationStateID);

            var rowsAffected = await _db.ExecuteAsync(
                "sp_Hr_Ruc_UpdateJobApplicationStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected;
        }
        public async Task<IEnumerable<RecruitmentRequisitionPublicDto>> ManagePublicAsync(RecruitmentRequisitionActionDto dto)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Action", dto.Action);
            parameters.Add("@RecruitmentRequisitionID", dto.RecruitmentRequisitionID);
            parameters.Add("@EmployeeCode", dto.EmployeeCode);
            parameters.Add("@CompanyID", dto.CompanyID);

            if (dto.Action == "GET")
            {
                return await _db.QueryAsync<RecruitmentRequisitionPublicDto>(
                    "sp_Hr_Ruc_RecruitmentRequisition_Public_Manage",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            else
            {
                await _db.ExecuteAsync(
                    "sp_Hr_Ruc_RecruitmentRequisition_Public_Manage",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return new List<RecruitmentRequisitionPublicDto>();
            }
        }
        public async Task<IEnumerable<ApplicationStatusDto>> GetInterviewStatusesAsync()
        {
            
            return await _db.QueryAsync<ApplicationStatusDto>(
                "sp_Hr_Ruc_ApplicationStatus_GetInterviews",
                commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<ApplicationStatusDto>> GetNotificationMethodAsync()
        {

            return await _db.QueryAsync<ApplicationStatusDto>(
                "sp_Hr_Ruc_ApplicationStatus_Notification Method",
                commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<ApplicationStatusDto>> GetVenueAsync()
        {

            return await _db.QueryAsync<ApplicationStatusDto>(
                "sp_Hr_Ruc_ApplicationStatus_Venue",
                commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<ApplicationStatusDto>> GetRecommendationAsync()
        {

            return await _db.QueryAsync<ApplicationStatusDto>(
                "sp_Hr_Ruc_ApplicationStatus_Recommendation",
                commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<ApplicationStatusDto>> GetOtherStageStatusesAsync()
        {
            return await _db.QueryAsync<ApplicationStatusDto>(
                "sp_Hr_Ruc_ApplicationStatus_GetOtherStages",
                commandType: CommandType.StoredProcedure);
        }
        public async Task<IEnumerable<ShortlistedCandidateDto>> GetAllShortlistedAsync(int CompanyID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompanyID", CompanyID);

            return await _db.QueryAsync<ShortlistedCandidateDto>(
                "sp_Hr_Ruc_GetAllShortlistedCandidates",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        public async Task UpdateInterviewScheduleAsync(bool isHired, int jobApplicationID, int interviewStateID, int applicantID, int companyID , string empCode)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@IsHired", isHired);
            parameters.Add("@JobApplicationID", jobApplicationID);
            parameters.Add("@InterviewStateID", interviewStateID);
            parameters.Add("@ApplicantID", applicantID);
            parameters.Add("@CompanyID", companyID);
            parameters.Add("@EmpCode", empCode);
            

            await _db.ExecuteAsync(
                "sp_Hr_Ruc_UpdateInterviewScheduleIsHired",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }
        //public async Task SaveInterviewScheduleAsync(InterviewScheduleRequestDto request)
        //{
        //    var parameters = new DynamicParameters();

        //    parameters.Add("@CreatedBy", request.CreatedBy);
        //    parameters.Add("@CompanyID", request.CompanyID);

        //    // Bulk Updates TVP - matches TVP_ScheduleUpdate structure
        //    var bulkUpdatesTable = new DataTable();
        //    bulkUpdatesTable.Columns.Add("ScheduleHeaderId", typeof(int));
        //    bulkUpdatesTable.Columns.Add("ApplicantID", typeof(int));
        //    bulkUpdatesTable.Columns.Add("JobApplicationID", typeof(int));
        //    bulkUpdatesTable.Columns.Add("InterviewDate", typeof(DateTime));
        //    bulkUpdatesTable.Columns.Add("StartTime", typeof(TimeSpan));
        //    bulkUpdatesTable.Columns.Add("Duration", typeof(int));
        //    bulkUpdatesTable.Columns.Add("InterviewStateID", typeof(int));
        //    bulkUpdatesTable.Columns.Add("NotificationMethodID", typeof(int));
        //    bulkUpdatesTable.Columns.Add("VenueID", typeof(int));

        //    foreach (var c in request.Candidates)
        //    {
        //        bulkUpdatesTable.Rows.Add(
        //            c.ScheduleHeaderId, // Each candidate has its own ScheduleHeaderId
        //            c.ApplicantID,
        //            c.JobApplicationID,
        //            c.InterviewDate,
        //            c.StartTime,
        //            c.Duration,
        //            c.InterviewStateID,
        //            c.NotificationMethodID, // Each candidate has its own NotificationMethodID
        //            c.VenueID // Each candidate has its own VenueID
        //        );
        //    }

        //    parameters.Add("@BulkUpdates", bulkUpdatesTable.AsTableValuedParameter("TVP_ScheduleUpdate"));

        //    // Panel TVP
        //    var panelTable = new DataTable();
        //    panelTable.Columns.Add("InterviewerId", typeof(int));
        //    panelTable.Columns.Add("IsHead", typeof(bool));

        //    foreach (var p in request.PanelMembers)
        //    {
        //        panelTable.Rows.Add(p.InterviewerId, p.IsHead);
        //    }

        //    parameters.Add("@PanelList", panelTable.AsTableValuedParameter("TVP_Interviewers"));

        //    await _db.ExecuteAsync(
        //        "sp_Hr_Ruc_SaveInterviewSchedule",
        //        parameters,
        //        commandType: CommandType.StoredProcedure
        //    );
        //}

        public async Task<InterviewScheduleCollectionDto> GetAllInterviewSchedulesAsync(int CompanyID)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompanyID", CompanyID, DbType.Int32);

            using var multi = await _db.QueryMultipleAsync(
                "sp_Hr_Ruc_GetAllInterviewSchedules", parameters,
                commandType: CommandType.StoredProcedure);

            var schedules = (await multi.ReadAsync<InterviewScheduleDto>()).ToList();
            var panelMembers = (await multi.ReadAsync<InterviewPanelMemberDto>()).ToList();

            // Group panel members by ScheduleHeaderId
            foreach (var schedule in schedules)
            {
                schedule.PanelMembers = panelMembers
                    .Where(p => p.ScheduleHeaderId == schedule.ScheduleHeaderId)
                    .ToList();
            }

            return new InterviewScheduleCollectionDto
            {
                Schedules = schedules
            };
        }

        public async Task<IEnumerable<CandidateEvaluationDto>> GetCandidateEvaluationsAsync(long? requisitionId, int? interviewRound)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RequisitionId", requisitionId, DbType.Int64);
            parameters.Add("@InterviewRound", interviewRound, DbType.Int32);

            var result = await _db.QueryAsync<CandidateEvaluationDto>(
                "sp_Hr_Ruc_GetCandidateEvaluations",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
        public async Task<EmployeeEmailDto> GetEmployeeEmailsAsync(int employeeIds)
        {
            //var employees = new List<EmployeeEmailDto>();
            var parameters = new DynamicParameters();
            parameters.Add("@EmployeeID", employeeIds, DbType.Int64);

            var result = await _db.QueryFirstAsync<EmployeeEmailDto>(
                "sp_Hr_GetEmployeeEmailList",
                parameters,
                commandType: CommandType.StoredProcedure
            );            

            return result;
        }
        public async Task<ApiResponse<bool>> DeleteRecruitmentRequisitionAsync(int recruitmentRequisitionID,string employeeCode,string? reasonToDelete)
        {
            try
            {
              
                var parameters = new DynamicParameters();
                parameters.Add("@RecruitmentRequisitionID", recruitmentRequisitionID);
                parameters.Add("@EmployeeCode", employeeCode);
                parameters.Add("@ReasonToDelete", reasonToDelete);

                var result = await _db.QueryFirstAsync<dynamic>(
                    "sp_Hr_Ruc_RecruitmentRequisition_Delete",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.IsSuccess ? ApiResponse<bool>.Success(true, result.Message) : ApiResponse<bool>.Fail(result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recruitment requisition");
                return ApiResponse<bool>.Fail(ex.Message);
            }
        }

        // Auto Process Methods
        public async Task<(int ParsingID, bool IsSuccess, string Message)> AutoParseResumeAsync(AutoParseResumeRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@ResumePath", request.ResumePath);
                parameters.Add("@ResumeFileName", request.ResumeFileName);
                parameters.Add("@ParsedData", request.ParsedDataJson);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@ParsingID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_AI_AutoParseResume]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var parsingID = parameters.Get<int>("@ParsingID");

                return (parsingID, result == 1, result == 1 ? "Resume parsed successfully" : "Failed to parse resume");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoParseResumeAsync");
                return (0, false, ex.Message);
            }
        }

        public async Task<(int ScreeningID, bool AutoShortlisted, bool IsSuccess, string Message)> AutoScreenResumeAsync(AutoScreenResumeRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@ResumeParsingID", request.ResumeParsingID);
                parameters.Add("@MatchScore", request.MatchScore);
                parameters.Add("@Recommendation", request.Recommendation ?? "");
                parameters.Add("@SkillsMatch", request.SkillsMatch ?? "");
                parameters.Add("@ExperienceMatch", request.ExperienceMatch ?? "");
                parameters.Add("@QualificationsMatch", request.QualificationsMatch ?? "");
                parameters.Add("@RedFlags", request.RedFlags ?? "");
                parameters.Add("@ScreeningProvider", request.ScreeningProvider);
                parameters.Add("@ModelUsed", request.ModelUsed ?? "");
                parameters.Add("@TokensUsed", request.TokensUsed);
                parameters.Add("@ProcessingTime", request.ProcessingTime);
                parameters.Add("@AutoShortlistThreshold", request.AutoShortlistThreshold);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@ScreeningID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@AutoShortlisted", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_AI_AutoScreenResume]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var screeningID = parameters.Get<int>("@ScreeningID");
                var autoShortlisted = parameters.Get<bool>("@AutoShortlisted");

                return (screeningID, autoShortlisted, result == 1, result == 1 ? "Resume screened successfully" : "Failed to screen resume");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoScreenResumeAsync");
                return (0, false, false, ex.Message);
            }
        }

        public async Task<ApplicationStatusUpdateDto?> GetApplicationStatusAsync(int applicationID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);

                var row = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    "[ruc].[SP_Recruitment_GetApplicationStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (row == null)
                    return null;

                return new ApplicationStatusUpdateDto
                {
                    ApplicationID = row.ApplicationID,
                    CurrentStatusID = row.CurrentStatusID,
                    StatusCode = row.StatusCode,
                    StatusName = row.StatusName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationStatusAsync");
                return null;
            }
        }

        public async Task<(bool IsSuccess, string Message)> AutoShortlistCandidateAsync(AutoShortlistRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // The SP actually defined for this operation is SP_AI_AutoShortlistCandidate
                // (003_demo_recruitment_sps.sql) — SP_Recruitment_AutoShortlistCandidate,
                // called here previously, does not exist anywhere in db/seed and threw on
                // every call. Its full parameter list is kept even though this method only
                // surfaces IsSuccess/Message upward: RecruitmentService re-reads the new
                // status via GetApplicationStatusAsync rather than these outputs.
                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@AIScreeningScore", request.AIScreeningScore);
                parameters.Add("@Threshold", request.Threshold > 0 ? request.Threshold : 80);
                parameters.Add("@PreviousStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PreviousStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@AutoShortlisted", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@AutoShortlistDate", dbType: DbType.DateTime, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_AI_AutoShortlistCandidate]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Candidate auto-shortlisted successfully" : "Failed to auto-shortlist candidate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AutoShortlistCandidateAsync");
                return (false, ex.Message);
            }
        }

        // Interview Rounds Methods
        public async Task<List<InterviewRoundDto>> GetInterviewRoundsAsync(int companyID, int applicationID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);
                parameters.Add("@ApplicationID", applicationID);

                var result = await _db.QueryAsync<dynamic>(
                    "[ruc].[SP_Recruitment_GetInterviewRounds]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var rounds = new List<InterviewRoundDto>();
                foreach (var row in result)
                {
                    var round = new InterviewRoundDto
                    {
                        ScheduleID = row.ScheduleID,
                        ScheduleCode = row.ScheduleCode,
                        RoundNumber = row.InterviewRound,
                        ScheduledDate = row.ScheduledDate,
                        DurationMinutes = row.DurationMinutes,
                        Venue = row.Venue,
                        OnlineMeetingLink = row.OnlineMeetingLink,
                        Instructions = row.Instructions,
                        StatusID = row.StatusID,
                        StatusCode = row.StatusCode,
                        StatusName = row.StatusName,
                        FeedbackSummary = row.FeedbackSummary,
                        EvaluationID = row.EvaluationID
                    };

                    // Parse PanelMembers JSON
                    if (row.PanelMembers != null)
                    {
                        var panelMembers = JsonConvert.DeserializeObject<List<InterviewRoundPanelMemberDto>>(row.PanelMembers.ToString());
                        round.PanelMembers = panelMembers ?? new List<InterviewRoundPanelMemberDto>();
                    }

                    rounds.Add(round);
                }

                return rounds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewRoundsAsync");
                return new List<InterviewRoundDto>();
            }
        }

        public async Task<(int ScheduleID, string? ScheduleCode, bool IsSuccess, string Message)> ScheduleInterviewRoundAsync(ScheduleInterviewRoundRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@RoundNumber", request.RoundNumber);
                parameters.Add("@ScheduledDate", request.ScheduledDate);
                parameters.Add("@DurationMinutes", request.DurationMinutes);
                parameters.Add("@Venue", request.Venue);
                parameters.Add("@OnlineMeetingLink", request.OnlineMeetingLink);
                parameters.Add("@Instructions", request.Instructions);
                parameters.Add("@PanelMembers", JsonConvert.SerializeObject(request.PanelMembers));
                parameters.Add("@InterviewTypeID", request.InterviewTypeID);
                parameters.Add("@Comments", request.Comments);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@ScheduleID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ScheduleInterviewRound]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var scheduleID = parameters.Get<int>("@ScheduleID");
                var scheduleCode = $"INT-{request.ApplicationID}-R{request.RoundNumber}";

                return (scheduleID, scheduleCode, result == 1, result == 1 ? "Interview round scheduled successfully" : "Failed to schedule interview round");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ScheduleInterviewRoundAsync");
                return (0, null, false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> CompleteInterviewRoundAsync(CompleteInterviewRoundRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Get schedule details to extract RoundNumber
                var schedule = await GetInterviewScheduleByIdAsync(request.ScheduleID);
                if (schedule == null)
                    return (false, "Schedule not found");

                // Derive Passed from Outcome
                bool passed = request.Outcome.ToUpper() == "PASSED";
                int roundNumber = schedule.InterviewRound;
                
                // Get OverallRating and Recommendation from evaluation if available
                decimal overallRating = 0;
                string recommendation = "PENDING";
                int? evaluationID = null;

                // Try to get latest evaluation for this schedule
                var evaluation = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT TOP 1 EvaluationID, OverallRating, Recommendation 
                      FROM [ruc].[Tbl_CandidateEvaluation] 
                      WHERE ScheduleID = @ScheduleID 
                      ORDER BY CreatedOn DESC",
                    new { ScheduleID = request.ScheduleID }
                );

                if (evaluation != null)
                {
                    evaluationID = evaluation.EvaluationID;
                    overallRating = evaluation.OverallRating ?? 0;
                    recommendation = evaluation.Recommendation ?? "PENDING";
                }

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@ScheduleID", request.ScheduleID);
                parameters.Add("@RoundNumber", roundNumber);
                parameters.Add("@Passed", passed);
                parameters.Add("@OverallRating", overallRating);
                parameters.Add("@Recommendation", recommendation);
                parameters.Add("@EvaluationID", evaluationID);
                parameters.Add("@Comments", request.Comments);
                parameters.Add("@UpdatedBy", updatedBy ?? request.CompletedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_CompleteInterviewRound]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Interview round completed successfully" : "Failed to complete interview round");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompleteInterviewRoundAsync");
                return (false, ex.Message);
            }
        }

        public async Task<GetApplicationsByInterviewStatusResponseDto> GetApplicationsByInterviewStatusAsync(GetApplicationsByInterviewStatusRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@Status", request.Status);
                parameters.Add("@RoundNumber", request.RoundNumber);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var result = await _db.QueryAsync<ApplicationByInterviewStatusDto>(
                    "[ruc].[SP_Recruitment_GetApplicationsByInterviewStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var totalRecords = parameters.Get<int>("@TotalRecords");

                return new GetApplicationsByInterviewStatusResponseDto
                {
                    Applications = result.ToList(),
                    TotalRecords = totalRecords,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationsByInterviewStatusAsync");
                return new GetApplicationsByInterviewStatusResponseDto();
            }
        }

        // =============================================
        // CRUD OPERATIONS - APPLICANT
        // =============================================

        public async Task<(int ApplicantID, string ApplicantCode, bool IsSuccess, string Message)> CreateApplicantAsync(ApplicantCreateRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@FirstName", request.FirstName);
                parameters.Add("@MiddleName", request.MiddleName);
                parameters.Add("@LastName", request.LastName);
                parameters.Add("@DateOfBirth", request.DateOfBirth);
                parameters.Add("@GenderID", request.GenderID);
                parameters.Add("@NationalID", request.NationalID);
                parameters.Add("@Email", request.Email);
                parameters.Add("@MobileNumber", request.MobileNumber);
                parameters.Add("@PhoneNumber", request.PhoneNumber);
                parameters.Add("@CurrentAddress", request.CurrentAddress);
                parameters.Add("@CityID", request.CityID);
                parameters.Add("@CountryID", request.CountryID);
                parameters.Add("@MaritalStatusID", request.MaritalStatusID);
                parameters.Add("@ReligionID", request.ReligionID);
                parameters.Add("@TotalExperience", request.TotalExperience);
                parameters.Add("@CurrentJobTitle", request.CurrentJobTitle);
                parameters.Add("@CurrentCompany", request.CurrentCompany);
                parameters.Add("@ExpectedSalary", request.ExpectedSalary);
                parameters.Add("@NoticePeriod", request.NoticePeriod);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@ResumePath", request.ResumePath);
                parameters.Add("@CoverLetter", request.CoverLetter);
                parameters.Add("@Cnic", request.Cnic);
                parameters.Add("@Skills", request.Skills);
                parameters.Add("@ExperienceYears", request.ExperienceYears);
                parameters.Add("@ExperienceSummary", request.ExperienceSummary);
                parameters.Add("@Education", request.Education);
                parameters.Add("@CurrentDesignation", request.CurrentDesignation);
                parameters.Add("@PreferredLocation", request.PreferredLocation);
                parameters.Add("@ApplicantID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ApplicantCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_Applicant_Create]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var applicantID = parameters.Get<int>("@ApplicantID");
                var applicantCode = parameters.Get<string>("@ApplicantCode") ?? "";

                return (applicantID, applicantCode, result == 1, result == 1 ? "Applicant created successfully" : "Failed to create applicant");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApplicantAsync");
                return (0, "", false, ex.Message);
            }
        }

        public async Task<ApplicantResponseDto?> GetApplicantByIdAsync(int applicantID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicantID", applicantID);

                var result = await _db.QueryFirstOrDefaultAsync<ApplicantResponseDto>(
                    "[ruc].[SP_Ruc_Applicant_GetById]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicantByIdAsync");
                return null;
            }
        }

        public async Task<(List<ApplicantResponseDto> Applicants, int TotalCount)> GetAllApplicantsAsync(ApplicantListRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@SearchTerm", request.SearchTerm);
                parameters.Add("@IsActive", request.IsActive);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Ruc_Applicant_GetAll]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var applicants = (await multi.ReadAsync<ApplicantResponseDto>()).ToList();
                var totalCount = await multi.ReadFirstAsync<int>();

                return (applicants, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllApplicantsAsync");
                return (new List<ApplicantResponseDto>(), 0);
            }
        }

        public async Task<(bool IsSuccess, string Message)> UpdateApplicantAsync(ApplicantUpdateRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@FirstName", request.FirstName);
                parameters.Add("@MiddleName", request.MiddleName);
                parameters.Add("@LastName", request.LastName);
                parameters.Add("@DateOfBirth", request.DateOfBirth);
                parameters.Add("@GenderID", request.GenderID);
                parameters.Add("@NationalID", request.NationalID);
                parameters.Add("@Email", request.Email);
                parameters.Add("@MobileNumber", request.MobileNumber);
                parameters.Add("@PhoneNumber", request.PhoneNumber);
                parameters.Add("@CurrentAddress", request.CurrentAddress);
                parameters.Add("@CityID", request.CityID);
                parameters.Add("@CountryID", request.CountryID);
                parameters.Add("@MaritalStatusID", request.MaritalStatusID);
                parameters.Add("@ReligionID", request.ReligionID);
                parameters.Add("@TotalExperience", request.TotalExperience);
                parameters.Add("@CurrentJobTitle", request.CurrentJobTitle);
                parameters.Add("@CurrentCompany", request.CurrentCompany);
                parameters.Add("@ExpectedSalary", request.ExpectedSalary);
                parameters.Add("@NoticePeriod", request.NoticePeriod);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_Applicant_Update]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Applicant updated successfully" : "Failed to update applicant");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicantAsync");
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> DeleteApplicantAsync(int applicantID, string deletedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicantID", applicantID);
                parameters.Add("@DeletedBy", deletedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_Applicant_Delete]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Applicant deleted successfully" : "Failed to delete applicant");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteApplicantAsync");
                return (false, ex.Message);
            }
        }

        // =============================================
        // CRUD OPERATIONS - JOB REQUISITION
        // =============================================

        public async Task<(int RequisitionID, string RequisitionCode, bool IsSuccess, string Message)> CreateJobRequisitionAsync(JobRequisitionCreateRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@JobTitle", request.JobTitle);
                parameters.Add("@DepartmentID", request.DepartmentID);
                parameters.Add("@DesignationID", request.DesignationID);
                parameters.Add("@EmploymentTypeID", request.EmploymentTypeID);
                parameters.Add("@GradeID", request.GradeID);
                parameters.Add("@Vacancies", request.Vacancies);
                parameters.Add("@MinExperience", request.MinExperience);
                parameters.Add("@MaxExperience", request.MaxExperience);
                parameters.Add("@MinAge", request.MinAge);
                parameters.Add("@MaxAge", request.MaxAge);
                parameters.Add("@MinSalary", request.MinSalary);
                parameters.Add("@MaxSalary", request.MaxSalary);
                parameters.Add("@Location", request.Location);
                parameters.Add("@ReportingTo", request.ReportingTo);
                parameters.Add("@KeyResponsibilities", request.KeyResponsibilities);
                parameters.Add("@Requirements", request.Requirements);
                parameters.Add("@Qualifications", request.Qualifications);
                parameters.Add("@Skills", request.Skills);
                parameters.Add("@Benefits", request.Benefits);
                parameters.Add("@Justification", request.Justification);
                parameters.Add("@IsPublished", request.IsPublished);
                parameters.Add("@PublishedDate", request.PublishedDate);
                parameters.Add("@ClosingDate", request.ClosingDate);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@JobCategoryID", request.JobCategoryID);
                parameters.Add("@Isbudget", request.Isbudget);
                parameters.Add("@IsNonBudget", request.IsNonBudget);
                parameters.Add("@IsPublic", request.IsPublic);
                parameters.Add("@IsDefault", request.IsDefault);
                parameters.Add("@SalaryRecommendationID", request.SalaryRecommendationID);
                parameters.Add("@CreatedBy", request.CreatedBy);
                parameters.Add("@RequisitionID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@RequisitionCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                // Distinct SP from SaveAsync's ruc.SP_Ruc_JobRequisition_Create — that
                // one mirrors the legacy production form's field names
                // (RecruitmentRequisitionName, AgeText, ...); this method's DTO is the
                // newer, FK-normalized shape (DepartmentID, MinAge/MaxAge as ints,
                // etc.). They cannot share one SP name with two different parameter
                // lists, and a prior edit collided them — this call was throwing
                // "expects parameter '@NewID'" on every invocation until this fix.
                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_JobRequisition_CreateDetailed]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var requisitionID = parameters.Get<int>("@RequisitionID");
                var requisitionCode = parameters.Get<string>("@RequisitionCode") ?? "";

                return (requisitionID, requisitionCode, result == 1, result == 1 ? "Job requisition created successfully" : "Failed to create job requisition");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateJobRequisitionAsync");
                return (0, "", false, ex.Message);
            }
        }

        public async Task<JobRequisitionResponseDto?> GetJobRequisitionByIdAsync(int requisitionID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RequisitionID", requisitionID);

                var result = await _db.QueryFirstOrDefaultAsync<JobRequisitionResponseDto>(
                    "[ruc].[SP_Ruc_JobRequisition_GetById]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result != null)
                {
                    var hiringDetail = await GetHiringDetailByRequisitionIdAsync(requisitionID, result.CompanyID);
                    if (hiringDetail != null)
                    {
                        result.HiringDetail = hiringDetail;
                        result.HiringType = hiringDetail.HiringType;
                        result.ReplacedEmployeeID = hiringDetail.ReplacedEmployeeID;
                        result.ReplacementReason = hiringDetail.ReplacementReason;
                        result.LastWorkingDate = hiringDetail.LastWorkingDate;
                        result.HiringRemarks = hiringDetail.Remarks;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobRequisitionByIdAsync");
                return null;
            }
        }

        public async Task<(bool IsSuccess, string Message)> UpsertHiringDetailAsync(int requisitionID, int companyID, JobRequisitionHiringDetailDto detail, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var hiringType = string.IsNullOrWhiteSpace(detail.HiringType) ? "NEW_JOINING" : detail.HiringType.Trim().ToUpperInvariant();

                var parameters = new DynamicParameters();
                parameters.Add("@RequisitionID", requisitionID);
                parameters.Add("@CompanyID", companyID);
                parameters.Add("@HiringType", hiringType);
                parameters.Add("@ReplacedEmployeeID", hiringType == "REPLACEMENT" ? detail.ReplacedEmployeeID : null);
                parameters.Add("@ReplacementReason", hiringType == "REPLACEMENT" ? detail.ReplacementReason : null);
                parameters.Add("@LastWorkingDate", hiringType == "REPLACEMENT" ? detail.LastWorkingDate : null);
                parameters.Add("@Remarks", detail.Remarks);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_JobRequisitionHiringDetail_Upsert]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return result switch
                {
                    1 => (true, "Hiring detail saved successfully"),
                    -3 => (false, "Invalid hiring type. Use NEW_JOINING or REPLACEMENT."),
                    -4 => (false, "Replaced employee is required for Employee Replacement."),
                    _ => (false, "Failed to save hiring detail")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpsertHiringDetailAsync");
                return (false, ex.Message);
            }
        }

        public async Task<JobRequisitionHiringDetailDto?> GetHiringDetailByRequisitionIdAsync(int requisitionID, int? companyID = null)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RequisitionID", requisitionID);
                parameters.Add("@CompanyID", companyID);

                return await _db.QueryFirstOrDefaultAsync<JobRequisitionHiringDetailDto>(
                    "[ruc].[SP_Ruc_JobRequisitionHiringDetail_GetByRequisitionID]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHiringDetailByRequisitionIdAsync");
                return null;
            }
        }

        public async Task<(List<JobRequisitionResponseDto> Requisitions, int TotalCount)> GetAllJobRequisitionsAsync(JobRequisitionListRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@SearchTerm", request.SearchTerm);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@IsActive", request.IsActive ?? true);
                parameters.Add("@DepartmentID", request.DepartmentID);

                parameters.Add("@CreatedBy", request.CreatedBy);


                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Ruc_JobRequisition_GetAll]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var requisitions = (await multi.ReadAsync<JobRequisitionResponseDto>()).ToList();
                var totalCount = await multi.ReadFirstAsync<int>();

                return (requisitions, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJobRequisitionsAsync");
                return (new List<JobRequisitionResponseDto>(), 0);
            }
        }

        public async Task<(bool IsSuccess, string Message)> UpdateJobRequisitionAsync(JobRequisitionUpdateRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@JobTitle", request.JobTitle);
                parameters.Add("@DepartmentID", request.DepartmentID);
                parameters.Add("@DesignationID", request.DesignationID);
                parameters.Add("@EmploymentTypeID", request.EmploymentTypeID);
                parameters.Add("@GradeID", request.GradeID);
                parameters.Add("@Vacancies", request.Vacancies);
                parameters.Add("@MinExperience", request.MinExperience);
                parameters.Add("@MaxExperience", request.MaxExperience);
                parameters.Add("@MinAge", request.MinAge);
                parameters.Add("@MaxAge", request.MaxAge);
                parameters.Add("@MinSalary", request.MinSalary);
                parameters.Add("@MaxSalary", request.MaxSalary);
                parameters.Add("@Location", request.Location);
                parameters.Add("@ReportingTo", request.ReportingTo);
                parameters.Add("@KeyResponsibilities", request.KeyResponsibilities);
                parameters.Add("@Requirements", request.Requirements);
                parameters.Add("@Qualifications", request.Qualifications);
                parameters.Add("@Skills", request.Skills);
                parameters.Add("@Benefits", request.Benefits);
                parameters.Add("@Justification", request.Justification);
                parameters.Add("@IsPublic", request.IsPublic);
                parameters.Add("@IsPublished", request.IsPublished);
                parameters.Add("@PublishedDate", request.PublishedDate);
                parameters.Add("@ClosingDate", request.ClosingDate);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@JobCategoryID", request.JobCategoryID);
                parameters.Add("@Isbudget", request.Isbudget);
                parameters.Add("@IsNonBudget", request.IsNonBudget);
                parameters.Add("@SalaryRecommendationID", request.SalaryRecommendationID);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                // Paired with SP_Ruc_JobRequisition_CreateDetailed — see that call
                // site's comment for why this needs its own SP name rather than
                // reusing SP_Ruc_JobRequisition_Update (legacy shape, used elsewhere).
                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_JobRequisition_UpdateDetailed]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Job requisition updated successfully" : "Failed to update job requisition");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateJobRequisitionAsync");
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> DeleteJobRequisitionAsync(int requisitionID, string deletedBy, int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@RequisitionID", requisitionID);
                    parameters.Add("@DeletedBy", deletedBy);
                    parameters.Add("@CompanyID", companyID);
                    parameters.Add("@Reason", (string?)null);
                    parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await _db.ExecuteAsync(
                        "[ruc].[SP_Ruc_JobRequisition_Delete]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var result = parameters.Get<int>("@Result");
                    return result switch
                    {
                        1 => (true, "Job requisition deleted successfully"),
                        -2 => (false, "Cannot delete this job requisition because it has active applications. Please reject or remove applications first."),
                        -1 => (false, "Job requisition not found or already deleted."),
                        _ => (false, "Failed to delete job requisition.")
                    };
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 2812 || sqlEx.Message.Contains("SP_Ruc_JobRequisition_Delete", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("SP_Ruc_JobRequisition_Delete not found. Falling back to direct soft-delete SQL.");

                    var rows = await _db.ExecuteAsync(
                        @"UPDATE dbo.Tbl_Ruc_RecruitmentRequisition 
                          SET IsActive = 0, UpdatedBy = @DeletedBy, UpdatedDate = GETDATE() 
                          WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID;

                          UPDATE dbo.Tbl_Ruc_JobApplication
                          SET IsActive = 0, UpdatedBy = @DeletedBy, UpdatedDate = GETDATE()
                          WHERE RequisitionID = @RequisitionID;",
                        new { RequisitionID = requisitionID, CompanyID = companyID, DeletedBy = deletedBy }
                    );

                    return (rows > 0, rows > 0 ? "Job requisition deleted successfully" : "Requisition not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteJobRequisitionAsync");
                return (false, ex.Message);
            }
        }

        // =============================================
        // CRUD OPERATIONS - JOB APPLICATION
        // =============================================

        public async Task<(int ApplicationID, string ApplicationCode, bool IsSuccess, string Message)> CreateJobApplicationAsync(JobApplicationCreateRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
               
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@ApplicationDate", DateTime.Now);
                //parameters.Add("@ApplicationSourceID", request.ApplicationSourceID);
                parameters.Add("@CurrentStatusID", request.CurrentStatusID);
                parameters.Add("@ResumePath", request.ResumePath);
                parameters.Add("@CoverLetter", request.CoverLetter);
                parameters.Add("@Remarks", request.Remarks);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@ApplicationID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ApplicationCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_JobApplication_Create]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var applicationID = parameters.Get<int>("@ApplicationID");
                var applicationCode = parameters.Get<string>("@ApplicationCode") ?? "";

                return (applicationID, applicationCode, result == 1, result == 1 ? "Job application created successfully" : "Failed to create job application");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateJobApplicationAsync");
                return (0, "", false, ex.Message);
            }
        }

        public async Task<JobApplicationResponseDto?> GetJobApplicationByIdAsync(int applicationID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);

                var result = await _db.QueryFirstOrDefaultAsync<JobApplicationResponseDto>(
                    "[ruc].[SP_Ruc_JobApplication_GetById]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetJobApplicationByIdAsync");
                return null;
            }
        }

        public async Task<(List<JobApplicationResponseDto> Applications, int TotalCount)> GetAllJobApplicationsAsync(JobApplicationListRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@CurrentStatusID", request.CurrentStatusID);
                parameters.Add("@SearchTerm", request.SearchTerm);
                parameters.Add("@IsActive", request.IsActive ?? true);


                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Ruc_JobApplication_GetAll]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var applications = (await multi.ReadAsync<JobApplicationResponseDto>()).ToList();
                var totalCount = await multi.ReadFirstAsync<int>();

                return (applications, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllJobApplicationsAsync");
                return (new List<JobApplicationResponseDto>(), 0);
            }
        }

        public async Task<(bool IsSuccess, string Message)> UpdateApplicationStatusOnlyAsync(int applicationId,int statusId, decimal? screeningScore,decimal? overallRating, string updatedBy)
        {
            var request = new JobApplicationUpdateRequestDto
            {
                ApplicationID = applicationId,
                CurrentStatusID = statusId,
                ScreeningScore = screeningScore,
                OverallRating = overallRating

            };

            return await UpdateJobApplicationAsync(request, updatedBy);
        }
        public async Task<(bool IsSuccess, string Message)> UpdateJobApplicationAsync(JobApplicationUpdateRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@CurrentStatusID", request.CurrentStatusID);
                parameters.Add("@ResumePath", request.ResumePath);
                parameters.Add("@CoverLetter", request.CoverLetter);
                parameters.Add("@ScreeningScore", request.ScreeningScore);
                parameters.Add("@OverallRating", request.OverallRating);
                parameters.Add("@FinalRecommendation", request.FinalRecommendation);
                parameters.Add("@RejectionReason", request.RejectionReason);
                parameters.Add("@OfferLetterPath", request.OfferLetterPath);
                parameters.Add("@OfferAccepted", request.OfferAccepted);
                parameters.Add("@Remarks", request.Remarks);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_JobApplication_Update]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Job application updated successfully" : "Failed to update job application");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateJobApplicationAsync");
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> DeleteJobApplicationAsync(int applicationID, string deletedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@ApplicationID", applicationID);
                    parameters.Add("@DeletedBy", deletedBy);
                    parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await _db.ExecuteAsync(
                        "[ruc].[SP_Ruc_JobApplication_Delete]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    var result = parameters.Get<int>("@Result");
                    return (result == 1, result == 1 ? "Job application deleted successfully" : "Failed to delete job application");
                }
                catch (SqlException sqlEx) when (sqlEx.Number == 2812 || sqlEx.Message.Contains("SP_Ruc_JobApplication_Delete", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("SP_Ruc_JobApplication_Delete not found. Falling back to direct soft-delete SQL.");

                    var rows = await _db.ExecuteAsync(
                        @"UPDATE dbo.Tbl_Ruc_JobApplication 
                          SET IsActive = 0, UpdatedBy = @DeletedBy, UpdatedDate = GETDATE() 
                          WHERE ApplicationID = @ApplicationID",
                        new { ApplicationID = applicationID, DeletedBy = deletedBy }
                    );

                    return (rows > 0, rows > 0 ? "Job application deleted successfully" : "Application not found.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteJobApplicationAsync");
                return (false, ex.Message);
            }
        }

        // =============================================
        // CRUD OPERATIONS - INTERVIEW SCHEDULE
        // =============================================

        public async Task<(int ScheduleID, string ScheduleCode, bool IsSuccess, string Message)> CreateInterviewScheduleAsync(InterviewScheduleCreateRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@InterviewRound", request.InterviewRound);
                parameters.Add("@InterviewTypeID", request.InterviewTypeID);
                parameters.Add("@ScheduledDate", request.ScheduledDate);
                parameters.Add("@DurationMinutes", request.DurationMinutes);
                parameters.Add("@Venue", request.Venue);
                parameters.Add("@OnlineMeetingLink", request.OnlineMeetingLink);
                parameters.Add("@Instructions", request.Instructions);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@ScheduleID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ScheduleCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_InterviewSchedule_Create]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var scheduleID = parameters.Get<int>("@ScheduleID");
                var scheduleCode = parameters.Get<string>("@ScheduleCode") ?? "";

                return (scheduleID, scheduleCode, result == 1, result == 1 ? "Interview schedule created successfully" : "Failed to create interview schedule");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateInterviewScheduleAsync");
                return (0, "", false, ex.Message);
            }
        }

        public async Task<bool> MarkInterviewAsNotifiedAsync(int scheduleId, string updatedBy)
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@ScheduleID", scheduleId);
            parameters.Add("@UpdatedBy", updatedBy);

            var result = await _db.ExecuteAsync(
                "[ruc].[SP_Ruc_InterviewSchedule_MarkAsNotified]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result > 0;
        }
        public async Task<InterviewScheduleResponseDto?> GetInterviewScheduleByIdAsync(int scheduleID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", scheduleID);

                // 1️⃣ Get Schedule
                var schedule = await _db.QueryFirstOrDefaultAsync<InterviewScheduleResponseDto>(
                    "[ruc].[SP_Ruc_InterviewSchedule_GetById]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (schedule == null)
                    return null;

                // 2️⃣ Get Panel Members
                //var panelMembers = await _db.QueryAsync<InterviewPanelDto>(@"
                //    SELECT p.PanelID, p.InterviewerID, p.IsPanelHead,
                //           emp.FirstName + ' ' + emp.LastName AS InterviewerName,
                //           emp.PersonalEmail AS InterviewerEmail
                //    FROM ruc.Tbl_InterviewPanel p
                //    INNER JOIN Tbl_Hr_Employee emp ON p.InterviewerID = emp.EmployeeID
                //    WHERE p.ScheduleID = @ScheduleID AND p.IsActive = 1 
                //", new { ScheduleID = scheduleID });
                var panelMembers = await _db.QueryAsync<InterviewPanelDto>(@"
                    SELECT p.PanelID, p.InterviewerID, p.IsPanelHead,
                           emp.FirstName + ' ' + emp.LastName AS InterviewerName,
                           emp.WorkEmail AS InterviewerEmail
                    FROM ruc.Tbl_InterviewPanel p
                    INNER JOIN Tbl_Hr_Employee emp ON p.InterviewerID = emp.EmployeeID
                    WHERE p.ScheduleID = @ScheduleID AND p.IsActive = 1 
                ", new { ScheduleID = scheduleID });
                //AND p.IsPanelHead = 1
                schedule.PanelMembers = panelMembers.ToList();

                return schedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewScheduleByIdAsync");
                return null;
            }
        }

        public async Task<(List<InterviewScheduleResponseDto> Schedules, int TotalCount)> GetAllInterviewSchedulesAsync(InterviewScheduleListRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@FromDate", request.FromDate);
                parameters.Add("@ToDate", request.ToDate);
                parameters.Add("@IsActive", request.IsActive);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Ruc_InterviewSchedule_GetAll]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var schedules = (await multi.ReadAsync<InterviewScheduleResponseDto>()).ToList();

                var panels = (await multi.ReadAsync<InterviewPanelDto>()).ToList();

                var panelLookup = panels.GroupBy(p => p.ScheduleID).ToDictionary(g => g.Key, g => g.ToList());

                foreach (var schedule in schedules)
                {
                    if (panelLookup.ContainsKey(schedule.ScheduleID))
                        schedule.PanelMembers = panelLookup[schedule.ScheduleID];
                }

                var totalCount = await multi.ReadFirstAsync<int>();

                return (schedules, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllInterviewSchedulesAsync");
                return (new List<InterviewScheduleResponseDto>(), 0);
            }
        }


        public async Task<(bool IsSuccess, string Message)> UpdateInterviewScheduleAsync(InterviewScheduleUpdateRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", request.ScheduleID);
                parameters.Add("@InterviewRound", request.InterviewRound);
                parameters.Add("@InterviewTypeID", request.InterviewTypeID);
                parameters.Add("@ScheduledDate", request.ScheduledDate);
                parameters.Add("@DurationMinutes", request.DurationMinutes);
                parameters.Add("@Venue", request.Venue);
                parameters.Add("@OnlineMeetingLink", request.OnlineMeetingLink);
                parameters.Add("@Instructions", request.Instructions);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@IsNotified", request.IsNotified);
                parameters.Add("@NotificationSentOn", request.NotificationSentOn);
                parameters.Add("@FeedbackSummary", request.FeedbackSummary);
                parameters.Add("@IsActive", request.IsActive);
                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_InterviewSchedule_Update]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Interview schedule updated successfully" : "Failed to update interview schedule");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateInterviewScheduleAsync");
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> DeleteInterviewScheduleAsync(int scheduleID, string deletedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", scheduleID);
                parameters.Add("@DeletedBy", deletedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Ruc_InterviewSchedule_Delete]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                return (result == 1, result == 1 ? "Interview schedule deleted successfully" : "Failed to delete interview schedule");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteInterviewScheduleAsync");
                return (false, ex.Message);
            }
        }

        // =============================================
        // STATUS MANAGEMENT
        // =============================================

        public async Task<List<StatusResponseDto>> GetAllStatusesAsync(string? statusTypeCode = null, bool isActive = true)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@StatusTypeCode", statusTypeCode);
                parameters.Add("@IsActive", isActive);

                var result = await _db.QueryAsync<StatusResponseDto>(
                    "[ruc].[SP_Ruc_Status_GetAll]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllStatusesAsync");
                return new List<StatusResponseDto>();
            }
        }

        public async Task<List<StatusTypeResponseDto>> GetAllStatusTypesAsync(bool isActive = true)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@IsActive", isActive);

                var result = await _db.QueryAsync<StatusTypeResponseDto>(
                    "[ruc].[SP_Ruc_StatusType_GetAll]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllStatusTypesAsync");
                return new List<StatusTypeResponseDto>();
            }
        }

        // =============================================
        // WORKFLOW OPERATIONS
        // =============================================

        public async Task<(int NewStatusID, string NewStatusCode, bool IsSuccess, string Message)> ManualShortlistAsync(ManualShortlistRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@Remarks", request.Remarks);
                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ManualShortlist]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var newStatusID = parameters.Get<int>("@NewStatusID");
                var newStatusCode = parameters.Get<string>("@NewStatusCode") ?? "";

                return (newStatusID, newStatusCode, result == 1, result == 1 ? "Candidate shortlisted successfully" : "Failed to shortlist candidate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualShortlistAsync");
                return (0, "", false, ex.Message);
            }
        }

        public async Task<(int PanelCount, bool IsSuccess, string Message)> AssignPanelMembersAsync(AssignPanelMembersRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Convert panel members list to JSON
                var panelMembersJson = System.Text.Json.JsonSerializer.Serialize(request.PanelMembers);

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", request.ScheduleID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);   // 🔥 ADD THIS
                parameters.Add("@PanelMembers", panelMembersJson);
                parameters.Add("@CreatedBy", createdBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PanelCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_AssignPanelMembers]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var panelCount = parameters.Get<int>("@PanelCount");

                return (panelCount, result == 1, result == 1 ? $"Assigned {panelCount} panel members successfully" : "Failed to assign panel members");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AssignPanelMembersAsync");
                return (0, false, ex.Message);
            }
        }

        public async Task<(int EvaluationID, bool IsSuccess, string Message)> SubmitEvaluationAsync(SubmitEvaluationRequestDto request, string createdBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", request.JobApplicationID);
                parameters.Add("@ScheduleID", request.ScheduleHeaderId);
                parameters.Add("@EvaluatorID", request.InterviewerID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@OverallRating", request.OverallRatingID);
                parameters.Add("@Recommendation", request.RecommendationID.ToString());
                parameters.Add("@Comments", request.Comments);
                parameters.Add("@CreatedBy", createdBy ?? request.CreatedBy);
                parameters.Add("@EvaluationID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_SubmitEvaluation]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var evaluationID = parameters.Get<int>("@EvaluationID");

                return (evaluationID, result == 1, result == 1 ? "Evaluation submitted successfully" : "Failed to submit evaluation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitEvaluationAsync");
                return (0, false, ex.Message);
            }
        }

        public async Task<(int NewStatusID, string NewStatusCode, bool IsSuccess, string Message)> MarkAsHiredAsync(MarkAsHiredRequestDto request, string updatedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@OfferAccepted", request.OfferAccepted);
                parameters.Add("@OfferLetterPath", request.OfferLetterPath);
                parameters.Add("@Remarks", request.Remarks);

                parameters.Add("@OfferLetterBit", request.OfferLetterBit);
                parameters.Add("@OfferLetterEmailSendBit", request.OfferLetterEmailSendBit);
                parameters.Add("@JoiningDate", request.JoiningDate);
                parameters.Add("@OfferDate", request.OfferDate);
                parameters.Add("@DepartmentID", request.DepartmentID);
                parameters.Add("@DesignationID", request.DesignationID);
                parameters.Add("@Amount", request.Amount);

                parameters.Add("@UpdatedBy", updatedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_MarkAsHired]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var newStatusID = parameters.Get<int>("@NewStatusID");
                var newStatusCode = parameters.Get<string>("@NewStatusCode") ?? "";

                return (newStatusID, newStatusCode, result == 1, result == 1 ? "Candidate marked as hired successfully" : "Failed to mark candidate as hired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkAsHiredAsync");
                return (0, "", false, ex.Message);
            }
        }

        // =============================================
        // MANUAL PROCESSING
        // =============================================

        public async Task<ManualProcessResponseDto> ManualProcessApplicationAsync(ManualProcessRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@ResumeFilePath", request.ResumePath);
                parameters.Add("@ResumeFileName", request.ResumeFileName);
                parameters.Add("@EnableManualParsing", request.EnableManualParsing);
                parameters.Add("@EnableManualScreening", request.EnableManualScreening);
                parameters.Add("@ManualScreeningScore", request.ManualScreeningScore != null ? (int)request.ManualScreeningScore : (int?)null);
                parameters.Add("@ManualRecommendation", request.ManualRecommendation);
                parameters.Add("@ParsedData", request.ParsedData != null ? JsonConvert.SerializeObject(request.ParsedData) : null);
                parameters.Add("@ParsedResumeText", null);
                parameters.Add("@ProcessedBy", request.ProcessedBy ?? "system");
                parameters.Add("@ResumeParsingID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@ScreeningID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ManualProcessApplication]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var resumeParsingID = parameters.Get<int?>("@ResumeParsingID");
                var screeningID = parameters.Get<int?>("@ScreeningID");

                return new ManualProcessResponseDto
                {
                    ApplicationID = request.ApplicationID,
                    ResumeParsed = resumeParsingID.HasValue,
                    ResumeParsingID = resumeParsingID,
                    ParsedData = request.ParsedData,
                    ManuallyScreened = screeningID.HasValue,
                    ScreeningID = screeningID,
                    ManualScreeningScore = request.ManualScreeningScore,
                    MatchScore = request.ManualScreeningScore,
                    Recommendation = request.ManualRecommendation,
                    ProcessingMethod = "MANUAL",
                    ProcessedBy = request.ProcessedBy,
                    ProcessedOn = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualProcessApplicationAsync");
                throw;
            }
        }

        public async Task<ManualParseResumeResponseDto> ManualParseResumeAsync(ManualParseResumeRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@ResumeFilePath", request.ResumePath);
                parameters.Add("@ResumeFileName", request.ResumeFileName);
                parameters.Add("@ParsedData", JsonConvert.SerializeObject(request.ParsedData));
                parameters.Add("@ParsedResumeText", null);
                parameters.Add("@ParsedBy", request.ParsedBy ?? "system");
                parameters.Add("@ParsingID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ManualParseResume]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var parsingID = parameters.Get<int>("@ParsingID");

                return new ManualParseResumeResponseDto
                {
                    ParsingID = parsingID,
                    ParsedData = request.ParsedData,
                    ParsingMethod = "MANUAL",
                    ParsedBy = request.ParsedBy,
                    ParsedOn = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualParseResumeAsync");
                throw;
            }
        }

        public async Task<ManualScreenResumeResponseDto> ManualScreenResumeAsync(ManualScreenResumeRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", request.ApplicationID);
                parameters.Add("@ApplicantID", request.ApplicantID);
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@ResumeParsingID", request.ResumeParsingID);
                parameters.Add("@MatchScore", request.MatchScore != null ? (int)request.MatchScore : 0);
                parameters.Add("@Recommendation", request.Recommendation);
                parameters.Add("@SkillsMatch", request.SkillsMatch != null ? JsonConvert.SerializeObject(request.SkillsMatch) : null);
                parameters.Add("@ExperienceMatch", request.ExperienceMatch != null ? JsonConvert.SerializeObject(request.ExperienceMatch) : null);
                parameters.Add("@QualificationsMatch", request.QualificationsMatch != null ? JsonConvert.SerializeObject(request.QualificationsMatch) : null);
                parameters.Add("@RedFlags", request.RedFlags != null ? JsonConvert.SerializeObject(request.RedFlags) : null);
                parameters.Add("@ScreenedBy", request.ScreenedBy ?? "system");
                parameters.Add("@ScreeningID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ManualScreenResume]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var screeningID = parameters.Get<int>("@ScreeningID");

                return new ManualScreenResumeResponseDto
                {
                    ScreeningID = screeningID,
                    MatchScore = request.MatchScore,
                    Recommendation = request.Recommendation,
                    SkillsMatch = request.SkillsMatch,
                    ExperienceMatch = request.ExperienceMatch,
                    QualificationsMatch = request.QualificationsMatch,
                    Strengths = request.Strengths,
                    Weaknesses = request.Weaknesses,
                    ScreeningMethod = "MANUAL",
                    ScreenedBy = request.ScreenedBy,
                    ScreenedOn = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ManualScreenResumeAsync");
                throw;
            }
        }

        // =============================================
        // WORKFLOW ACTIONS
        // =============================================

        public async Task<ShortlistCandidateResponseDto> ShortlistCandidateAsync(int applicationID, ShortlistCandidateRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ShortlistedBy", request.ShortlistedBy ?? "system");
                parameters.Add("@Remarks", request.Remarks);
                parameters.Add("@PreviousStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PreviousStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ShortlistCandidate]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var previousStatusID = parameters.Get<int>("@PreviousStatusID");
                var previousStatusCode = parameters.Get<string>("@PreviousStatusCode");
                var newStatusID = parameters.Get<int>("@NewStatusID");
                var newStatusCode = parameters.Get<string>("@NewStatusCode");

                // Get status name
                var statusName = await _db.QueryFirstOrDefaultAsync<string>(
                    "SELECT StatusName FROM [ruc].[Tbl_Status] WHERE StatusID = @StatusID",
                    new { StatusID = newStatusID }
                );

                return new ShortlistCandidateResponseDto
                {
                    ApplicationID = applicationID,
                    PreviousStatusID = previousStatusID,
                    PreviousStatusCode = previousStatusCode,
                    NewStatusID = newStatusID,
                    NewStatusCode = newStatusCode,
                    NewStatusName = statusName,
                    IsShortlisted = true,
                    ShortlistDate = DateTime.UtcNow,
                    ShortlistedBy = request.ShortlistedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ShortlistCandidateAsync");
                throw;
            }
        }

        public async Task<RejectApplicationResponseDto> RejectApplicationAsync(int applicationID, RejectApplicationRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@RejectionReason", request.RejectionReason);
                parameters.Add("@RejectedBy", request.RejectedBy ?? "system");
                parameters.Add("@PreviousStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PreviousStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_RejectApplication]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var previousStatusID = parameters.Get<int>("@PreviousStatusID");
                var previousStatusCode = parameters.Get<string>("@PreviousStatusCode");
                var newStatusID = parameters.Get<int>("@NewStatusID");
                var newStatusCode = parameters.Get<string>("@NewStatusCode");

                // Get status name
                var statusName = await _db.QueryFirstOrDefaultAsync<string>(
                    "SELECT StatusName FROM [ruc].[Tbl_Status] WHERE StatusID = @StatusID",
                    new { StatusID = newStatusID }
                );

                return new RejectApplicationResponseDto
                {
                    ApplicationID = applicationID,
                    PreviousStatusID = previousStatusID,
                    PreviousStatusCode = previousStatusCode,
                    NewStatusID = newStatusID,
                    NewStatusCode = newStatusCode,
                    NewStatusName = statusName,
                    IsRejected = true,
                    RejectionReason = request.RejectionReason,
                    RejectedDate = DateTime.UtcNow,
                    RejectedBy = request.RejectedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectApplicationAsync");
                throw;
            }
        }

        public async Task<HireCandidateResponseDto> HireCandidateAsync(int applicationID, HireCandidateRequestDto request)
        {
            // Use existing MarkAsHiredAsync
            var markAsHiredRequest = new MarkAsHiredRequestDto
            {
                ApplicationID = applicationID,
                CompanyID = request.CompanyID,
                OfferAccepted = request.OfferAccepted ?? true,
                OfferLetterPath = request.OfferLetterPath,
                Remarks = request.Remarks,
                OfferLetterEmailSendBit = request.OfferLetterEmailSendBit,
                OfferLetterBit = request.OfferLetterBit,
                JoiningDate = request.JoiningDate,
                OfferDate = request.OfferDate,
                DepartmentID = request.DepartmentID,
                DesignationID = request.DesignationID,
                Amount = request.Amount
            };
            var result = await MarkAsHiredAsync(markAsHiredRequest, request.HiredBy ?? "system");
            return new HireCandidateResponseDto
            {
                ApplicationID = applicationID,
                NewStatusID = result.NewStatusID,
                NewStatusCode = result.NewStatusCode,
                IsHired = true,
                OfferLetterPath = request.OfferLetterPath,
                OfferAccepted = request.OfferAccepted,
                HiredDate = DateTime.UtcNow,
                HiredBy = request.HiredBy
            };
        }

        public async Task<PublishRequisitionResponseDto> PublishRequisitionAsync(int requisitionID, PublishRequisitionRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RequisitionID", requisitionID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@PublishedBy", request.PublishedBy ?? "system");
                parameters.Add("@IsPublished", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@PublishedDate", dbType: DbType.DateTime, direction: ParameterDirection.Output);
                parameters.Add("@StatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@StatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_PublishRequisition]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var isPublished = parameters.Get<bool>("@IsPublished");
                var publishedDate = parameters.Get<DateTime?>("@PublishedDate");
                var statusID = parameters.Get<int>("@StatusID");
                var statusCode = parameters.Get<string>("@StatusCode");

                // Get status name
                var statusName = await _db.QueryFirstOrDefaultAsync<string>(
                    "SELECT StatusName FROM [ruc].[Tbl_Status] WHERE StatusID = @StatusID",
                    new { StatusID = statusID }
                );

                return new PublishRequisitionResponseDto
                {
                    RequisitionID = requisitionID,
                    IsPublished = isPublished,
                    PublishedDate = publishedDate,
                    PublishedBy = request.PublishedBy,
                    StatusID = statusID,
                    StatusCode = statusCode,
                    StatusName = statusName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PublishRequisitionAsync");
                throw;
            }
        }

        public async Task<List<JobRequisitionResponseDto>> GetPublicRequisitionsAsync(int companyID, string? searchText, int? departmentID, string? location)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);
                parameters.Add("@SearchText", searchText);
                parameters.Add("@DepartmentID", departmentID);
                parameters.Add("@Location", location);

                var result = await _db.QueryAsync<JobRequisitionResponseDto>(
                    "[ruc].[SP_Recruitment_GetPublicRequisitions]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPublicRequisitionsAsync");
                return new List<JobRequisitionResponseDto>();
            }
        }

        public async Task<UpdateApplicationStatusResponseDto> UpdateApplicationStatusAsync(int applicationID, UpdateApplicationStatusRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@StatusID", request.StatusID);
                parameters.Add("@Remarks", request.Remarks);
                parameters.Add("@UpdatedBy", request.UpdatedBy ?? "system");
                parameters.Add("@PreviousStatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@PreviousStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@NewStatusName", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_UpdateApplicationStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var previousStatusID = parameters.Get<int>("@PreviousStatusID");
                var previousStatusCode = parameters.Get<string>("@PreviousStatusCode");
                var newStatusCode = parameters.Get<string>("@NewStatusCode");
                var newStatusName = parameters.Get<string>("@NewStatusName");

                return new UpdateApplicationStatusResponseDto
                {
                    ApplicationID = applicationID,
                    PreviousStatusID = previousStatusID,
                    PreviousStatusCode = previousStatusCode,
                    NewStatusID = request.StatusID,
                    NewStatusCode = newStatusCode,
                    NewStatusName = newStatusName,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = request.UpdatedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApplicationStatusAsync");
                throw;
            }
        }

        public async Task<CancelInterviewScheduleResponseDto> CancelInterviewScheduleAsync(int scheduleID, CancelInterviewScheduleRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", scheduleID);
                parameters.Add("@Reason", request.Reason);
                parameters.Add("@CancelledBy", request.CancelledBy ?? "system");
                parameters.Add("@StatusID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@StatusCode", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@StatusName", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_CancelInterviewSchedule]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var statusID = parameters.Get<int>("@StatusID");
                var statusCode = parameters.Get<string>("@StatusCode");
                var statusName = parameters.Get<string>("@StatusName");

                return new CancelInterviewScheduleResponseDto
                {
                    ScheduleID = scheduleID,
                    StatusID = statusID,
                    StatusCode = statusCode,
                    StatusName = statusName,
                    CancelledDate = DateTime.UtcNow,
                    CancelledBy = request.CancelledBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelInterviewScheduleAsync");
                throw;
            }
        }

        // =============================================
        // EVALUATION
        // =============================================

        public async Task<List<EvaluationCriteriaDto>> GetEvaluationCriteriaAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                var result = await _db.QueryAsync<EvaluationCriteriaDto>(
                    "[ruc].[SP_Recruitment_GetEvaluationCriteria]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationCriteriaAsync");
                return new List<EvaluationCriteriaDto>();
            }
        }

        public async Task<List<RatingScaleDto>> GetRatingScalesAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                var result = await _db.QueryAsync<RatingScaleDto>(
                    "[ruc].[SP_Recruitment_GetRatingScales]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRatingScalesAsync");
                return new List<RatingScaleDto>();
            }
        }

        public async Task<SubmitEvaluationResponseDto> SubmitEvaluationAsync(SubmitEvaluationRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                // Convert API DTO to SP DTO (matching SP parameters exactly)
                // Get OverallRating decimal value from RatingScale table
                decimal overallRating = 0;
                var ratingScale = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT RatingValue FROM [ruc].[Tbl_RatingScale] 
                      WHERE RatingScaleID = @RatingScaleID AND IsActive = 1",
                    new { RatingScaleID = request.OverallRatingID }
                );
                if (ratingScale != null && ratingScale.RatingValue != null)
                {
                    overallRating = (decimal)ratingScale.RatingValue;
                }
                else
                {
                    // Fallback: use OverallRatingID as decimal if RatingScale not found
                    overallRating = request.OverallRatingID;
                }
                
                // Get Recommendation string from Status table
                string recommendation = "PASS"; // Default
                var recommendationStatus = await _db.QueryFirstOrDefaultAsync<dynamic>(
                    @"SELECT StatusCode FROM [ruc].[Tbl_Status] 
                      WHERE StatusID = @StatusID AND IsActive = 1",
                    new { StatusID = request.RecommendationID }
                );
                if (recommendationStatus != null && recommendationStatus.StatusCode != null)
                {
                    recommendation = recommendationStatus.StatusCode.ToString().ToUpper();
                    // Map common status codes to SP expected values
                    recommendation = recommendation switch
                    {
                        "PASS" or "PASSED" => "PASS",
                        "FAIL" or "FAILED" => "FAIL",
                        "CONDITIONAL" => "CONDITIONAL",
                        "STRONG_PASS" => "STRONG_PASS",
                        _ => recommendation
                    };
                }

                // Create SP request DTO matching SP parameters exactly
                var spRequest = new SubmitEvaluationSPRequestDto
                {
                    ApplicationID = request.JobApplicationID,
                    ScheduleID = request.ScheduleHeaderId,
                    RecommendationID = request.RecommendationID,
                    EvaluatorID = request.InterviewerID,
                    CompanyID = request.CompanyID,
                    OverallRating = overallRating,
                    Recommendation = recommendation,
                    Comments = request.Comments,
                    CreatedBy = request.CreatedBy ?? "system"
                };

                // Call SP directly with SP DTO
                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", spRequest.ApplicationID);
                parameters.Add("@ScheduleID", spRequest.ScheduleID);
                parameters.Add("@EvaluatorID", spRequest.EvaluatorID);
                parameters.Add("@CompanyID", spRequest.CompanyID);
                parameters.Add("@OverallRating", spRequest.OverallRating);
                parameters.Add("@RecommendationID", spRequest.RecommendationID);
                parameters.Add("@Recommendation", spRequest.Recommendation);
                parameters.Add("@Comments", spRequest.Comments);
                parameters.Add("@CreatedBy", spRequest.CreatedBy);
                parameters.Add("@EvaluationID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_SubmitEvaluation]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var evaluationID = parameters.Get<int>("@EvaluationID");
                
                if (result != 1)
                    throw new Exception("Failed to submit evaluation");

                // Insert criteria scores (using Tbl_EvaluationScore with Score int)
                if (request.CriteriaScores != null && request.CriteriaScores.Any())
                {
                    foreach (var criteriaScore in request.CriteriaScores)
                    {
                        await _db.ExecuteAsync(
                            @"INSERT INTO [ruc].[Tbl_EvaluationScore] 
                              (EvaluationID, CriteriaID, Score, Comments)
                              VALUES (@EvaluationID, @CriteriaID, @Score, @Comments)",
                            new
                            {
                                EvaluationID = evaluationID,
                                CriteriaID = criteriaScore.CriteriaID,
                                Score = criteriaScore.RatingScaleID, // Using RatingScaleID as Score value
                                Comments = (string?)null
                            }
                        );
                    }
                }

                return new SubmitEvaluationResponseDto
                {
                    EvaluationID = evaluationID,
                    JobApplicationID = request.JobApplicationID,
                    ScheduleHeaderId = request.ScheduleHeaderId,
                    InterviewerID = request.InterviewerID,
                    InterviewRound = request.InterviewRound,
                    EvaluationDate = request.EvaluationDate,
                    OverallRatingID = request.OverallRatingID,
                    RecommendationID = request.RecommendationID,
                    EvaluationScore = request.EvaluationScore,
                    CreatedOn = DateTime.UtcNow,
                    CreatedBy = request.CreatedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitEvaluationAsync");
                throw;
            }
        }

        public async Task<List<EvaluationDto>> GetEvaluationsByScheduleAsync(int scheduleID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", scheduleID);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Recruitment_GetEvaluationsBySchedule]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var evaluations = (await multi.ReadAsync<dynamic>()).ToList();
                var criteriaScores = (await multi.ReadAsync<dynamic>()).ToList();

                // Map to EvaluationDto
                var evaluationDtos = new List<EvaluationDto>();
                foreach (var eval in evaluations)
                {
                    var evalDto = new EvaluationDto
                    {
                        EvaluationID = eval.EvaluationID,
                        ScheduleID = eval.ScheduleID,
                        InterviewerID = eval.InterviewerID,
                        InterviewerName = eval.InterviewerName,
                        InterviewRound = eval.InterviewRound ?? 0,
                        ScheduledDate = eval.ScheduledDate,
                        OverallRating = eval.OverallRating,
                        Recommendation = eval.Recommendation,
                        Strengths = eval.Strengths,
                        Weaknesses = eval.Weaknesses,
                        Comments = eval.Comments,
                        IsSubmitted = eval.IsSubmitted ?? false,
                        SubmittedOn = eval.SubmittedOn,
                        CreatedOn = eval.CreatedOn
                    };

                    // Map criteria scores
                    evalDto.CriteriaScores = criteriaScores
                        .Where(cs => cs.EvaluationID == eval.EvaluationID)
                        .Select(cs => new EvaluationCriteriaScoreDetailDto
                        {
                            EvaluationID = cs.EvaluationID,
                            CriteriaID = cs.CriteriaID,
                            CriteriaTitle = cs.CriteriaTitle,
                            Score = cs.Score,
                            Comments = cs.Comments
                        })
                        .ToList();

                    evaluationDtos.Add(evalDto);
                }

                return evaluationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationsByScheduleAsync");
                return new List<EvaluationDto>();
            }
        }

        public async Task<List<EvaluationDto>> GetEvaluationsByApplicationAsync(int applicationID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Recruitment_GetEvaluationsByApplication]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var evaluations = (await multi.ReadAsync<dynamic>()).ToList();
                var criteriaScores = (await multi.ReadAsync<dynamic>()).ToList();

                // Map to EvaluationDto
                var evaluationDtos = new List<EvaluationDto>();
                foreach (var eval in evaluations)
                {
                    var evalDto = new EvaluationDto
                    {
                        EvaluationID = eval.EvaluationID,
                        ScheduleID = eval.ScheduleID,
                        InterviewerID = eval.InterviewerID,
                        InterviewerName = eval.InterviewerName,
                        InterviewRound = eval.InterviewRound ?? 0,
                        ScheduledDate = eval.ScheduledDate,
                        OverallRating = eval.OverallRating,
                        Recommendation = eval.Recommendation,
                        Strengths = eval.Strengths,
                        Weaknesses = eval.Weaknesses,
                        Comments = eval.Comments,
                        IsSubmitted = eval.IsSubmitted ?? false,
                        SubmittedOn = eval.SubmittedOn,
                        CreatedOn = eval.CreatedOn
                    };

                    // Map criteria scores
                    evalDto.CriteriaScores = criteriaScores
                        .Where(cs => cs.EvaluationID == eval.EvaluationID)
                        .Select(cs => new EvaluationCriteriaScoreDetailDto
                        {
                            EvaluationID = cs.EvaluationID,
                            CriteriaID = cs.CriteriaID,
                            CriteriaTitle = cs.CriteriaTitle,
                            Score = cs.Score,
                            Comments = cs.Comments
                        })
                        .ToList();

                    evaluationDtos.Add(evalDto);
                }

                return evaluationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationsByApplicationAsync");
                return new List<EvaluationDto>();
            }
        }

        // =============================================
        // MASTER DATA
        // =============================================

        public async Task<List<ApplicationSourceDto>> GetApplicationSourcesAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                var result = await _db.QueryAsync<ApplicationSourceDto>(
                    "[ruc].[SP_Recruitment_GetApplicationSources]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApplicationSourcesAsync");
                return new List<ApplicationSourceDto>();
            }
        }

        public async Task<List<InterviewTypeDto>> GetInterviewTypesAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                var result = await _db.QueryAsync<InterviewTypeDto>(
                    "[ruc].[SP_Recruitment_GetInterviewTypes]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInterviewTypesAsync");
                return new List<InterviewTypeDto>();
            }
        }

        public async Task<List<VenueDto>> GetVenuesAsync(int companyID)
        {
            // Use existing GetVenueAsync and map to VenueDto
            var venues = await GetVenueAsync();
            return venues.Select(v => new VenueDto
            {
                VenueID = v.ApplicationStatusID,
                VenueName = v.StatusName ?? string.Empty,
                VenueAddress = null,
                IsActive = true
            }).ToList();
        }

        public async Task<List<NotificationMethodDto>> GetNotificationMethodsAsync(int companyID)
        {
            // Use existing GetNotificationMethodAsync and map to NotificationMethodDto
            var methods = await GetNotificationMethodAsync();
            return methods.Select(m => new NotificationMethodDto
            {
                NotificationMethodID = m.ApplicationStatusID,
                NotificationMethodName = m.StatusName ?? string.Empty,
                NotificationMethodCode = m.StatusCode ?? string.Empty,
                IsActive = true
            }).ToList();
        }

        // =============================================
        // STATUS MANAGEMENT
        // =============================================

        public async Task<List<StatusResponseDto>> GetStatusesByTypeAsync(string statusTypeCode, int companyID)
        {
            var statuses = await GetAllStatusesAsync(statusTypeCode, true);
            return statuses;
        }

        // =============================================
        // DASHBOARD
        // =============================================

        public async Task<DashboardResponseDto> GetDashboardStatisticsAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Recruitment_GetDashboardStatistics]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var stats = await multi.ReadFirstOrDefaultAsync<DashboardStatsDto>();
                var recentActivity = (await multi.ReadAsync<RecentActivityDto>()).ToList();

                return new DashboardResponseDto
                {
                    Stats = stats ?? new DashboardStatsDto(),
                    RecentActivity = recentActivity
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardStatisticsAsync");
                return new DashboardResponseDto
                {
                    Stats = new DashboardStatsDto(),
                    RecentActivity = new List<RecentActivityDto>()
                };
            }
        }

        // =============================================
        // PANEL MEMBER EVALUATION
        // =============================================

        public async Task<PanelMemberScheduleListResponseDto> GetPanelMemberSchedulesAsync(int interviewerID, int companyID, int? statusID, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@InterviewerID", interviewerID);
                parameters.Add("@CompanyID", companyID);
                parameters.Add("@StatusID", statusID);
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);

                var schedules = await _db.QueryAsync<PanelMemberScheduleDto>(
                    "[ruc].[SP_Recruitment_GetPanelMemberSchedules]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var schedulesList = schedules.ToList();

                return new PanelMemberScheduleListResponseDto
                {
                    Schedules = schedulesList,
                    TotalRecords = schedulesList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPanelMemberSchedulesAsync");
                return new PanelMemberScheduleListResponseDto
                {
                    Schedules = new List<PanelMemberScheduleDto>(),
                    TotalRecords = 0
                };
            }
        }

        public async Task<PanelEvaluationResponseDto?> GetPanelEvaluationAsync(int scheduleID, int interviewerID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", scheduleID);
                parameters.Add("@InterviewerID", interviewerID);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Recruitment_GetPanelEvaluation]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var evaluation = await multi.ReadFirstOrDefaultAsync<PanelEvaluationResponseDto>();
                if (evaluation == null)
                    return null;

                var criteriaRatings = (await multi.ReadAsync<CriteriaRatingDetailDto>()).ToList();
                evaluation.CriteriaRatings = criteriaRatings;

                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPanelEvaluationAsync");
                return null;
            }
        }

        public async Task<(int EvaluationID, bool IsSuccess, string Message)> SavePanelEvaluationAsync(PanelEvaluationRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@ScheduleID", request.ScheduleID);
                parameters.Add("@InterviewerID", request.InterviewerID);
                parameters.Add("@RecommendationID", request.RecommendationID);
                parameters.Add("@Comments", request.Comments);
                parameters.Add("@OverallRating", request.OverallRating);
                parameters.Add("@CriteriaRatings", JsonConvert.SerializeObject(request.CriteriaRatings));
                parameters.Add("@CreatedBy", request.CreatedBy);
                parameters.Add("@EvaluationID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_SavePanelEvaluation]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var evaluationID = parameters.Get<int>("@EvaluationID");
                var result = parameters.Get<int>("@Result");
                var message = parameters.Get<string>("@Message") ?? "Evaluation saved successfully";

                return (evaluationID, result == 1, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SavePanelEvaluationAsync");
                return (0, false, $"Error saving evaluation: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string Message)> ConfirmPanelAttendanceAsync(int panelID, string confirmedBy)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@PanelID", panelID);
                parameters.Add("@ConfirmedBy", confirmedBy);
                parameters.Add("@Result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[ruc].[SP_Recruitment_ConfirmPanelAttendance]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var result = parameters.Get<int>("@Result");
                var message = parameters.Get<string>("@Message") ?? "Attendance confirmed successfully";

                return (result == 1, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmPanelAttendanceAsync");
                return (false, $"Error confirming attendance: {ex.Message}");
            }
        }

        public async Task<List<RecommendationDto>> GetRecommendationsAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                var recommendations = await _db.QueryAsync<RecommendationDto>(
                    "[ruc].[SP_Recruitment_GetRecommendations]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return recommendations.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetRecommendationsAsync");
                return new List<RecommendationDto>();
            }
        }

        public async Task<List<EvaluationCriteriaWithRatingsDto>> GetEvaluationCriteriaWithRatingsAsync(int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);

                var criteriaDict = new Dictionary<int, EvaluationCriteriaWithRatingsDto>();

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Recruitment_GetEvaluationCriteriaWithRatings]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var criteria = await multi.ReadAsync<EvaluationCriteriaDto>();
                var ratings = await multi.ReadAsync<RatingScaleDto>();

                foreach (var criterion in criteria)
                {
                    if (!criteriaDict.ContainsKey(criterion.CriteriaID))
                    {
                        criteriaDict[criterion.CriteriaID] = new EvaluationCriteriaWithRatingsDto
                        {
                            CriteriaID = criterion.CriteriaID,
                            CriteriaCode = criterion.CriteriaCode,
                            CriteriaTitle = criterion.CriteriaTitle,
                            Description = criterion.Description,
                            Ratings = new List<RatingScaleDto>()
                        };
                    }
                }

                var ratingsList = ratings.ToList();
                foreach (var rating in ratingsList)
                {
                    // Add rating to all criteria (assuming same rating scale for all)
                    foreach (var criterion in criteriaDict.Values)
                    {
                        criterion.Ratings.Add(rating);
                    }
                }

                return criteriaDict.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationCriteriaWithRatingsAsync");
                return new List<EvaluationCriteriaWithRatingsDto>();
            }
        }


        //public async Task<PanelMemberScheduleListResponseDto> GetConfirmedHeadSchedulesAsync(int companyID, int? statusID, DateTime? startDate, DateTime? endDate)
        //{
        //    try
        //    {
        //        if (_db.State != ConnectionState.Open)
        //            _db.Open();

        //        var parameters = new DynamicParameters();
        //        parameters.Add("@CompanyID", companyID);
        //        parameters.Add("@StatusID", statusID);
        //        parameters.Add("@StartDate", startDate);
        //        parameters.Add("@EndDate", endDate);

        //        var schedules = await _db.QueryAsync<PanelMemberScheduleDto>(
        //            "[RUC].[SP_Recruitment_GetConfirmedHeadSchedules]",
        //            parameters,
        //            commandType: CommandType.StoredProcedure
        //        );

        //        var schedulesList = schedules.ToList();

        //        return new PanelMemberScheduleListResponseDto
        //        {
        //            Schedules = schedulesList,
        //            TotalRecords = schedulesList.Count
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in GetConfirmedHeadSchedulesAsync");
        //        return new PanelMemberScheduleListResponseDto
        //        {
        //            Schedules = new List<PanelMemberScheduleDto>(),
        //            TotalRecords = 0
        //        };
        //    }
        //}

        public async Task<PanelMemberScheduleListResponseDto> GetConfirmedHeadSchedulesAsync(
    int companyID, int? statusID, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", companyID);
                parameters.Add("@StatusID", statusID);
                parameters.Add("@StartDate", startDate);
                parameters.Add("@EndDate", endDate);

                var schedules = await _db.QueryAsync<PanelMemberScheduleDto>(
                    "[RUC].[SP_Recruitment_GetConfirmedHeadSchedules]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var schedulesList = schedules.ToList();

                return new PanelMemberScheduleListResponseDto
                {
                    Schedules = schedulesList,
                    TotalRecords = schedulesList.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetConfirmedHeadSchedulesAsync");

                return new PanelMemberScheduleListResponseDto
                {
                    Schedules = new List<PanelMemberScheduleDto>(),
                    TotalRecords = 0
                };
            }
        }


        public async Task<ApplicationAIStatusDto> GetApplicationAIStatusAsync(int applicationID, int companyID)
        {
            if (_db.State != ConnectionState.Open)
                _db.Open();

            var parameters = new DynamicParameters();
            parameters.Add("@ApplicationID", applicationID);
            parameters.Add("@CompanyID", companyID);

            var result = await _db.QueryFirstOrDefaultAsync<ApplicationAIStatusDto>(
                "[RUC].[SP_Recruitment_GetApplicationAIStatus]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result ?? new ApplicationAIStatusDto
            {
                ApplicationID = applicationID,
                IsResumeParsed = false,
                IsScreened = false,
                ScreeningScore = null
            };
        }

        public async Task<List<RecDashboardRecStatsItemDto>> GetDashboardRecStatsAsync()
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var result = (await _db.QueryAsync<RecDashboardRecStatsItemDto>(
                    "[RUC].[sp_Dashboard_RecStats]",
                    null,
                    commandType: CommandType.StoredProcedure)).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardRecStatsAsync");
                return new List<RecDashboardRecStatsItemDto>();
            }
        }

        // =============================================
        // JOB BANK
        // =============================================

        public async Task<(int JobBankCandidateID, bool IsSuccess, string Message)> JobBankCandidateInsertAsync(JobBankCandidateInsertRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@FirstName", request.FirstName);
                parameters.Add("@LastName", request.LastName);
                parameters.Add("@Email", request.Email);
                parameters.Add("@PhoneNumber", request.PhoneNumber);
                //parameters.Add("@CNIC", request.Cnic);
                //parameters.Add("@DateOfBirth", request.DateOfBirth);
                //parameters.Add("@GenderID", request.GenderID);
                //parameters.Add("@CurrentAddress", request.CurrentAddress);
                //parameters.Add("@CityID", request.CityID);
                //parameters.Add("@CountryID", request.CountryID);
                parameters.Add("@ResumeFilePath", request.ResumeFilePath);
                parameters.Add("@ResumeFileName", request.ResumeFileName);
                //parameters.Add("@Skills", request.Skills);
                //parameters.Add("@ExperienceYears", request.ExperienceYears);
                //parameters.Add("@ExperienceSummary", request.ExperienceSummary);
                //parameters.Add("@Education", request.Education);
                //parameters.Add("@CurrentDesignation", request.CurrentDesignation);
                //parameters.Add("@ExpectedSalary", request.ExpectedSalary);
                //parameters.Add("@PreferredLocation", request.PreferredLocation);
                parameters.Add("@CreatedBy", request.CreatedBy);
                parameters.Add("@JobBankCandidateID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[RUC].[JobBankCandidate_Insert]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var id = parameters.Get<int>("@JobBankCandidateID");
                var isSuccess = parameters.Get<bool>("@IsSuccess");
                var message = parameters.Get<string>("@Message") ?? "";
                return (id, isSuccess, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateInsertAsync");
                return (0, false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> JobBankCandidateUpdateAsync(JobBankCandidateUpdateRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@JobBankCandidateID", request.JobBankCandidateID);
                parameters.Add("@FirstName", request.FirstName);
                parameters.Add("@LastName", request.LastName);
                parameters.Add("@Email", request.Email);
                parameters.Add("@PhoneNumber", request.PhoneNumber);
                parameters.Add("@Cnic", request.Cnic);
                parameters.Add("@DateOfBirth", request.DateOfBirth);
                parameters.Add("@GenderID", request.GenderID);
                parameters.Add("@CurrentAddress", request.CurrentAddress);
                parameters.Add("@CityID", request.CityID);
                parameters.Add("@CountryID", request.CountryID);
                parameters.Add("@ResumeFilePath", request.ResumeFilePath);
                parameters.Add("@ResumeFileName", request.ResumeFileName);
                parameters.Add("@Skills", request.Skills);
                parameters.Add("@ExperienceYears", request.ExperienceYears);
                parameters.Add("@ExperienceSummary", request.ExperienceSummary);
                parameters.Add("@Education", request.Education);
                parameters.Add("@CurrentDesignation", request.CurrentDesignation);
                parameters.Add("@ExpectedSalary", request.ExpectedSalary);
                parameters.Add("@PreferredLocation", request.PreferredLocation);
                parameters.Add("@UpdatedBy", request.UpdatedBy);
                parameters.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[RUC].[JobBankCandidate_Update]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var isSuccess = parameters.Get<bool>("@IsSuccess");
                var message = parameters.Get<string>("@Message") ?? "";
                return (isSuccess, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateUpdateAsync");
                return (false, ex.Message);
            }
        }

        public async Task<JobBankCandidateResponseDto?> JobBankCandidateGetByIdAsync(int jobBankCandidateID, int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@JobBankCandidateID", jobBankCandidateID);
                parameters.Add("@CompanyID", companyID);

                var result = await _db.QueryFirstOrDefaultAsync<JobBankCandidateResponseDto>(
                    "[RUC].[JobBankCandidate_GetById]",
                    parameters,
                    commandType: CommandType.StoredProcedure);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateGetByIdAsync");
                return null;
            }
        }

        public async Task<(List<JobBankCandidateResponseDto> Candidates, int TotalRecords)> JobBankCandidateSearchAsync(JobBankCandidateSearchRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@SearchText", request.SearchText);
                parameters.Add("@SkillsFilter", request.SkillsFilter);
                parameters.Add("@MinExperienceYears", request.MinExperienceYears);
                parameters.Add("@MaxExperienceYears", request.MaxExperienceYears);
                parameters.Add("@EducationKeyword", request.EducationKeyword);
                parameters.Add("@CityID", request.CityID);
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var candidates = (await _db.QueryAsync<JobBankCandidateResponseDto>(
                    "[RUC].[JobBankCandidate_Search]",
                    parameters,
                    commandType: CommandType.StoredProcedure)).ToList();
                var totalRecords = parameters.Get<int>("@TotalRecords");
                return (candidates, totalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateSearchAsync");
                return (new List<JobBankCandidateResponseDto>(), 0);
            }
        }

        public async Task<(List<JobBankCandidateResponseDto> Candidates, int TotalRecords)> JobBankCandidateGetListAsync(JobBankCandidateListRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@SearchText", request.SearchText);
                parameters.Add("@PageNumber", request.PageNumber);
                parameters.Add("@PageSize", request.PageSize);
                parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var candidates = (await _db.QueryAsync<JobBankCandidateResponseDto>(
                    "[RUC].[JobBankCandidate_GetList]",
                    parameters,
                    commandType: CommandType.StoredProcedure)).ToList();
                var totalRecords = parameters.Get<int>("@TotalRecords");
                return (candidates, totalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankCandidateGetListAsync");
                return (new List<JobBankCandidateResponseDto>(), 0);
            }
        }

        public async Task<(int JobBankShortlistID, bool IsSuccess, string Message)> JobBankShortlistInsertAsync(JobBankShortlistInsertRequestDto request)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@CompanyID", request.CompanyID);
                parameters.Add("@RequisitionID", request.RequisitionID);
                parameters.Add("@JobBankCandidateID", request.JobBankCandidateID);
                parameters.Add("@ShortlistedBy", request.ShortlistedBy);
                parameters.Add("@Remarks", request.Remarks);
                parameters.Add("@JobBankShortlistID", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[RUC].[JobBankShortlist_Insert]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var id = parameters.Get<int>("@JobBankShortlistID");
                var isSuccess = parameters.Get<bool>("@IsSuccess");
                var message = parameters.Get<string>("@Message") ?? "";
                return (id, isSuccess, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlistInsertAsync");
                return (0, false, ex.Message);
            }
        }

        public async Task<List<JobBankShortlistByRequisitionDto>> JobBankShortlistGetByRequisitionAsync(int requisitionID, int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@RequisitionID", requisitionID);
                parameters.Add("@CompanyID", companyID);

                var result = (await _db.QueryAsync<JobBankShortlistByRequisitionDto>(
                    "[RUC].[JobBankShortlist_GetByRequisition]",
                    parameters,
                    commandType: CommandType.StoredProcedure)).ToList();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlistGetByRequisitionAsync");
                return new List<JobBankShortlistByRequisitionDto>();
            }
        }

        public async Task<(bool IsSuccess, string Message)> JobBankShortlistRemoveAsync(int jobBankShortlistID, int companyID)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@JobBankShortlistID", jobBankShortlistID);
                parameters.Add("@CompanyID", companyID);
                parameters.Add("@IsSuccess", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 500, direction: ParameterDirection.Output);

                await _db.ExecuteAsync(
                    "[RUC].[JobBankShortlist_Remove]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var isSuccess = parameters.Get<bool>("@IsSuccess");
                var message = parameters.Get<string>("@Message") ?? "";
                return (isSuccess, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JobBankShortlistRemoveAsync");
                return (false, ex.Message);
            }
        }

        public async Task<(bool IsSuccess, string Message)> ConvertJobBankCandidateAsync(ConvertRequestDto request)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CompanyID", request.CompanyID);
            parameters.Add("@RequisitionID", request.RequisitionID);
            parameters.Add("@JobBankCandidateID", request.JobBankCandidateID);
            parameters.Add("@CreatedBy", request.CreatedBy);

            var result = await _db.QueryFirstOrDefaultAsync(
                "RUC.sp_JobBankCandidate_ConvertToApplication",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return (true, "successfully");
        }

        public async Task<EvaluationResponseDtos?> GetEvaluationByScheduleAsync(int scheduleId)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ScheduleID", scheduleId);

                using var multi = await _db.QueryMultipleAsync(
                    "[ruc].[SP_Ruc_Evaluation_GetByScheduleID]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var evaluation = await multi.ReadFirstOrDefaultAsync<EvaluationResponseDtos>();

                if (evaluation == null)
                    return null;

                var criteria = (await multi.ReadAsync<EvaluationCriteriaScoreDtos>()).ToList();

                evaluation.CriteriaScores = criteria;

                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEvaluationByScheduleAsync");
                return null;
            }
        }

        public async Task<bool> HireCandidateStatusAsync(int applicationID, HireCandidateDto dto)
        {
            try
            {
                if (_db.State != ConnectionState.Open)
                    _db.Open();

                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationID);
                parameters.Add("@CompanyID", dto.CompanyID);
                parameters.Add("@HiredBy", dto.HiredBy);
                parameters.Add("@Remarks", dto.Remarks);

                var result = await _db.ExecuteAsync(
                    "[ruc].[sp_HireCandidateStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HireCandidateStatusAsync");
                return false;   
            }
        }
    }
}

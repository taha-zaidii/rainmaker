/* =============================================================================
   DEMO — corrections to the [ruc] schema. NOT THE REAL ONE. DO NOT SHIP.
   =============================================================================

   Two fixes found by running the hiring loop end to end:

   1. Column aliases in the application list did not match the DTO.
      JobApplicationResponseDto exposes RequisitionJobTitle and
      RequisitionLocation, not JobTitle and Location. Dapper maps by name and
      silently leaves unmatched properties null, so the Applications grid
      rendered every row with a blank job column and no error anywhere.

   2. RequisitionCode was never generated. The requisition insert leaves it
      null, so every requisition showed as "REQ —" in the UI and the public
      careers cards had nothing to identify them by.

   Prepared by Syed Taha Zaidi, Multinet.
============================================================================= */

USE [InternDB];
GO


/* ── 1. Requisition codes ─────────────────────────────────────────────────
   Backfill what exists, then keep new rows correct with a trigger.

   A trigger rather than editing the insert procedure: several paths create
   requisitions (the AI save flow, the manual CRUD endpoint) and only one of
   them is in this demo seed. Putting it on the table means every path gets a
   code regardless of which procedure ran. Production generates the code
   inside the procedure, which is tidier but requires owning all of them.  */

UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
SET RequisitionCode = 'REQ-' + RIGHT('000000' + CAST(RequisitionID AS VARCHAR(10)), 6)
WHERE RequisitionCode IS NULL;
GO
PRINT 'Backfilled requisition codes.';
GO

CREATE OR ALTER TRIGGER dbo.TR_RucRequisition_AssignCode
ON dbo.Tbl_Ruc_RecruitmentRequisition
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
    SET RequisitionCode = 'REQ-' + RIGHT('000000' + CAST(r.RequisitionID AS VARCHAR(10)), 6)
    FROM dbo.Tbl_Ruc_RecruitmentRequisition r
    INNER JOIN inserted i ON i.RequisitionID = r.RequisitionID
    WHERE r.RequisitionCode IS NULL;
END
GO
PRINT 'Created code-assignment trigger.';
GO


/* ── 2. Application list — aliases the DTO actually binds to ───────────── */

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobApplication_GetAll
    @CompanyID       INT,
    @PageNumber      INT           = 1,
    @PageSize        INT           = 50,
    @SearchTerm      NVARCHAR(200) = NULL,
    @RequisitionID   INT           = NULL,
    @ApplicantID     INT           = NULL,
    @CurrentStatusID INT           = NULL,
    @IsActive        BIT           = NULL,
    @ApplicationID   INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ap.ApplicationID, ap.ApplicationCode, ap.CompanyID,
        ap.RequisitionID, ap.ApplicantID, ap.ApplicationDate,
        ap.ApplicationSourceID, ap.CurrentStatusID,
        StatusID   = s.StatusID,
        StatusCode = s.StatusCode,
        StatusName = s.StatusName,
        ap.ResumePath, ap.CoverLetter, ap.Remarks,
        ap.ScreeningScore, ap.ResumeParsingID,
        ap.IsActive, ap.CreatedBy, ap.CreatedDate,

        -- Candidate, denormalised so the grid needs one round trip.
        FullName        = LTRIM(RTRIM(a.FirstName + ' ' + ISNULL(a.LastName, ''))),
        Email           = a.Email,
        MobileNumber    = a.MobileNumber,
        ApplicantCode   = a.ApplicantCode,
        CurrentJobTitle = a.CurrentJobTitle,

        -- These three names are what the DTO binds. Renaming them to the
        -- obvious JobTitle/Location silently blanks the columns.
        RequisitionCode     = r.RequisitionCode,
        RequisitionJobTitle = r.JobTitle,
        RequisitionLocation = r.Location,
        DepartmentID        = r.DepartmentID,
        DepartmentName      = r.DepartmentName
    FROM dbo.Tbl_Ruc_JobApplication ap
    LEFT JOIN dbo.Tbl_Ruc_Applicant a              ON a.ApplicantID   = ap.ApplicantID
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition r ON r.RequisitionID = ap.RequisitionID
    LEFT JOIN ruc.Tbl_Status s                     ON s.StatusID      = ap.CurrentStatusID
    WHERE ap.CompanyID = @CompanyID
      AND (@RequisitionID   IS NULL OR ap.RequisitionID   = @RequisitionID)
      AND (@ApplicantID     IS NULL OR ap.ApplicantID     = @ApplicantID)
      AND (@CurrentStatusID IS NULL OR ap.CurrentStatusID = @CurrentStatusID)
      AND (@ApplicationID   IS NULL OR ap.ApplicationID   = @ApplicationID)
      AND ap.IsActive       = ISNULL(@IsActive, 1)
      AND (@SearchTerm      IS NULL OR a.FirstName LIKE '%' + @SearchTerm + '%'
                                    OR a.LastName  LIKE '%' + @SearchTerm + '%'
                                    OR a.Email     LIKE '%' + @SearchTerm + '%'
                                    OR r.JobTitle  LIKE '%' + @SearchTerm + '%')
    ORDER BY ap.ApplicationID DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*)
    FROM dbo.Tbl_Ruc_JobApplication ap
    LEFT JOIN dbo.Tbl_Ruc_Applicant a              ON a.ApplicantID   = ap.ApplicantID
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition r ON r.RequisitionID = ap.RequisitionID
    WHERE ap.CompanyID = @CompanyID
      AND (@RequisitionID   IS NULL OR ap.RequisitionID   = @RequisitionID)
      AND (@ApplicantID     IS NULL OR ap.ApplicantID     = @ApplicantID)
      AND (@CurrentStatusID IS NULL OR ap.CurrentStatusID = @CurrentStatusID)
      AND (@ApplicationID   IS NULL OR ap.ApplicationID   = @ApplicationID)
      AND ap.IsActive       = ISNULL(@IsActive, 1)
      AND (@SearchTerm      IS NULL OR a.FirstName LIKE '%' + @SearchTerm + '%'
                                    OR a.LastName  LIKE '%' + @SearchTerm + '%'
                                    OR a.Email     LIKE '%' + @SearchTerm + '%'
                                    OR r.JobTitle  LIKE '%' + @SearchTerm + '%');

END
GO

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_Applicant_Create
    @CompanyID          INT,
    @FirstName          NVARCHAR(100),
    @MiddleName         NVARCHAR(100)  = NULL,
    @LastName           NVARCHAR(100)  = NULL,
    @DateOfBirth        DATETIME       = NULL,
    @GenderID           INT            = NULL,
    @NationalID         NVARCHAR(50)   = NULL,
    @Email              NVARCHAR(200)  = NULL,
    @MobileNumber       NVARCHAR(50)   = NULL,
    @PhoneNumber        NVARCHAR(50)   = NULL,
    @CurrentAddress     NVARCHAR(500)  = NULL,
    @CityID             INT            = NULL,
    @CountryID          INT            = NULL,
    @MaritalStatusID    INT            = NULL,
    @ReligionID         INT            = NULL,
    @TotalExperience    DECIMAL(5,2)   = NULL,
    @CurrentJobTitle    NVARCHAR(200)  = NULL,
    @CurrentCompany     NVARCHAR(200)  = NULL,
    @ExpectedSalary     DECIMAL(18,2)  = NULL,
    @NoticePeriod       INT            = NULL,
    @ResumePath         NVARCHAR(500)  = NULL,
    @CoverLetter        NVARCHAR(MAX)  = NULL,
    @Cnic               NVARCHAR(50)   = NULL,
    @Skills             NVARCHAR(MAX)  = NULL,
    @ExperienceYears    DECIMAL(5,2)   = NULL,
    @ExperienceSummary  NVARCHAR(MAX)  = NULL,
    @Education          NVARCHAR(MAX)  = NULL,
    @CurrentDesignation NVARCHAR(200)  = NULL,
    @PreferredLocation  NVARCHAR(200)  = NULL,
    @CreatedBy          NVARCHAR(100)  = NULL,
    @ApplicantID        INT            OUTPUT,
    @ApplicantCode      NVARCHAR(50)   OUTPUT,
    @Result             INT            OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Result = 0;

    BEGIN TRY
        DECLARE @ExistingID INT = NULL;
        IF @Email IS NOT NULL AND LEN(RTRIM(@Email)) > 0
        BEGIN
            SELECT TOP 1 @ExistingID = ApplicantID 
            FROM dbo.Tbl_Ruc_Applicant 
            WHERE Email = @Email AND CompanyID = @CompanyID AND IsActive = 1;
        END

        IF @ExistingID IS NOT NULL
        BEGIN
            UPDATE dbo.Tbl_Ruc_Applicant
            SET FirstName          = COALESCE(@FirstName, FirstName),
                LastName           = COALESCE(@LastName, LastName),
                MobileNumber       = COALESCE(@MobileNumber, MobileNumber),
                CurrentAddress     = COALESCE(@CurrentAddress, CurrentAddress),
                CurrentJobTitle    = COALESCE(@CurrentJobTitle, CurrentJobTitle),
                CurrentDesignation = COALESCE(@CurrentDesignation, @CurrentJobTitle, CurrentDesignation),
                CurrentCompany     = COALESCE(@CurrentCompany, CurrentCompany),
                TotalExperience    = COALESCE(@TotalExperience, TotalExperience),
                ExperienceYears    = COALESCE(@ExperienceYears, ExperienceYears),
                ExperienceSummary  = COALESCE(@ExperienceSummary, ExperienceSummary),
                Education          = COALESCE(@Education, Education),
                Skills             = COALESCE(@Skills, Skills),
                PreferredLocation  = COALESCE(@PreferredLocation, @CurrentAddress, PreferredLocation),
                ExpectedSalary     = COALESCE(@ExpectedSalary, ExpectedSalary),
                NoticePeriod       = COALESCE(@NoticePeriod, NoticePeriod),
                ResumePath         = COALESCE(@ResumePath, ResumePath),
                CoverLetter        = COALESCE(@CoverLetter, CoverLetter),
                UpdatedDate        = GETDATE()
            WHERE ApplicantID = @ExistingID;


            SET @ApplicantID   = @ExistingID;
            SELECT @ApplicantCode = ApplicantCode FROM dbo.Tbl_Ruc_Applicant WHERE ApplicantID = @ApplicantID;
            SET @Result = 1;
            RETURN;
        END

        INSERT INTO dbo.Tbl_Ruc_Applicant
            (CompanyID, FirstName, MiddleName, LastName, DateOfBirth, GenderID,
             NationalID, Cnic, Email, MobileNumber, PhoneNumber, CurrentAddress,
             CityID, CountryID, MaritalStatusID, ReligionID,
             TotalExperience, ExperienceYears, ExperienceSummary, Education, Skills,
             CurrentJobTitle, CurrentDesignation, CurrentCompany, PreferredLocation,
             ExpectedSalary, NoticePeriod, ResumePath, CoverLetter, CreatedBy)
        VALUES
            (@CompanyID, @FirstName, @MiddleName, @LastName, @DateOfBirth, @GenderID,
             @NationalID, @Cnic, @Email, @MobileNumber, @PhoneNumber, @CurrentAddress,
             @CityID, @CountryID, @MaritalStatusID, @ReligionID,
             @TotalExperience, @ExperienceYears, @ExperienceSummary, @Education, @Skills,
             @CurrentJobTitle, ISNULL(@CurrentDesignation, @CurrentJobTitle), @CurrentCompany, @PreferredLocation,
             @ExpectedSalary, @NoticePeriod, @ResumePath, @CoverLetter, @CreatedBy);

        SET @ApplicantID   = CAST(SCOPE_IDENTITY() AS INT);
        SET @ApplicantCode = 'CAN' + RIGHT('000000' + CAST(@ApplicantID AS VARCHAR(10)), 6);

        UPDATE dbo.Tbl_Ruc_Applicant
        SET ApplicantCode = @ApplicantCode
        WHERE ApplicantID = @ApplicantID;

        SET @Result = 1;
    END TRY
    BEGIN CATCH
        SET @Result = 0;
    END CATCH
END
GO



CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobApplication_GetById
    @ApplicationID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ap.ApplicationID, ap.ApplicationCode, ap.CompanyID,
        ap.RequisitionID, ap.ApplicantID, ap.ApplicationDate,
        ap.ApplicationSourceID, ap.CurrentStatusID,
        StatusID   = s.StatusID,
        StatusCode = s.StatusCode,
        StatusName = s.StatusName,
        ap.ResumePath, ap.CoverLetter, ap.Remarks,
        ap.ScreeningScore, ap.ResumeParsingID,
        ap.IsActive, ap.CreatedBy, ap.CreatedDate,

        -- Candidate, denormalised by joining Tbl_Ruc_Applicant
        FullName           = LTRIM(RTRIM(a.FirstName + ' ' + ISNULL(a.LastName, ''))),
        Email              = a.Email,
        MobileNumber       = a.MobileNumber,
        ApplicantCode      = a.ApplicantCode,
        CurrentJobTitle    = a.CurrentJobTitle,
        CurrentDesignation = ISNULL(a.CurrentDesignation, a.CurrentJobTitle),
        CurrentCompany     = a.CurrentCompany,
        CurrentAddress     = a.CurrentAddress,
        Skills             = a.Skills,
        Education          = a.Education,
        ExperienceSummary  = a.ExperienceSummary,
        ExpectedSalary     = CAST(a.ExpectedSalary AS VARCHAR(50)),
        PreferredLocation  = a.PreferredLocation,
        NoticePeriod       = CAST(a.NoticePeriod AS VARCHAR(50)),
        ExperienceYears    = a.ExperienceYears,
        TotalExperience    = a.TotalExperience,

        RequisitionCode     = r.RequisitionCode,
        RequisitionJobTitle = r.JobTitle,
        RequisitionLocation = r.Location,
        DepartmentID        = r.DepartmentID,
        DepartmentName      = r.DepartmentName
    FROM dbo.Tbl_Ruc_JobApplication ap
    LEFT JOIN dbo.Tbl_Ruc_Applicant a              ON a.ApplicantID   = ap.ApplicantID
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition r ON r.RequisitionID = ap.RequisitionID
    LEFT JOIN ruc.Tbl_Status s                     ON s.StatusID      = ap.CurrentStatusID
    WHERE ap.ApplicationID = @ApplicationID;
END
GO


/* ── 3. Delete Stored Procedures for Applications and Requisitions ─────── */

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobApplication_Delete
    @ApplicationID INT,
    @DeletedBy NVARCHAR(100) = 'System',
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Ruc_JobApplication WHERE ApplicationID = @ApplicationID)
    BEGIN
        SET @Result = -1;
        RETURN;
    END

    DECLARE @ReqID INT;
    SELECT @ReqID = RequisitionID FROM dbo.Tbl_Ruc_JobApplication WHERE ApplicationID = @ApplicationID;

    UPDATE dbo.Tbl_Ruc_JobApplication
    SET IsActive = 0,
        UpdatedBy = ISNULL(@DeletedBy, 'System'),
        UpdatedDate = GETDATE()
    WHERE ApplicationID = @ApplicationID;

    IF @ReqID IS NOT NULL
    BEGIN
        UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
        SET TotalApplications = CASE WHEN ISNULL(TotalApplications, 0) > 0 THEN TotalApplications - 1 ELSE 0 END
        WHERE RequisitionID = @ReqID;
    END

    SET @Result = 1;
END
GO

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobRequisition_Delete
    @RequisitionID INT,
    @CompanyID INT,
    @DeletedBy NVARCHAR(100) = 'System',
    @Reason NVARCHAR(MAX) = NULL,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Ruc_RecruitmentRequisition WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID)
    BEGIN
        SET @Result = -1;
        RETURN;
    END

    UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
    SET IsActive = 0,
        UpdatedBy = ISNULL(@DeletedBy, 'System'),
        UpdatedOn = GETDATE()
    WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID;


    UPDATE dbo.Tbl_Ruc_JobApplication
    SET IsActive = 0,
        UpdatedBy = ISNULL(@DeletedBy, 'System'),
        UpdatedDate = GETDATE()
    WHERE RequisitionID = @RequisitionID;

    SET @Result = 1;
END
GO

PRINT 'Created delete stored procedures.';
GO

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobRequisition_GetAll
    @CompanyID    INT,
    @PageNumber   INT           = 1,
    @PageSize     INT           = 50,
    @SearchTerm   NVARCHAR(200) = NULL,
    @StatusID     INT           = NULL,
    @IsActive     BIT           = NULL,
    @DepartmentID INT           = NULL,
    @CreatedBy    NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.RequisitionID, r.RequisitionCode, r.CompanyID, r.JobTitle, r.JobSummary,
        r.DepartmentID, r.DepartmentName, r.DesignationID, r.DesignationName,
        r.EmploymentTypeID, r.EmploymentTypeName, r.GradeID, r.Vacancies,
        r.MinExperience, r.MaxExperience, r.MinAge, r.MaxAge,
        r.MinSalary, r.MaxSalary, r.Location, r.ReportingTo,
        r.KeyResponsibilities, r.Requirements, r.Qualifications, r.Skills,
        r.Benefits, r.Justification,
        r.IsPublished, r.PublishedDate, r.ClosingDate,
        r.StatusID, r.StatusCode, r.StatusName, r.JobCategoryID,
        r.Isbudget, r.IsNonBudget, r.IsActive, r.IsDefault,
        r.CreatedBy, r.CreatedOn AS CreatedDate,
        EmployeeName = NULL,
        ApplicationCount = (SELECT COUNT(*) FROM dbo.Tbl_Ruc_JobApplication a
                            WHERE a.RequisitionID = r.RequisitionID AND a.IsActive = 1)
    FROM dbo.Tbl_Ruc_RecruitmentRequisition r
    WHERE r.CompanyID = @CompanyID
      AND r.IsActive     = ISNULL(@IsActive, 1)
      AND (@StatusID     IS NULL OR r.StatusID = @StatusID)
      AND (@DepartmentID IS NULL OR r.DepartmentID = @DepartmentID)
      AND (@CreatedBy    IS NULL OR r.CreatedBy = @CreatedBy)
      AND (@SearchTerm   IS NULL OR r.JobTitle LIKE '%' + @SearchTerm + '%'
                                 OR r.RequisitionCode LIKE '%' + @SearchTerm + '%')
    ORDER BY r.RequisitionID DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*)
    FROM dbo.Tbl_Ruc_RecruitmentRequisition r
    WHERE r.CompanyID = @CompanyID
      AND r.IsActive = ISNULL(@IsActive, 1)
      AND (@StatusID     IS NULL OR r.StatusID = @StatusID)
      AND (@DepartmentID IS NULL OR r.DepartmentID = @DepartmentID)
      AND (@CreatedBy    IS NULL OR r.CreatedBy = @CreatedBy)
      AND (@SearchTerm   IS NULL OR r.JobTitle LIKE '%' + @SearchTerm + '%'
                                 OR r.RequisitionCode LIKE '%' + @SearchTerm + '%');
END
GO





-- ============================================================================
-- Workflow Stored Procedures for Shortlist & Reject Candidates
-- ============================================================================

CREATE OR ALTER PROCEDURE ruc.SP_Recruitment_ShortlistCandidate
    @ApplicationID       INT,
    @CompanyID           INT = 133,
    @ShortlistedBy       NVARCHAR(100) = 'System',
    @Remarks             NVARCHAR(MAX) = NULL,
    @PreviousStatusID    INT OUTPUT,
    @PreviousStatusCode  NVARCHAR(50) OUTPUT,
    @NewStatusID         INT OUTPUT,
    @NewStatusCode       NVARCHAR(50) OUTPUT,
    @Result              INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT 
            @PreviousStatusID = ap.CurrentStatusID,
            @PreviousStatusCode = s.StatusCode
        FROM dbo.Tbl_Ruc_JobApplication ap
        LEFT JOIN ruc.Tbl_Status s ON s.StatusID = ap.CurrentStatusID
        WHERE ap.ApplicationID = @ApplicationID;

        IF @PreviousStatusID IS NULL
        BEGIN
            SET @Result = 0;
            RETURN;
        END

        -- Update Application Status to SHORTLISTED (StatusID = 2)
        UPDATE dbo.Tbl_Ruc_JobApplication
        SET CurrentStatusID = 2,
            Remarks = ISNULL(@Remarks, Remarks),
            UpdatedBy = @ShortlistedBy,
            UpdatedDate = GETDATE()
        WHERE ApplicationID = @ApplicationID;

        SET @NewStatusID = 2;
        SET @NewStatusCode = 'SHORTLISTED';
        SET @Result = 1;
    END TRY
    BEGIN CATCH
        SET @Result = 0;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE ruc.SP_Recruitment_RejectApplication
    @ApplicationID       INT,
    @CompanyID           INT = 133,
    @RejectionReason     NVARCHAR(MAX) = NULL,
    @RejectedBy          NVARCHAR(100) = 'System',
    @PreviousStatusID    INT OUTPUT,
    @PreviousStatusCode  NVARCHAR(50) OUTPUT,
    @NewStatusID         INT OUTPUT,
    @NewStatusCode       NVARCHAR(50) OUTPUT,
    @Result              INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        SELECT 
            @PreviousStatusID = ap.CurrentStatusID,
            @PreviousStatusCode = s.StatusCode
        FROM dbo.Tbl_Ruc_JobApplication ap
        LEFT JOIN ruc.Tbl_Status s ON s.StatusID = ap.CurrentStatusID
        WHERE ap.ApplicationID = @ApplicationID;

        IF @PreviousStatusID IS NULL
        BEGIN
            SET @Result = 0;
            RETURN;
        END

        -- Update Application Status to REJECTED (StatusID = 7)
        UPDATE dbo.Tbl_Ruc_JobApplication
        SET CurrentStatusID = 7,
            Remarks = ISNULL(@RejectionReason, Remarks),
            UpdatedBy = @RejectedBy,
            UpdatedDate = GETDATE()
        WHERE ApplicationID = @ApplicationID;

        SET @NewStatusID = 7;
        SET @NewStatusCode = 'REJECTED';
        SET @Result = 1;
    END TRY
    BEGIN CATCH
        SET @Result = 0;
    END CATCH
END
GO



CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobApplication_Update
    @ApplicationID        INT,
    @CurrentStatusID      INT           = NULL,
    @ResumePath           NVARCHAR(500) = NULL,
    @CoverLetter          NVARCHAR(MAX) = NULL,
    @ScreeningScore       DECIMAL(5,2)  = NULL,
    @OverallRating        DECIMAL(5,2)  = NULL,
    @FinalRecommendation NVARCHAR(MAX) = NULL,
    @RejectionReason      NVARCHAR(MAX) = NULL,
    @OfferLetterPath      NVARCHAR(500) = NULL,
    @OfferAccepted        BIT           = NULL,
    @Remarks              NVARCHAR(MAX) = NULL,
    @IsActive             BIT           = NULL,
    @UpdatedBy            NVARCHAR(100) = NULL,
    @Result               INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Ruc_JobApplication WHERE ApplicationID = @ApplicationID)
        BEGIN
            SET @Result = 0;
            RETURN;
        END

        UPDATE dbo.Tbl_Ruc_JobApplication
        SET CurrentStatusID     = COALESCE(@CurrentStatusID, CurrentStatusID),
            ResumePath          = COALESCE(@ResumePath, ResumePath),
            CoverLetter         = COALESCE(@CoverLetter, CoverLetter),
            ScreeningScore      = COALESCE(@ScreeningScore, ScreeningScore),
            Remarks             = COALESCE(@Remarks, @RejectionReason, @FinalRecommendation, Remarks),
            IsActive            = COALESCE(@IsActive, IsActive),
            UpdatedBy           = COALESCE(@UpdatedBy, 'System'),
            UpdatedDate         = GETDATE()
        WHERE ApplicationID = @ApplicationID;

        SET @Result = 1;
    END TRY
    BEGIN CATCH
        SET @Result = 0;
    END CATCH
END
GO


PRINT '';
PRINT '─────────────────────────────────────────────────────';
PRINT ' [ruc] corrections and shortlist/reject/update SPs applied.';
PRINT '─────────────────────────────────────────────────────';
GO



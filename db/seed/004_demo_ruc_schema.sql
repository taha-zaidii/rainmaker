/* =============================================================================
   DEMO — the [ruc] schema. NOT THE REAL ONE. DO NOT SHIP.
   =============================================================================

   Why this file exists
   --------------------
   The recruitment repository calls almost every CRUD procedure with an
   explicit schema:

       await _db.ExecuteAsync("[ruc].[SP_Ruc_JobApplication_Create]", ...)

   The earlier demo seeds created their procedures in [dbo]. SQL Server does
   not fall back across schemas, so every one of those calls threw
   "Could not find stored procedure" — and because the repository catches and
   logs rather than rethrows, the API answered 200 with an empty list.

   That is the worst possible failure mode: two job requisitions sitting in
   the table while the screen cheerfully reports none, with no error anywhere
   a user can see. This file puts the procedures where the code looks for
   them.

   Scope
   -----
   Only the objects needed for the end-to-end hiring loop:
       generate JD -> save as requisition -> publish -> public careers page
       -> candidate applies -> resume parsed -> appears in Applications
   Interviews, evaluations and panel scoring are NOT here.

   Fidelity
   --------
   Parameter names and result-set shapes are taken from the repository's own
   Dapper calls and DTOs, so the contract is exact. The BUSINESS LOGIC inside
   each procedure is a reasonable reconstruction, not the production one.
   Anything subtle — code formats, status transitions, audit columns — will
   differ from the real thing.

   Replaced wholesale by the supervisor's InternDB.bak. Delete this after.

   Prepared by Syed Taha Zaidi, Multinet.
============================================================================= */

USE [InternDB];
GO

IF SCHEMA_ID('ruc') IS NULL
    EXEC('CREATE SCHEMA [ruc]');
GO
PRINT 'Schema [ruc] ready.';
GO


/* ═══════════════════════════════════════════════════════════════════════════
   TABLES
   ═══════════════════════════════════════════════════════════════════════════ */

/* ── ruc.Tbl_StatusType ───────────────────────────────────────────────── */
IF OBJECT_ID('ruc.Tbl_StatusType', 'U') IS NULL
BEGIN
    CREATE TABLE ruc.Tbl_StatusType
    (
        StatusTypeID INT IDENTITY(1,1) PRIMARY KEY,
        TypeCode     NVARCHAR(50)  NOT NULL,
        TypeName     NVARCHAR(100) NOT NULL,
        Description  NVARCHAR(500) NULL,
        IsActive     BIT           NOT NULL DEFAULT (1),
        CreatedBy    NVARCHAR(100) NULL,
        CreatedOn    DATETIME      NULL DEFAULT (GETDATE()),
        UpdatedBy    NVARCHAR(100) NULL,
        UpdatedOn    DATETIME      NULL
    );
    PRINT 'Created ruc.Tbl_StatusType.';
END
GO

/* ── ruc.Tbl_Status ───────────────────────────────────────────────────────
   The application pipeline. The repository reads StatusName straight from
   this table after publishing, so it must exist in [ruc], not [dbo].      */
IF OBJECT_ID('ruc.Tbl_Status', 'U') IS NULL
BEGIN
    CREATE TABLE ruc.Tbl_Status
    (
        StatusID     INT IDENTITY(1,1) PRIMARY KEY,
        StatusTypeID INT           NOT NULL,
        StatusCode   NVARCHAR(50)  NOT NULL,
        StatusName   NVARCHAR(100) NOT NULL,
        Description  NVARCHAR(500) NULL,
        DisplayOrder INT           NULL,
        IsActive     BIT           NOT NULL DEFAULT (1),
        CreatedBy    NVARCHAR(100) NULL,
        CreatedOn    DATETIME      NULL DEFAULT (GETDATE())
    );
    PRINT 'Created ruc.Tbl_Status.';
END
GO

/* ── dbo.Tbl_Ruc_Applicant ────────────────────────────────────────────────
   The candidate record. A person exists once here and may hold many
   applications — which is what lets the same CV be reused across openings
   instead of being re-parsed each time.                                    */
IF OBJECT_ID('dbo.Tbl_Ruc_Applicant', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_Ruc_Applicant
    (
        ApplicantID        INT IDENTITY(1,1) PRIMARY KEY,
        ApplicantCode      NVARCHAR(50)   NULL,
        CompanyID          INT            NOT NULL,

        FirstName          NVARCHAR(100)  NOT NULL,
        MiddleName         NVARCHAR(100)  NULL,
        LastName           NVARCHAR(100)  NULL,
        DateOfBirth        DATETIME       NULL,
        GenderID           INT            NULL,
        NationalID         NVARCHAR(50)   NULL,
        Cnic               NVARCHAR(50)   NULL,

        Email              NVARCHAR(200)  NULL,
        MobileNumber       NVARCHAR(50)   NULL,
        PhoneNumber        NVARCHAR(50)   NULL,
        CurrentAddress     NVARCHAR(500)  NULL,
        CityID             INT            NULL,
        CountryID          INT            NULL,
        MaritalStatusID    INT            NULL,
        ReligionID         INT            NULL,

        -- Populated by the AI resume parser when a CV is uploaded.
        TotalExperience    DECIMAL(5,2)   NULL,
        ExperienceYears    DECIMAL(5,2)   NULL,
        ExperienceSummary  NVARCHAR(MAX)  NULL,
        Education          NVARCHAR(MAX)  NULL,
        Skills             NVARCHAR(MAX)  NULL,
        CurrentJobTitle    NVARCHAR(200)  NULL,
        CurrentDesignation NVARCHAR(200)  NULL,
        CurrentCompany     NVARCHAR(200)  NULL,
        PreferredLocation  NVARCHAR(200)  NULL,
        ExpectedSalary     DECIMAL(18,2)  NULL,
        NoticePeriod       INT            NULL,

        ResumePath         NVARCHAR(500)  NULL,
        CoverLetter        NVARCHAR(MAX)  NULL,

        IsActive           BIT            NOT NULL DEFAULT (1),
        CreatedBy          NVARCHAR(100)  NULL,
        CreatedDate        DATETIME       NULL DEFAULT (GETDATE()),
        UpdatedBy          NVARCHAR(100)  NULL,
        UpdatedDate        DATETIME       NULL
    );

    CREATE INDEX IX_RucApplicant_Company ON dbo.Tbl_Ruc_Applicant (CompanyID, IsActive);
    CREATE INDEX IX_RucApplicant_Email   ON dbo.Tbl_Ruc_Applicant (Email);
    PRINT 'Created dbo.Tbl_Ruc_Applicant.';
END
GO

/* Columns the application table needs but the earlier seed may not have. */
IF COL_LENGTH('dbo.Tbl_Ruc_JobApplication', 'ApplicationCode') IS NULL
    ALTER TABLE dbo.Tbl_Ruc_JobApplication ADD ApplicationCode NVARCHAR(50) NULL;
GO
IF COL_LENGTH('dbo.Tbl_Ruc_JobApplication', 'ResumeParsingID') IS NULL
    ALTER TABLE dbo.Tbl_Ruc_JobApplication ADD ResumeParsingID INT NULL;
GO
IF COL_LENGTH('dbo.Tbl_Ruc_JobApplication', 'ScreeningScore') IS NULL
    ALTER TABLE dbo.Tbl_Ruc_JobApplication ADD ScreeningScore DECIMAL(5,2) NULL;
GO


/* ═══════════════════════════════════════════════════════════════════════════
   SEED DATA — the application pipeline
   ═══════════════════════════════════════════════════════════════════════════ */

IF NOT EXISTS (SELECT 1 FROM ruc.Tbl_StatusType WHERE TypeCode = 'APPLICATION')
    INSERT INTO ruc.Tbl_StatusType (TypeCode, TypeName, Description)
    VALUES ('APPLICATION', 'Application Status', 'Stages a job application moves through'),
           ('REQUISITION', 'Requisition Status', 'Lifecycle of a job requisition');
GO

IF NOT EXISTS (SELECT 1 FROM ruc.Tbl_Status WHERE StatusCode = 'APPLIED')
BEGIN
    DECLARE @appType INT = (SELECT StatusTypeID FROM ruc.Tbl_StatusType WHERE TypeCode = 'APPLICATION');
    DECLARE @reqType INT = (SELECT StatusTypeID FROM ruc.Tbl_StatusType WHERE TypeCode = 'REQUISITION');

    INSERT INTO ruc.Tbl_Status (StatusTypeID, StatusCode, StatusName, DisplayOrder) VALUES
        (@appType, 'APPLIED',      'Applied',      1),
        (@appType, 'SHORTLISTED',  'Shortlisted',  2),
        (@appType, 'INTERVIEW',    'Interview',    3),
        (@appType, 'EVALUATED',    'Evaluated',    4),
        (@appType, 'OFFERED',      'Offered',      5),
        (@appType, 'HIRED',        'Hired',        6),
        (@appType, 'REJECTED',     'Rejected',     7),
        (@reqType, 'DRAFT',        'Draft',        1),
        (@reqType, 'PENDING',      'Pending Approval', 2),
        (@reqType, 'PUBLISHED',    'Published',    3),
        (@reqType, 'CLOSED',       'Closed',       4);
    PRINT 'Seeded application + requisition statuses.';
END
GO


/* ═══════════════════════════════════════════════════════════════════════════
   PROCEDURES — job requisitions
   ═══════════════════════════════════════════════════════════════════════════ */

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

    -- Two result sets: the page, then the total. The repository reads them
    -- with QueryMultipleAsync in exactly this order.
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
                            WHERE a.RequisitionID = r.RequisitionID)
    FROM dbo.Tbl_Ruc_RecruitmentRequisition r
    WHERE r.CompanyID = @CompanyID
      AND (@IsActive     IS NULL OR r.IsActive = @IsActive)
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
      AND (@IsActive     IS NULL OR r.IsActive = @IsActive)
      AND (@StatusID     IS NULL OR r.StatusID = @StatusID)
      AND (@DepartmentID IS NULL OR r.DepartmentID = @DepartmentID)
      AND (@CreatedBy    IS NULL OR r.CreatedBy = @CreatedBy)
      AND (@SearchTerm   IS NULL OR r.JobTitle LIKE '%' + @SearchTerm + '%'
                                 OR r.RequisitionCode LIKE '%' + @SearchTerm + '%');
END
GO


CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobRequisition_GetById
    @RequisitionID INT
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
        EmployeeName = NULL
    FROM dbo.Tbl_Ruc_RecruitmentRequisition r
    WHERE r.RequisitionID = @RequisitionID;
END
GO


/* ── The public careers feed ──────────────────────────────────────────────
   Deliberately narrow: only PUBLISHED, ACTIVE requisitions whose closing
   date has not passed. This is the one query in the system that anonymous
   internet traffic can reach, so it must never expose a draft.            */
CREATE OR ALTER PROCEDURE ruc.SP_Recruitment_GetPublicRequisitions
    @CompanyID    INT,
    @SearchText   NVARCHAR(200) = NULL,
    @DepartmentID INT           = NULL,
    @Location     NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        r.RequisitionID, r.RequisitionCode, r.CompanyID, r.JobTitle, r.JobSummary,
        r.DepartmentID, r.DepartmentName, r.DesignationID, r.DesignationName,
        r.EmploymentTypeID, r.EmploymentTypeName, r.Vacancies,
        r.MinExperience, r.MaxExperience, r.Location,
        r.KeyResponsibilities, r.Requirements, r.Qualifications, r.Skills,
        r.Benefits, r.IsPublished, r.PublishedDate, r.ClosingDate,
        r.StatusID, r.StatusCode, r.StatusName, r.JobCategoryID,
        r.IsActive,
        ApplicationCount = (SELECT COUNT(*) FROM dbo.Tbl_Ruc_JobApplication a
                            WHERE a.RequisitionID = r.RequisitionID)
    FROM dbo.Tbl_Ruc_RecruitmentRequisition r
    WHERE r.CompanyID = @CompanyID
      AND r.IsPublished = 1
      AND ISNULL(r.IsActive, 1) = 1
      AND (r.ClosingDate IS NULL OR r.ClosingDate >= CAST(GETDATE() AS DATE))
      AND (@DepartmentID IS NULL OR r.DepartmentID = @DepartmentID)
      AND (@Location     IS NULL OR r.Location LIKE '%' + @Location + '%')
      AND (@SearchText   IS NULL OR r.JobTitle LIKE '%' + @SearchText + '%'
                                 OR r.Skills   LIKE '%' + @SearchText + '%'
                                 OR r.JobSummary LIKE '%' + @SearchText + '%')
    ORDER BY r.PublishedDate DESC, r.RequisitionID DESC;
END
GO


/* ── Publishing: the moment a draft becomes a public advert ───────────────
   A human action, always. The AI produces drafts and never reaches here.  */
CREATE OR ALTER PROCEDURE ruc.SP_Recruitment_PublishRequisition
    @RequisitionID INT,
    @CompanyID     INT,
    @PublishedBy   NVARCHAR(100) = NULL,
    @IsPublished   BIT           OUTPUT,
    @PublishedDate DATETIME      OUTPUT,
    @StatusID      INT           OUTPUT,
    @StatusCode    NVARCHAR(50)  OUTPUT,
    @Result        INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Result = 0;
    SET @IsPublished = 0;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Ruc_RecruitmentRequisition
                   WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID)
        RETURN;

    SELECT @StatusID  = StatusID, @StatusCode = StatusCode
    FROM ruc.Tbl_Status WHERE StatusCode = 'PUBLISHED';

    SET @PublishedDate = GETDATE();

    UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
    SET IsPublished   = 1,
        PublishedDate = @PublishedDate,
        StatusID      = @StatusID,
        StatusCode    = @StatusCode,
        StatusName    = 'Published',
        UpdatedBy     = @PublishedBy,
        UpdatedOn     = GETDATE()
    WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID;

    SET @IsPublished = 1;
    SET @Result = 1;
END
GO


/* ═══════════════════════════════════════════════════════════════════════════
   PROCEDURES — applicants (the candidate record)
   ═══════════════════════════════════════════════════════════════════════════ */

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
             @CurrentJobTitle, @CurrentDesignation, @CurrentCompany, @PreferredLocation,
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


CREATE OR ALTER PROCEDURE ruc.SP_Ruc_Applicant_GetAll
    @CompanyID  INT,
    @PageNumber INT           = 1,
    @PageSize   INT           = 50,
    @SearchTerm NVARCHAR(200) = NULL,
    @IsActive   BIT           = NULL,
    @ApplicantID INT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ApplicantID, a.ApplicantCode, a.CompanyID,
        a.FirstName, a.MiddleName, a.LastName,
        FullName = LTRIM(RTRIM(a.FirstName + ' ' + ISNULL(a.LastName, ''))),
        a.DateOfBirth, a.GenderID, a.NationalID, a.Email, a.MobileNumber,
        a.PhoneNumber, a.Cnic, a.CurrentAddress, a.CityID, a.CountryID,
        a.MaritalStatusID, a.ReligionID, a.TotalExperience,
        a.CurrentJobTitle, a.CurrentCompany, a.ExpectedSalary, a.NoticePeriod,
        a.Skills, a.Education, a.ExperienceSummary, a.ResumePath,
        a.IsActive, a.CreatedBy, a.CreatedDate, a.UpdatedBy, a.UpdatedDate
    FROM dbo.Tbl_Ruc_Applicant a
    WHERE a.CompanyID = @CompanyID
      AND (@IsActive    IS NULL OR a.IsActive = @IsActive)
      AND (@ApplicantID IS NULL OR a.ApplicantID = @ApplicantID)
      AND (@SearchTerm  IS NULL OR a.FirstName LIKE '%' + @SearchTerm + '%'
                                OR a.LastName  LIKE '%' + @SearchTerm + '%'
                                OR a.Email     LIKE '%' + @SearchTerm + '%')
    ORDER BY a.ApplicantID DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT(*)
    FROM dbo.Tbl_Ruc_Applicant a
    WHERE a.CompanyID = @CompanyID
      AND (@IsActive    IS NULL OR a.IsActive = @IsActive)
      AND (@ApplicantID IS NULL OR a.ApplicantID = @ApplicantID)
      AND (@SearchTerm  IS NULL OR a.FirstName LIKE '%' + @SearchTerm + '%'
                                OR a.LastName  LIKE '%' + @SearchTerm + '%'
                                OR a.Email     LIKE '%' + @SearchTerm + '%');
END
GO


CREATE OR ALTER PROCEDURE ruc.SP_Ruc_Applicant_GetById
    @ApplicantID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ApplicantID, a.ApplicantCode, a.CompanyID,
        a.FirstName, a.MiddleName, a.LastName,
        FullName = LTRIM(RTRIM(a.FirstName + ' ' + ISNULL(a.LastName, ''))),
        a.DateOfBirth, a.GenderID, a.NationalID, a.Email, a.MobileNumber,
        a.PhoneNumber, a.Cnic, a.CurrentAddress, a.CityID, a.CountryID,
        a.MaritalStatusID, a.ReligionID, a.TotalExperience,
        a.CurrentJobTitle, a.CurrentCompany, a.ExpectedSalary, a.NoticePeriod,
        a.Skills, a.Education, a.ExperienceSummary, a.ResumePath, a.CoverLetter,
        a.IsActive, a.CreatedBy, a.CreatedDate, a.UpdatedBy, a.UpdatedDate
    FROM dbo.Tbl_Ruc_Applicant a
    WHERE a.ApplicantID = @ApplicantID;
END
GO


/* ═══════════════════════════════════════════════════════════════════════════
   PROCEDURES — job applications
   ═══════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobApplication_Create
    @CompanyID           INT,
    @RequisitionID       INT,
    @ApplicantID         INT,
    @ApplicationDate     DATETIME      = NULL,
    @ApplicationSourceID INT           = NULL,
    @CurrentStatusID     INT           = NULL,
    @ResumePath          NVARCHAR(500) = NULL,
    @CoverLetter         NVARCHAR(MAX) = NULL,
    @Remarks             NVARCHAR(MAX) = NULL,
    @CreatedBy           NVARCHAR(100) = NULL,
    @ApplicationID       INT           OUTPUT,
    @ApplicationCode     NVARCHAR(50)  OUTPUT,
    @Result              INT           OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Result = 0;

    BEGIN TRY
        -- A candidate applies to a given opening once. Re-applying updates
        -- the existing row rather than creating a duplicate the recruiter
        -- would then have to reconcile by hand.
        SELECT @ApplicationID = ApplicationID
        FROM dbo.Tbl_Ruc_JobApplication
        WHERE RequisitionID = @RequisitionID AND ApplicantID = @ApplicantID;

        IF @ApplicationID IS NOT NULL
        BEGIN
            UPDATE dbo.Tbl_Ruc_JobApplication
            SET ResumePath  = ISNULL(@ResumePath, ResumePath),
                CoverLetter = ISNULL(@CoverLetter, CoverLetter),
                Remarks     = ISNULL(@Remarks, Remarks)
            WHERE ApplicationID = @ApplicationID;

            SELECT @ApplicationCode = ApplicationCode
            FROM dbo.Tbl_Ruc_JobApplication WHERE ApplicationID = @ApplicationID;

            SET @Result = 1;
            RETURN;
        END

        IF @CurrentStatusID IS NULL
            SELECT @CurrentStatusID = StatusID FROM ruc.Tbl_Status WHERE StatusCode = 'APPLIED';

        INSERT INTO dbo.Tbl_Ruc_JobApplication
            (CompanyID, RequisitionID, ApplicantID, ApplicationDate,
             ApplicationSourceID, CurrentStatusID, ResumePath, CoverLetter,
             Remarks, IsActive, CreatedBy, CreatedDate)
        VALUES
            (@CompanyID, @RequisitionID, @ApplicantID, ISNULL(@ApplicationDate, GETDATE()),
             @ApplicationSourceID, @CurrentStatusID, @ResumePath, @CoverLetter,
             @Remarks, 1, @CreatedBy, GETDATE());

        SET @ApplicationID   = CAST(SCOPE_IDENTITY() AS INT);
        SET @ApplicationCode = 'APP' + RIGHT('000000' + CAST(@ApplicationID AS VARCHAR(10)), 6);

        UPDATE dbo.Tbl_Ruc_JobApplication
        SET ApplicationCode = @ApplicationCode
        WHERE ApplicationID = @ApplicationID;

        SET @Result = 1;
    END TRY
    BEGIN CATCH
        SET @Result = 0;
    END CATCH
END
GO


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

        -- Denormalised so the Applications grid renders without extra round
        -- trips. The list is the busiest screen in the module.
        FullName     = LTRIM(RTRIM(a.FirstName + ' ' + ISNULL(a.LastName, ''))),
        Email        = a.Email,
        MobileNumber = a.MobileNumber,
        ApplicantCode = a.ApplicantCode,
        JobTitle       = r.JobTitle,
        RequisitionCode = r.RequisitionCode,
        DepartmentName  = r.DepartmentName
    FROM dbo.Tbl_Ruc_JobApplication ap
    LEFT JOIN dbo.Tbl_Ruc_Applicant a               ON a.ApplicantID   = ap.ApplicantID
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition r  ON r.RequisitionID = ap.RequisitionID
    LEFT JOIN ruc.Tbl_Status s                      ON s.StatusID      = ap.CurrentStatusID
    WHERE ap.CompanyID = @CompanyID
      AND (@RequisitionID   IS NULL OR ap.RequisitionID   = @RequisitionID)
      AND (@ApplicantID     IS NULL OR ap.ApplicantID     = @ApplicantID)
      AND (@CurrentStatusID IS NULL OR ap.CurrentStatusID = @CurrentStatusID)
      AND (@ApplicationID   IS NULL OR ap.ApplicationID   = @ApplicationID)
      AND (@IsActive        IS NULL OR ap.IsActive        = @IsActive)
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
      AND (@IsActive        IS NULL OR ap.IsActive        = @IsActive)
      AND (@SearchTerm      IS NULL OR a.FirstName LIKE '%' + @SearchTerm + '%'
                                    OR a.LastName  LIKE '%' + @SearchTerm + '%'
                                    OR a.Email     LIKE '%' + @SearchTerm + '%'
                                    OR r.JobTitle  LIKE '%' + @SearchTerm + '%');
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
        FullName     = LTRIM(RTRIM(a.FirstName + ' ' + ISNULL(a.LastName, ''))),
        Email        = a.Email,
        MobileNumber = a.MobileNumber,
        ApplicantCode = a.ApplicantCode,
        JobTitle = r.JobTitle,
        RequisitionCode = r.RequisitionCode,
        DepartmentName  = r.DepartmentName
    FROM dbo.Tbl_Ruc_JobApplication ap
    LEFT JOIN dbo.Tbl_Ruc_Applicant a              ON a.ApplicantID   = ap.ApplicantID
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition r ON r.RequisitionID = ap.RequisitionID
    LEFT JOIN ruc.Tbl_Status s                     ON s.StatusID      = ap.CurrentStatusID
    WHERE ap.ApplicationID = @ApplicationID;
END
GO


/* ═══════════════════════════════════════════════════════════════════════════
   PROCEDURES — lookups
   ═══════════════════════════════════════════════════════════════════════════ */

CREATE OR ALTER PROCEDURE ruc.SP_Ruc_Status_GetAll
    @StatusTypeCode NVARCHAR(50) = NULL,
    @IsActive       BIT          = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        s.StatusID, s.StatusTypeID, s.StatusCode, s.StatusName,
        s.Description, s.DisplayOrder, s.IsActive,
        StatusTypeCode = t.TypeCode,
        StatusTypeName = t.TypeName
    FROM ruc.Tbl_Status s
    INNER JOIN ruc.Tbl_StatusType t ON t.StatusTypeID = s.StatusTypeID
    WHERE (@StatusTypeCode IS NULL OR t.TypeCode = @StatusTypeCode)
      AND (@IsActive       IS NULL OR s.IsActive = @IsActive)
    ORDER BY t.TypeCode, s.DisplayOrder;
END
GO


CREATE OR ALTER PROCEDURE ruc.SP_Ruc_StatusType_GetAll
    @IsActive BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StatusTypeID, TypeCode, TypeName, Description, IsActive,
           CreatedBy, CreatedOn, UpdatedBy, UpdatedOn
    FROM ruc.Tbl_StatusType
    WHERE (@IsActive IS NULL OR IsActive = @IsActive)
    ORDER BY TypeCode;
END
GO


CREATE OR ALTER PROCEDURE ruc.SP_Recruitment_GetApplicationSources
AS
BEGIN
    SET NOCOUNT ON;

    -- Static in the demo; a lookup table in production.
    SELECT * FROM (VALUES
        (1, 'CAREERS',  'Careers Page'),
        (2, 'JOBBANK',  'Job Bank'),
        (3, 'REFERRAL', 'Employee Referral'),
        (4, 'MANUAL',   'Added by Recruiter')
    ) AS s(ApplicationSourceID, SourceCode, SourceName);
END
GO


PRINT '';
PRINT '─────────────────────────────────────────────────────────────';
PRINT ' [ruc] demo schema ready.';
PRINT '';
PRINT ' The hiring loop should now work end to end:';
PRINT '   generate JD -> save -> publish -> public careers page';
PRINT '   -> apply -> resume parsed -> Applications Management';
PRINT '';
PRINT ' Interviews, evaluations and panel scoring are NOT included.';
PRINT '─────────────────────────────────────────────────────────────';
GO

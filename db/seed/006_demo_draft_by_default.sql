/* =============================================================================
   DEMO — requisitions are created as DRAFTS. NOT THE REAL SCHEMA. DO NOT SHIP.
   =============================================================================

   The bug
   -------
   sp_Hr_Ruc_RecruitmentRequisition_Insert hardcoded the publish state:

       ... , 1, GETDATE(), @ClosingDate, 1, 'PUBLISHED', 'Published', ...

   So EVERY requisition was born published and immediately visible on the
   public careers page — while the wizard's own footer told the user
   "Saving creates the requisition as a Draft". The screen and the database
   disagreed, and the database won silently.

   That matters beyond tidiness. Publishing is the moment an AI-drafted advert
   becomes public. The whole integration is built so a human makes that call:
   the AI service returns status "Draft", the client re-asserts it, and the UI
   marks the draft for review. Auto-publishing on save quietly bypassed all of
   it.

   The fix
   -------
   Default to DRAFT. Publishing stays a separate, deliberate call to
   ruc.SP_Recruitment_PublishRequisition — either from the Job Requisitions
   grid, or from the wizard's explicit "Save & Publish" button, which simply
   performs both steps in order.

   Also fixed here: @JobSummary was never inserted, so the AI's summary was
   dropped and the careers page had nothing to show under "About the role".

   Prepared by Syed Taha Zaidi, Multinet.
============================================================================= */

USE [InternDB];
GO

CREATE OR ALTER PROCEDURE dbo.sp_Hr_Ruc_RecruitmentRequisition_Insert
    @CompanyID                          INT,
    @RecruitmentRequisitionName         NVARCHAR(500)  = NULL,
    @JobSummary                         NVARCHAR(MAX)  = NULL,
    @KeyResponsibilities                NVARCHAR(MAX)  = NULL,
    @SkillsRequired                     NVARCHAR(MAX)  = NULL,
    @QualificationsEntryRequirments     NVARCHAR(MAX)  = NULL,
    @OtherRequirments                   NVARCHAR(MAX)  = NULL,
    @Location                           NVARCHAR(200)  = NULL,
    @Vacancies                          INT            = 1,
    @EmploymentTypeID                   INT            = NULL,
    @GradeID                            INT            = NULL,
    @DepartmentID                       INT            = NULL,
    @DesignationID                      INT            = NULL,
    @JobCategoryID                      INT            = NULL,
    @ExperienceYears                    NVARCHAR(100)  = NULL,
    @RecruitmentRequisitionDate         DATETIME       = NULL,
    @RecruitmentRequisitionClosingDate  DATETIME       = NULL,

    /* Draft unless a caller explicitly asks otherwise. Nothing in the app
       passes this today — publishing goes through the dedicated procedure —
       but it exists so a future "create and publish in one step" has a path
       that does not mean re-hardcoding the old behaviour. */
    @IsPublished                        BIT            = 0,

    /* Accepted and ignored. The C# repository passes a wide parameter set
       shared with the production procedure; SQL Server errors on any it does
       not declare, so they are absorbed here rather than pruned from senior
       devs' code. */
    @AgeText NVARCHAR(100) = NULL, @AlwaysPublished BIT = NULL,
    @ApprovalStatus NVARCHAR(50) = NULL, @AttachedDocument NVARCHAR(500) = NULL,
    @AttachmentURL NVARCHAR(MAX) = NULL, @BudgetPeriodId INT = NULL,
    @ClusterId INT = NULL, @CommenceWorkOn DATETIME = NULL,
    @Comments NVARCHAR(MAX) = NULL, @EducationalQualifications NVARCHAR(MAX) = NULL,
    @EducationalQualificationsDesirable NVARCHAR(MAX) = NULL,
    @EmployeeCode NVARCHAR(100) = NULL, @EmployeeID INT = NULL,
    @Exposure NVARCHAR(MAX) = NULL, @IsClosed BIT = 0,
    @IsSystemDefault BIT = NULL, @JdId INT = NULL,
    @Justification NVARCHAR(MAX) = NULL, @JustificationBy NVARCHAR(100) = NULL,
    @JustificationDate DATETIME = NULL, @KeyDeliverables NVARCHAR(MAX) = NULL,
    @ModuleId INT = NULL, @NewPublishNotifiedToAll BIT = NULL,
    @ObjectId INT = NULL, @PublishStatus NVARCHAR(50) = NULL,
    @PublishedBy NVARCHAR(100) = NULL, @PublishedDate DATETIME = NULL,
    @Replacement BIT = NULL, @ReplacementEmpType NVARCHAR(100) = NULL,
    @ReportingPersonCode NVARCHAR(100) = NULL, @RequestId INT = NULL,
    @RequiredExperiences NVARCHAR(MAX) = NULL,
    @RequiredExperiencesDesirable NVARCHAR(MAX) = NULL,
    @RequiredTrainings NVARCHAR(MAX) = NULL,
    @RequiredTrainingsDesirable NVARCHAR(MAX) = NULL,
    @Salary DECIMAL(18,2) = NULL, @SpecialAttributes NVARCHAR(MAX) = NULL,
    @Status NVARCHAR(50) = NULL, @TechnicalCompetencies NVARCHAR(MAX) = NULL,
    @ToExternal BIT = NULL, @ToInternal BIT = NULL, @ToThirdParty BIT = NULL,

    @NewID     INT           OUTPUT,
    @IsSuccess BIT           OUTPUT,
    @Message   NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StatusID INT, @StatusCode NVARCHAR(50), @StatusName NVARCHAR(100);

    SELECT TOP 1 @StatusID = StatusID, @StatusCode = StatusCode, @StatusName = StatusName
    FROM ruc.Tbl_Status
    WHERE StatusCode = CASE WHEN @IsPublished = 1 THEN 'PUBLISHED' ELSE 'DRAFT' END;

    -- Experience arrives as free text ("3 - 6 years"). Pull the numbers out so
    -- the careers page can render a range instead of echoing the raw string.
    DECLARE @MinExp INT = NULL, @MaxExp INT = NULL;
    IF @ExperienceYears IS NOT NULL
    BEGIN
        DECLARE @digits TABLE (Seq INT IDENTITY(1,1), Val INT);
        INSERT INTO @digits (Val)
        SELECT TRY_CAST(value AS INT)
        FROM STRING_SPLIT(
                 REPLACE(REPLACE(REPLACE(REPLACE(@ExperienceYears, '-', ' '),
                         '+', ' '), 'years', ' '), 'yrs', ' '), ' ')
        WHERE TRY_CAST(value AS INT) IS NOT NULL;

        SELECT @MinExp = MIN(Val), @MaxExp = MAX(Val) FROM @digits;
        IF @MinExp = @MaxExp SET @MaxExp = NULL;
    END

    INSERT INTO dbo.Tbl_Ruc_RecruitmentRequisition
    (
        CompanyID, JobTitle, JobSummary, KeyResponsibilities, Skills, Qualifications,
        Benefits, Location, Vacancies, MinExperience, MaxExperience,
        EmploymentTypeID, GradeID, DepartmentID, DesignationID, JobCategoryID,
        IsPublished, PublishedDate, ClosingDate,
        StatusID, StatusCode, StatusName, IsActive, CreatedBy, CreatedOn
    )
    VALUES
    (
        @CompanyID,
        ISNULL(@RecruitmentRequisitionName, 'AI Generated Requisition'),
        @JobSummary,
        @KeyResponsibilities,
        @SkillsRequired,
        @QualificationsEntryRequirments,
        @OtherRequirments,
        @Location,
        ISNULL(@Vacancies, 1),
        @MinExp, @MaxExp,
        @EmploymentTypeID, @GradeID, @DepartmentID, @DesignationID, @JobCategoryID,

        -- Draft unless explicitly told otherwise. PublishedDate stays NULL
        -- until it is genuinely published, so "posted N days ago" on the
        -- careers page is truthful.
        @IsPublished,
        CASE WHEN @IsPublished = 1 THEN GETDATE() ELSE NULL END,
        @RecruitmentRequisitionClosingDate,

        @StatusID, @StatusCode, @StatusName,
        1,
        ISNULL(@EmployeeCode, 'System'),
        GETDATE()
    );

    SET @NewID = SCOPE_IDENTITY();
    SET @IsSuccess = 1;
    SET @Message = CASE WHEN @IsPublished = 1
                        THEN 'Requisition created and published'
                        ELSE 'Requisition saved as draft' END;

    SELECT
        RequisitionID, RequisitionCode, CompanyID, JobTitle, JobSummary,
        DepartmentID, DepartmentName, DesignationID, EmploymentTypeID, Vacancies,
        Location, MinExperience, MaxExperience,
        KeyResponsibilities, Skills, Qualifications, Benefits,
        IsPublished, PublishedDate, ClosingDate,
        StatusID, StatusCode, StatusName, IsActive, CreatedBy, CreatedOn
    FROM dbo.Tbl_Ruc_RecruitmentRequisition
    WHERE RequisitionID = @NewID;
END
GO

PRINT 'sp_Hr_Ruc_RecruitmentRequisition_Insert: now creates DRAFTS, keeps JobSummary.';
GO

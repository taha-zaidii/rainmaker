/* =============================================================================
   DEMO — fixes found by an end-to-end realism audit of the recruitment AI
   integration (frontend → backend → repository → stored procedure → table).
   NOT THE REAL SCHEMA. DO NOT SHIP.
   =============================================================================

   Three C# repository methods were calling stored procedures that either did
   not exist, or existed under the same name as a DIFFERENT method's
   incompatible parameter list — both throw a SqlException on every call
   against a freshly-seeded database. This file adds the missing procedures.

   1. ruc.SP_Ruc_RecruitmentAI_FeatureSettings_Save
      SaveSettingsAsync (feature toggles: auto-screening/matching/parsing,
      generate-questions, email notifications, auto-shortlist threshold) was
      calling ruc.SP_Ruc_RecruitmentAI_Settings_Save — the SP that belongs to
      SaveApiKeySettingsAsync (provider/key/endpoint/model). Passing feature-
      toggle parameters into the key-settings SP throws (missing
      @CreatedBy/@UpdatedBy, unknown @AutoParse/@AutoShortlistThreshold/
      @Result). Worse than the throw: had the parameter names happened to
      line up, every feature-toggle save would have overwritten the
      company's Provider/ApiKey/ApiEndpoint/Model with NULL. This is a new,
      separate SP that touches ONLY the six feature-toggle columns on an
      already-existing settings row.

   2. ruc.SP_Ruc_JobRequisition_CreateDetailed
   3. ruc.SP_Ruc_JobRequisition_UpdateDetailed
      CreateJobRequisitionAsync/UpdateJobRequisitionAsync (the newer,
      FK-normalized requisition DTOs — DepartmentID/DesignationID/
      EmploymentTypeID/GradeID as ints, not free text) were calling
      ruc.SP_Ruc_JobRequisition_Create / _Update, the SAME names SaveAsync/
      UpdateAsync use for the older, denormalized production-mirroring shape
      (RecruitmentRequisitionName, AgeText, QualificationsEntryRequirments,
      ...). SQL Server has one parameter list per procedure name; only the
      legacy shape survived. These two are new procedures under distinct
      names for the normalized DTOs, following the same DRAFT-by-default
      discipline as sp_Hr_Ruc_RecruitmentRequisition_Insert (006) — an AI
      draft or a human's in-progress requisition must never be silently
      published.

   Also fixed alongside (see RecruitmentRepository.cs):
   - AutoShortlistCandidateAsync was calling a nonexistent
     ruc.SP_Recruitment_AutoShortlistCandidate; the real procedure for this
     (with a different parameter list) is ruc.SP_AI_AutoShortlistCandidate,
     already defined in 003_demo_recruitment_sps.sql. Fixed at the call site,
     no new SP needed.
   - Two Dapper parameter names carried trailing whitespace
     ("@IsNonBudget ", "@IsPublic  ") and one was missing its "@" prefix
     ("IsPublic") in CreateJobRequisitionAsync/UpdateJobRequisitionAsync.

   Confirmed OUT OF SCOPE for this file (matches CLAUDE.md §12's own caveat
   that only the AI-settings/job-description/activity paths are demo-ready):
   interview scheduling, panel assignment, evaluations, and the hire flow
   call stored procedures with no definition anywhere in db/seed and no
   underlying schema for the tables they would need (e.g. employee
   onboarding). Building those out means designing a schema against a spec
   this workspace does not have — the real InternDB.bak is still required
   for that surface, exactly as already documented.

   Prepared by Syed Taha Zaidi, Multinet.
============================================================================= */

USE [InternDB];
GO

-- ============================================================================
-- 1. Feature-settings-only save — never touches Provider/ApiKey/ApiEndpoint/Model
-- ============================================================================
CREATE OR ALTER PROCEDURE ruc.SP_Ruc_RecruitmentAI_FeatureSettings_Save
    @CompanyID INT,
    @AutoScreening BIT,
    @AutoMatching BIT,
    @AutoParse BIT,
    @GenerateQuestions BIT,
    @EmailNotifications BIT,
    @AutoShortlistThreshold INT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Tbl_Ruc_RecruitmentAI_Settings WHERE CompanyID = @CompanyID AND IsActive = 1)
    BEGIN
        -- A company must configure its AI provider (Provider/ApiKey) before it
        -- has anything to toggle features on; there is no sensible row to
        -- create here without those required, NOT NULL columns.
        SET @Result = 0;
        RETURN;
    END

    UPDATE Tbl_Ruc_RecruitmentAI_Settings
    SET AutoScreening = @AutoScreening,
        AutoMatching = @AutoMatching,
        AutoParse = @AutoParse,
        GenerateQuestions = @GenerateQuestions,
        EmailNotifications = @EmailNotifications,
        AutoShortlistThreshold = @AutoShortlistThreshold,
        UpdatedOn = GETDATE()
    WHERE CompanyID = @CompanyID AND IsActive = 1;

    SET @Result = 1;
END
GO

PRINT 'Created ruc.SP_Ruc_RecruitmentAI_FeatureSettings_Save.';
GO

-- ============================================================================
-- 2. Job requisition create — FK-normalized shape (distinct from SaveAsync's)
-- ============================================================================
CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobRequisition_CreateDetailed
    @CompanyID INT,
    @JobTitle NVARCHAR(250),
    @DepartmentID INT = NULL,
    @DesignationID INT = NULL,
    @EmploymentTypeID INT = NULL,
    @GradeID INT = NULL,
    @Vacancies INT = 1,
    @MinExperience INT = NULL,
    @MaxExperience INT = NULL,
    @MinAge INT = NULL,
    @MaxAge INT = NULL,
    @MinSalary DECIMAL(18,2) = NULL,
    @MaxSalary DECIMAL(18,2) = NULL,
    @Location NVARCHAR(250) = NULL,
    @ReportingTo INT = NULL,
    @KeyResponsibilities NVARCHAR(MAX) = NULL,
    @Requirements NVARCHAR(MAX) = NULL,
    @Qualifications NVARCHAR(MAX) = NULL,
    @Skills NVARCHAR(MAX) = NULL,
    @Benefits NVARCHAR(MAX) = NULL,
    @Justification NVARCHAR(MAX) = NULL,
    @IsPublished BIT = 0,
    @PublishedDate DATETIME = NULL,
    @ClosingDate DATETIME = NULL,
    @StatusID INT = NULL,
    @JobCategoryID INT = NULL,
    @Isbudget BIT = 0,
    @IsNonBudget BIT = 0,
    @IsPublic BIT = 1,
    @IsDefault BIT = 0,
    @SalaryRecommendationID INT = NULL,
    @CreatedBy NVARCHAR(100) = NULL,
    @RequisitionID INT OUTPUT,
    @RequisitionCode NVARCHAR(50) OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedStatusID INT = @StatusID, @ResolvedStatusCode NVARCHAR(50), @ResolvedStatusName NVARCHAR(100);

    -- Age is a protected attribute end to end: age_limits is null-by-design
    -- coming out of AI generation (see CLAUDE.md §10), and it stays optional
    -- and human-entered here too — never defaulted, never inferred.
    IF @ResolvedStatusID IS NULL
    BEGIN
        SELECT TOP 1 @ResolvedStatusID = StatusID, @ResolvedStatusCode = StatusCode, @ResolvedStatusName = StatusName
        FROM ruc.Tbl_Status
        WHERE StatusCode = CASE WHEN @IsPublished = 1 THEN 'PUBLISHED' ELSE 'DRAFT' END;
    END
    ELSE
    BEGIN
        SELECT @ResolvedStatusCode = StatusCode, @ResolvedStatusName = StatusName
        FROM ruc.Tbl_Status WHERE StatusID = @ResolvedStatusID;
    END

    INSERT INTO dbo.Tbl_Ruc_RecruitmentRequisition
    (
        CompanyID, JobTitle, DepartmentID, DesignationID, EmploymentTypeID, GradeID,
        Vacancies, MinExperience, MaxExperience, MinAge, MaxAge, MinSalary, MaxSalary,
        Location, ReportingTo, KeyResponsibilities, Requirements, Qualifications, Skills,
        Benefits, Justification, IsPublished, PublishedDate, ClosingDate,
        StatusID, StatusCode, StatusName, JobCategoryID, Isbudget, IsNonBudget,
        IsPublic, IsDefault, SalaryRecommendationID, IsActive, CreatedBy, CreatedOn
    )
    VALUES
    (
        @CompanyID, @JobTitle, @DepartmentID, @DesignationID, @EmploymentTypeID, @GradeID,
        ISNULL(@Vacancies, 1), @MinExperience, @MaxExperience, @MinAge, @MaxAge, @MinSalary, @MaxSalary,
        @Location, @ReportingTo, @KeyResponsibilities, @Requirements, @Qualifications, @Skills,
        @Benefits, @Justification, @IsPublished,
        CASE WHEN @IsPublished = 1 THEN ISNULL(@PublishedDate, GETDATE()) ELSE NULL END,
        @ClosingDate,
        @ResolvedStatusID, @ResolvedStatusCode, @ResolvedStatusName, @JobCategoryID, @Isbudget, @IsNonBudget,
        @IsPublic, @IsDefault, @SalaryRecommendationID, 1, ISNULL(@CreatedBy, 'System'), GETDATE()
    );

    SET @RequisitionID = CAST(SCOPE_IDENTITY() AS INT);
    SET @RequisitionCode = 'REQ-' + RIGHT('000000' + CAST(@RequisitionID AS VARCHAR(10)), 6);

    UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
    SET RequisitionCode = @RequisitionCode
    WHERE RequisitionID = @RequisitionID;

    SET @Result = 1;
END
GO

PRINT 'Created ruc.SP_Ruc_JobRequisition_CreateDetailed.';
GO

-- ============================================================================
-- 3. Job requisition update — pairs with CreateDetailed above
-- ============================================================================
CREATE OR ALTER PROCEDURE ruc.SP_Ruc_JobRequisition_UpdateDetailed
    @RequisitionID INT,
    @CompanyID INT,
    @JobTitle NVARCHAR(250),
    @DepartmentID INT = NULL,
    @DesignationID INT = NULL,
    @EmploymentTypeID INT = NULL,
    @GradeID INT = NULL,
    @Vacancies INT = 1,
    @MinExperience INT = NULL,
    @MaxExperience INT = NULL,
    @MinAge INT = NULL,
    @MaxAge INT = NULL,
    @MinSalary DECIMAL(18,2) = NULL,
    @MaxSalary DECIMAL(18,2) = NULL,
    @Location NVARCHAR(250) = NULL,
    @ReportingTo INT = NULL,
    @KeyResponsibilities NVARCHAR(MAX) = NULL,
    @Requirements NVARCHAR(MAX) = NULL,
    @Qualifications NVARCHAR(MAX) = NULL,
    @Skills NVARCHAR(MAX) = NULL,
    @Benefits NVARCHAR(MAX) = NULL,
    @Justification NVARCHAR(MAX) = NULL,
    @IsPublic BIT = 1,
    @IsPublished BIT = 0,
    @PublishedDate DATETIME = NULL,
    @ClosingDate DATETIME = NULL,
    @StatusID INT = NULL,
    @JobCategoryID INT = NULL,
    @Isbudget BIT = 0,
    @IsNonBudget BIT = 0,
    @SalaryRecommendationID INT = NULL,
    @IsActive BIT = 1,
    @UpdatedBy NVARCHAR(100) = NULL,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tbl_Ruc_RecruitmentRequisition WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID)
    BEGIN
        SET @Result = 0;
        RETURN;
    END

    DECLARE @ResolvedStatusID INT = @StatusID, @ResolvedStatusCode NVARCHAR(50), @ResolvedStatusName NVARCHAR(100);

    IF @ResolvedStatusID IS NOT NULL
    BEGIN
        SELECT @ResolvedStatusCode = StatusCode, @ResolvedStatusName = StatusName
        FROM ruc.Tbl_Status WHERE StatusID = @ResolvedStatusID;
    END

    UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
    SET JobTitle = @JobTitle,
        DepartmentID = @DepartmentID,
        DesignationID = @DesignationID,
        EmploymentTypeID = @EmploymentTypeID,
        GradeID = @GradeID,
        Vacancies = ISNULL(@Vacancies, Vacancies),
        MinExperience = @MinExperience,
        MaxExperience = @MaxExperience,
        MinAge = @MinAge,
        MaxAge = @MaxAge,
        MinSalary = @MinSalary,
        MaxSalary = @MaxSalary,
        Location = @Location,
        ReportingTo = @ReportingTo,
        KeyResponsibilities = @KeyResponsibilities,
        Requirements = @Requirements,
        Qualifications = @Qualifications,
        Skills = @Skills,
        Benefits = @Benefits,
        Justification = @Justification,
        IsPublic = @IsPublic,
        IsPublished = @IsPublished,
        PublishedDate = CASE WHEN @IsPublished = 1 THEN COALESCE(@PublishedDate, PublishedDate, GETDATE()) ELSE PublishedDate END,
        ClosingDate = @ClosingDate,
        StatusID = ISNULL(@ResolvedStatusID, StatusID),
        StatusCode = ISNULL(@ResolvedStatusCode, StatusCode),
        StatusName = ISNULL(@ResolvedStatusName, StatusName),
        JobCategoryID = @JobCategoryID,
        Isbudget = @Isbudget,
        IsNonBudget = @IsNonBudget,
        SalaryRecommendationID = @SalaryRecommendationID,
        IsActive = @IsActive,
        UpdatedBy = @UpdatedBy,
        UpdatedOn = GETDATE()
    WHERE RequisitionID = @RequisitionID AND CompanyID = @CompanyID;

    SET @Result = 1;
END
GO

PRINT 'Created ruc.SP_Ruc_JobRequisition_UpdateDetailed.';
GO

-- ============================================================================
-- 4. Activity feed insert — was raw inline SQL, only write in this repository
--    that still bypassed the stored-procedure-only convention against a table
--    that actually exists (Phase 4 hardening: RecruitmentAIRepository audit).
-- ============================================================================
CREATE OR ALTER PROCEDURE ruc.SP_Ruc_RecruitmentAI_Activity_Save
    @CompanyID INT,
    @ActivityType NVARCHAR(100),
    @Title NVARCHAR(500),
    @Description NVARCHAR(MAX),
    @RelatedId INT = NULL,
    @Id INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tbl_Ruc_RecruitmentAI_Activity
        (CompanyID, ActivityType, Title, Description, RelatedId, CreatedOn)
    VALUES
        (@CompanyID, @ActivityType, @Title, @Description, @RelatedId, GETDATE());

    SET @Id = CAST(SCOPE_IDENTITY() AS INT);
END
GO

PRINT 'Created ruc.SP_Ruc_RecruitmentAI_Activity_Save.';
GO

PRINT '';
PRINT '─────────────────────────────────────────────────────';
PRINT ' Realism-audit fixes applied: feature-settings save no';
PRINT ' longer collides with API-key save; requisition create/';
PRINT ' update have their own SPs, no longer colliding with';
PRINT ' SaveAsync/UpdateAsync''s legacy shape.';
PRINT '─────────────────────────────────────────────────────';
GO

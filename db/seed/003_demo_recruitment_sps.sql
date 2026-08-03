/* =============================================================================
   DEMO RECRUITMENT SPs & TABLES — FOR LOCAL DEMO ENVIRONMENT
   ============================================================================= */

USE [InternDB];
GO

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'ruc')
BEGIN
    EXEC('CREATE SCHEMA [ruc]');
    PRINT 'Created schema [ruc].';
END
GO

-- 1. Table: Tbl_Ruc_RecruitmentRequisition
IF OBJECT_ID('dbo.Tbl_Ruc_RecruitmentRequisition', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_Ruc_RecruitmentRequisition
    (
        RequisitionID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RequisitionCode NVARCHAR(50) NULL,
        CompanyID INT NOT NULL,
        JobTitle NVARCHAR(250) NOT NULL,
        JobSummary NVARCHAR(MAX) NULL,
        DepartmentID INT NULL,
        DepartmentName NVARCHAR(200) NULL,
        DesignationID INT NULL,
        DesignationName NVARCHAR(200) NULL,
        EmploymentTypeID INT NULL,
        EmploymentTypeName NVARCHAR(100) NULL,
        GradeID INT NULL,
        Vacancies INT NOT NULL DEFAULT (1),
        MinExperience INT NULL,
        MaxExperience INT NULL,
        MinAge INT NULL,
        MaxAge INT NULL,
        MinSalary DECIMAL(18,2) NULL,
        MaxSalary DECIMAL(18,2) NULL,
        Location NVARCHAR(250) NULL,
        ReportingTo INT NULL,
        KeyResponsibilities NVARCHAR(MAX) NULL,
        Requirements NVARCHAR(MAX) NULL,
        Qualifications NVARCHAR(MAX) NULL,
        Skills NVARCHAR(MAX) NULL,
        Benefits NVARCHAR(MAX) NULL,
        Justification NVARCHAR(MAX) NULL,
        IsPublished BIT NOT NULL DEFAULT (1),
        PublishedDate DATETIME NULL DEFAULT (GETDATE()),
        ClosingDate DATETIME NULL,
        StatusID INT NULL DEFAULT (1),
        StatusCode NVARCHAR(50) NULL DEFAULT ('PUBLISHED'),
        StatusName NVARCHAR(100) NULL DEFAULT ('Published'),
        JobCategoryID INT NULL,
        Isbudget BIT NULL DEFAULT (0),
        IsNonBudget BIT NULL DEFAULT (0),
        IsActive BIT NOT NULL DEFAULT (1),
        IsDefault BIT NULL DEFAULT (0),
        IsPublic BIT NULL DEFAULT (1),
        CreatedBy NVARCHAR(100) NULL,
        CreatedOn DATETIME NULL DEFAULT (GETDATE()),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedOn DATETIME NULL,
        SalaryRecommendationID INT NULL,
        TotalApplications INT NULL DEFAULT (0),
        FilePath NVARCHAR(500) NULL
    );
    PRINT 'Created table Tbl_Ruc_RecruitmentRequisition.';
END
GO

-- 2. Table: Tbl_Ruc_JobApplication
IF OBJECT_ID('dbo.Tbl_Ruc_JobApplication', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_Ruc_JobApplication
    (
        ApplicationID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ApplicationCode NVARCHAR(50) NULL,
        CompanyID INT NOT NULL,
        RequisitionID INT NOT NULL,
        ApplicantID INT NOT NULL DEFAULT(1),
        ApplicationDate DATETIME NULL DEFAULT(GETDATE()),
        ApplicationSourceID INT NULL DEFAULT(1),
        CurrentStatusID INT NULL DEFAULT(1),
        StatusID INT NULL DEFAULT(1),
        StatusCode NVARCHAR(50) NULL DEFAULT('APPLIED'),
        StatusName NVARCHAR(100) NULL DEFAULT('Applied'),
        ApplyCode NVARCHAR(50) NULL,
        ApplicantStatus NVARCHAR(50) NULL DEFAULT('New'),
        ResumePath NVARCHAR(500) NULL,
        CoverLetter NVARCHAR(MAX) NULL,
        ScreeningScore DECIMAL(5,2) NULL,
        OverallRating DECIMAL(5,2) NULL,
        Recommendation NVARCHAR(MAX) NULL,
        RejectionReason NVARCHAR(MAX) NULL,
        OfferLetterPath NVARCHAR(500) NULL,
        OfferAccepted BIT NULL,
        Remarks NVARCHAR(MAX) NULL,
        ResumeParsingID INT NULL,
        CandidateRankingID INT NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedBy NVARCHAR(100) NULL,
        CreatedDate DATETIME NULL DEFAULT(GETDATE()),
        UpdatedBy NVARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,
        FullName NVARCHAR(200) NULL,
        Email NVARCHAR(200) NULL,
        MobileNumber NVARCHAR(50) NULL
    );
    PRINT 'Created table Tbl_Ruc_JobApplication.';
END
GO

-- 3. Table: Tbl_RecruitmentAI_ResumeParsing
IF OBJECT_ID('dbo.Tbl_RecruitmentAI_ResumeParsing', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_RecruitmentAI_ResumeParsing
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID INT NOT NULL,
        ApplicantID INT NULL,
        ApplicationID INT NULL,
        ResumeFileName NVARCHAR(250) NULL,
        ResumeFilePath NVARCHAR(500) NULL,
        FileType NVARCHAR(50) NULL,
        FileSize BIGINT NULL,
        ParsedData NVARCHAR(MAX) NULL,
        ParsedResumeText NVARCHAR(MAX) NULL,
        ParsingMethod NVARCHAR(50) NULL DEFAULT('AI'),
        ParsingProvider NVARCHAR(50) NULL DEFAULT('multinetai'),
        ParsingModel NVARCHAR(200) NULL DEFAULT('qwen3.5:27b'),
        ParsingStatus NVARCHAR(50) NULL DEFAULT('Success'),
        ParsingConfidence DECIMAL(5,2) NULL,
        ParsingErrors NVARCHAR(MAX) NULL,
        TokensUsed INT NULL DEFAULT(0),
        ProcessingTime INT NULL DEFAULT(0),
        CreatedBy NVARCHAR(100) NULL DEFAULT('System'),
        CreatedOn DATETIME NULL DEFAULT(GETDATE()),
        UpdatedBy NVARCHAR(100) NULL DEFAULT('System'),
        UpdatedOn DATETIME NULL
    );
    PRINT 'Created table Tbl_RecruitmentAI_ResumeParsing.';
END
GO

-- 4. Table: Tbl_RecruitmentAI_Screening
IF OBJECT_ID('dbo.Tbl_RecruitmentAI_Screening', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_RecruitmentAI_Screening
    (
        ScreeningID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID INT NOT NULL,
        ApplicationID INT NOT NULL,
        ApplicantID INT NOT NULL,
        RequisitionID INT NOT NULL,
        ResumeParsingID INT NULL,
        MatchScore INT NULL DEFAULT(0),
        Recommendation NVARCHAR(MAX) NULL,
        SkillsMatch NVARCHAR(MAX) NULL,
        ExperienceMatch NVARCHAR(MAX) NULL,
        QualificationsMatch NVARCHAR(MAX) NULL,
        RedFlags NVARCHAR(MAX) NULL,
        ScreeningProvider NVARCHAR(50) NULL DEFAULT('AI'),
        ModelUsed NVARCHAR(200) NULL,
        TokensUsed INT NULL DEFAULT(0),
        ProcessingTime INT NULL DEFAULT(0),
        AutoShortlistThreshold INT NULL DEFAULT(80),
        AutoShortlisted BIT NULL DEFAULT(0),
        CreatedBy NVARCHAR(100) NULL DEFAULT('System'),
        CreatedOn DATETIME NULL DEFAULT(GETDATE())
    );
    PRINT 'Created table Tbl_RecruitmentAI_Screening.';
END
GO

/* =============================================================================
   STORED PROCEDURES
   ============================================================================= */

-- SP 1: [ruc].[SP_Ruc_JobRequisition_Create]
CREATE OR ALTER PROCEDURE [ruc].[SP_Ruc_JobRequisition_Create]
    @CompanyID INT,
    @JobTitle NVARCHAR(250),
    @JobSummary NVARCHAR(MAX) = NULL,
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
    @IsPublished BIT = 1,
    @PublishedDate DATETIME = NULL,
    @ClosingDate DATETIME = NULL,
    @StatusID INT = 1,
    @JobCategoryID INT = NULL,
    @Isbudget BIT = 0,
    @IsNonBudget BIT = 0,
    @IsPublic BIT = 1,
    @IsDefault BIT = 0,
    @SalaryRecommendationID INT = NULL,
    @CreatedBy NVARCHAR(100) = 'System',
    @RequisitionID INT OUTPUT,
    @RequisitionCode NVARCHAR(50) OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tbl_Ruc_RecruitmentRequisition
    (
        CompanyID, JobTitle, JobSummary, DepartmentID, DesignationID, EmploymentTypeID, GradeID,
        Vacancies, MinExperience, MaxExperience, MinAge, MaxAge, MinSalary, MaxSalary, Location,
        ReportingTo, KeyResponsibilities, Requirements, Qualifications, Skills, Benefits, Justification,
        IsPublished, PublishedDate, ClosingDate, StatusID, StatusCode, StatusName, JobCategoryID, Isbudget,
        IsNonBudget, IsPublic, IsDefault, SalaryRecommendationID, CreatedBy, CreatedOn
    )
    VALUES
    (
        @CompanyID, @JobTitle, @JobSummary, @DepartmentID, @DesignationID, @EmploymentTypeID, @GradeID,
        ISNULL(@Vacancies, 1), @MinExperience, @MaxExperience, @MinAge, @MaxAge, @MinSalary, @MaxSalary, @Location,
        @ReportingTo, @KeyResponsibilities, @Requirements, @Qualifications, @Skills, @Benefits, @Justification,
        ISNULL(@IsPublished, 1), ISNULL(@PublishedDate, GETDATE()), @ClosingDate, ISNULL(@StatusID, 1), 'PUBLISHED', 'Published', @JobCategoryID, ISNULL(@Isbudget, 0),
        ISNULL(@IsNonBudget, 0), ISNULL(@IsPublic, 1), ISNULL(@IsDefault, 0), @SalaryRecommendationID, ISNULL(@CreatedBy, 'System'), GETDATE()
    );

    SET @RequisitionID = SCOPE_IDENTITY();
    SET @RequisitionCode = 'REQ-' + RIGHT('00000' + CAST(@RequisitionID AS VARCHAR(10)), 5);

    UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
    SET RequisitionCode = @RequisitionCode
    WHERE RequisitionID = @RequisitionID;

    SET @Result = 1;
END
GO

-- SP 2: [sp_Hr_Ruc_RecruitmentRequisition_Insert]
CREATE OR ALTER PROCEDURE [dbo].[sp_Hr_Ruc_RecruitmentRequisition_Insert]
    @CompanyID INT,
    @EmployeeID INT = NULL,
    @RecruitmentRequisitionName NVARCHAR(250) = NULL,
    @BudgetPeriodId INT = NULL,
    @IsSystemDefault BIT = 0,
    @Location NVARCHAR(250) = NULL,
    @JobCategoryID INT = NULL,
    @DesignationID INT = NULL,
    @Vacancies INT = 1,
    @CommenceWorkOn DATETIME = NULL,
    @EmploymentTypeID INT = NULL,
    @GradeID INT = NULL,
    @AgeText NVARCHAR(100) = NULL,
    @ExperienceYears NVARCHAR(100) = NULL,
    @QualificationsEntryRequirments NVARCHAR(MAX) = NULL,
    @Exposure NVARCHAR(MAX) = NULL,
    @SkillsRequired NVARCHAR(MAX) = NULL,
    @SpecialAttributes NVARCHAR(MAX) = NULL,
    @Comments NVARCHAR(MAX) = NULL,
    @KeyResponsibilities NVARCHAR(MAX) = NULL,
    @KeyDeliverables NVARCHAR(MAX) = NULL,
    @OtherRequirments NVARCHAR(MAX) = NULL,
    @TechnicalCompetencies NVARCHAR(MAX) = NULL,
    @EducationalQualifications NVARCHAR(MAX) = NULL,
    @EducationalQualificationsDesirable NVARCHAR(MAX) = NULL,
    @RequiredExperiences NVARCHAR(MAX) = NULL,
    @RequiredExperiencesDesirable NVARCHAR(MAX) = NULL,
    @RequiredTrainings NVARCHAR(MAX) = NULL,
    @RequiredTrainingsDesirable NVARCHAR(MAX) = NULL,
    @AlwaysPublished BIT = 0,
    @PublishStatus NVARCHAR(50) = 'Published',
    @RecruitmentRequisitionDate DATETIME = NULL,
    @RecruitmentRequisitionClosingDate DATETIME = NULL,
    @PublishedBy NVARCHAR(100) = NULL,
    @PublishedDate DATETIME = NULL,
    @ApprovalStatus NVARCHAR(50) = 'Approved',
    @IsClosed BIT = 0,
    @AttachedDocument NVARCHAR(500) = NULL,
    @DepartmentID INT = NULL,
    @Salary DECIMAL(18,2) = NULL,
    @EmployeeCode NVARCHAR(100) = NULL,
    @AttachmentURL NVARCHAR(MAX) = NULL,
    @NewID INT OUTPUT,
    @IsSuccess BIT OUTPUT,
    @Message NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tbl_Ruc_RecruitmentRequisition
    (
        CompanyID, JobTitle, KeyResponsibilities, Skills, Qualifications, Benefits, Location, Vacancies,
        EmploymentTypeID, GradeID, DepartmentID, IsPublished, PublishedDate, ClosingDate, StatusID, StatusCode, StatusName, CreatedBy, CreatedOn
    )
    VALUES
    (
        @CompanyID, ISNULL(@RecruitmentRequisitionName, 'AI Generated Requisition'), @KeyResponsibilities, @SkillsRequired,
        @QualificationsEntryRequirments, @OtherRequirments, @Location, ISNULL(@Vacancies, 1), @EmploymentTypeID, @GradeID, @DepartmentID,
        1, GETDATE(), @RecruitmentRequisitionClosingDate, 1, 'PUBLISHED', 'Published', ISNULL(@EmployeeCode, 'System'), GETDATE()
    );

    SET @NewID = SCOPE_IDENTITY();
    SET @IsSuccess = 1;
    SET @Message = 'Requisition inserted successfully';

    SELECT 
        RequisitionID, RequisitionCode, CompanyID, JobTitle, JobSummary, DepartmentID, DesignationID,
        EmploymentTypeID, Vacancies, Location, KeyResponsibilities, Skills, Qualifications, Benefits,
        IsPublished, PublishedDate, ClosingDate, StatusID, StatusCode, StatusName, IsActive, CreatedBy, CreatedOn
    FROM dbo.Tbl_Ruc_RecruitmentRequisition
    WHERE RequisitionID = @NewID;
END
GO

-- SP 3: [ruc].[SP_Ruc_JobRequisition_GetAll]
CREATE OR ALTER PROCEDURE [ruc].[SP_Ruc_JobRequisition_GetAll]
    @CompanyID INT,
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @SearchTerm NVARCHAR(250) = NULL,
    @StatusID INT = NULL,
    @DepartmentID INT = NULL,
    @DesignationID INT = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM dbo.Tbl_Ruc_RecruitmentRequisition
    WHERE CompanyID = @CompanyID AND IsActive = 1
      AND (@SearchTerm IS NULL OR JobTitle LIKE '%' + @SearchTerm + '%')
      AND (@StatusID IS NULL OR StatusID = @StatusID)
      AND (@DepartmentID IS NULL OR DepartmentID = @DepartmentID);

    SELECT 
        RequisitionID, RequisitionCode, CompanyID, JobTitle, JobSummary, DepartmentID, DesignationID,
        EmploymentTypeID, GradeID, Vacancies, MinExperience, MaxExperience, MinAge, MaxAge, MinSalary, MaxSalary,
        Location, KeyResponsibilities, Requirements, Qualifications, Skills, Benefits, Justification,
        IsPublished, PublishedDate, ClosingDate, StatusID, StatusCode, StatusName, JobCategoryID, Isbudget, IsNonBudget,
        IsActive, IsDefault, IsPublic, CreatedBy, CreatedOn, TotalApplications
    FROM dbo.Tbl_Ruc_RecruitmentRequisition
    WHERE CompanyID = @CompanyID AND IsActive = 1
      AND (@SearchTerm IS NULL OR JobTitle LIKE '%' + @SearchTerm + '%')
      AND (@StatusID IS NULL OR StatusID = @StatusID)
      AND (@DepartmentID IS NULL OR DepartmentID = @DepartmentID)
    ORDER BY RequisitionID DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- SP 4: [ruc].[SP_Ruc_JobRequisition_GetById]
CREATE OR ALTER PROCEDURE [ruc].[SP_Ruc_JobRequisition_GetById]
    @RequisitionID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        RequisitionID, RequisitionCode, CompanyID, JobTitle, JobSummary, DepartmentID, DesignationID,
        EmploymentTypeID, GradeID, Vacancies, MinExperience, MaxExperience, MinAge, MaxAge, MinSalary, MaxSalary,
        Location, KeyResponsibilities, Requirements, Qualifications, Skills, Benefits, Justification,
        IsPublished, PublishedDate, ClosingDate, StatusID, StatusCode, StatusName, JobCategoryID, Isbudget, IsNonBudget,
        IsActive, IsDefault, IsPublic, CreatedBy, CreatedOn, TotalApplications
    FROM dbo.Tbl_Ruc_RecruitmentRequisition
    WHERE RequisitionID = @RequisitionID;
END
GO

-- SP 5: [RUC].[SP_Ruc_RecruitmentAI_ResumeParsing_Save]
CREATE OR ALTER PROCEDURE [RUC].[SP_Ruc_RecruitmentAI_ResumeParsing_Save]
    @CompanyID INT,
    @ApplicantID INT = NULL,
    @ApplicationID INT = NULL,
    @ResumeFileName NVARCHAR(250) = NULL,
    @ResumeFilePath NVARCHAR(500) = NULL,
    @FileType NVARCHAR(50) = NULL,
    @FileSize BIGINT = NULL,
    @ParsedData NVARCHAR(MAX) = NULL,
    @ParsedResumeText NVARCHAR(MAX) = NULL,
    @ParsingMethod NVARCHAR(50) = 'AI',
    @ParsingProvider NVARCHAR(50) = 'multinetai',
    @ParsingModel NVARCHAR(200) = 'qwen3.5:27b',
    @ParsingStatus NVARCHAR(50) = 'Success',
    @ParsingConfidence DECIMAL(5,2) = NULL,
    @ParsingErrors NVARCHAR(MAX) = NULL,
    @TokensUsed INT = 0,
    @ProcessingTime INT = 0,
    @CreatedBy NVARCHAR(100) = 'System',
    @UpdatedBy NVARCHAR(100) = 'System',
    @Id INT OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tbl_RecruitmentAI_ResumeParsing
    (
        CompanyID, ApplicantID, ApplicationID, ResumeFileName, ResumeFilePath, FileType, FileSize,
        ParsedData, ParsedResumeText, ParsingMethod, ParsingProvider, ParsingModel, ParsingStatus,
        ParsingConfidence, ParsingErrors, TokensUsed, ProcessingTime, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn
    )
    VALUES
    (
        @CompanyID, @ApplicantID, @ApplicationID, @ResumeFileName, @ResumeFilePath, @FileType, @FileSize,
        @ParsedData, @ParsedResumeText, ISNULL(@ParsingMethod, 'AI'), ISNULL(@ParsingProvider, 'multinetai'), ISNULL(@ParsingModel, 'qwen3.5:27b'),
        ISNULL(@ParsingStatus, 'Success'), @ParsingConfidence, @ParsingErrors, ISNULL(@TokensUsed, 0), ISNULL(@ProcessingTime, 0),
        ISNULL(@CreatedBy, 'System'), GETDATE(), ISNULL(@UpdatedBy, 'System'), GETDATE()
    );

    SET @Id = SCOPE_IDENTITY();
    SET @Result = 1;
END
GO

-- SP 6: [ruc].[SP_AI_AutoParseResume]
CREATE OR ALTER PROCEDURE [ruc].[SP_AI_AutoParseResume]
    @CompanyID INT,
    @ApplicationID INT = 0,
    @ApplicantID INT = 0,
    @ResumePath NVARCHAR(500) = NULL,
    @ResumeFileName NVARCHAR(250) = NULL,
    @ParsedData NVARCHAR(MAX) = NULL,
    @CreatedBy NVARCHAR(100) = 'System',
    @ParsingID INT OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tbl_RecruitmentAI_ResumeParsing
    (
        CompanyID, ApplicationID, ApplicantID, ResumeFilePath, ResumeFileName, ParsedData,
        ParsingMethod, ParsingProvider, ParsingModel, ParsingStatus, CreatedBy, CreatedOn
    )
    VALUES
    (
        @CompanyID, @ApplicationID, @ApplicantID, @ResumePath, @ResumeFileName, @ParsedData,
        'AI', 'multinetai', 'qwen3.5:27b', 'Success', ISNULL(@CreatedBy, 'System'), GETDATE()
    );

    SET @ParsingID = SCOPE_IDENTITY();
    SET @Result = 1;
END
GO

-- SP 7: [ruc].[SP_Ruc_JobApplication_Create]
CREATE OR ALTER PROCEDURE [ruc].[SP_Ruc_JobApplication_Create]
    @CompanyID INT,
    @RequisitionID INT,
    @ApplicantID INT = 1,
    @ApplicationDate DATETIME = NULL,
    @ApplicationSourceID INT = 1,
    @CurrentStatusID INT = 1,
    @StatusID INT = 1,
    @StatusCode NVARCHAR(50) = 'APPLIED',
    @StatusName NVARCHAR(100) = 'Applied',
    @ApplyCode NVARCHAR(50) = NULL,
    @ApplicantStatus NVARCHAR(50) = 'New',
    @ResumePath NVARCHAR(500) = NULL,
    @CoverLetter NVARCHAR(MAX) = NULL,
    @ScreeningScore DECIMAL(5,2) = NULL,
    @OverallRating DECIMAL(5,2) = NULL,
    @Recommendation NVARCHAR(MAX) = NULL,
    @RejectionReason NVARCHAR(MAX) = NULL,
    @OfferLetterPath NVARCHAR(500) = NULL,
    @OfferAccepted BIT = NULL,
    @Remarks NVARCHAR(MAX) = NULL,
    @ResumeParsingID INT = NULL,
    @CandidateRankingID INT = NULL,
    @CreatedBy NVARCHAR(100) = 'System',
    @FullName NVARCHAR(200) = NULL,
    @Email NVARCHAR(200) = NULL,
    @MobileNumber NVARCHAR(50) = NULL,
    @ApplicationID INT OUTPUT,
    @ApplicationCode NVARCHAR(50) OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Tbl_Ruc_JobApplication
    (
        CompanyID, RequisitionID, ApplicantID, ApplicationDate, ApplicationSourceID, CurrentStatusID, StatusID,
        StatusCode, StatusName, ApplyCode, ApplicantStatus, ResumePath, CoverLetter, ScreeningScore, OverallRating,
        Recommendation, RejectionReason, OfferLetterPath, OfferAccepted, Remarks, ResumeParsingID, CandidateRankingID,
        CreatedBy, CreatedDate, FullName, Email, MobileNumber
    )
    VALUES
    (
        @CompanyID, @RequisitionID, ISNULL(@ApplicantID, 1), ISNULL(@ApplicationDate, GETDATE()), ISNULL(@ApplicationSourceID, 1),
        ISNULL(@CurrentStatusID, 1), ISNULL(@StatusID, 1), ISNULL(@StatusCode, 'APPLIED'), ISNULL(@StatusName, 'Applied'),
        @ApplyCode, ISNULL(@ApplicantStatus, 'New'), @ResumePath, @CoverLetter, @ScreeningScore, @OverallRating,
        @Recommendation, @RejectionReason, @OfferLetterPath, @OfferAccepted, @Remarks, @ResumeParsingID, @CandidateRankingID,
        ISNULL(@CreatedBy, 'System'), GETDATE(), @FullName, @Email, @MobileNumber
    );

    SET @ApplicationID = SCOPE_IDENTITY();
    SET @ApplicationCode = 'APP-' + RIGHT('00000' + CAST(@ApplicationID AS VARCHAR(10)), 5);

    UPDATE dbo.Tbl_Ruc_JobApplication
    SET ApplicationCode = @ApplicationCode
    WHERE ApplicationID = @ApplicationID;

    UPDATE dbo.Tbl_Ruc_RecruitmentRequisition
    SET TotalApplications = ISNULL(TotalApplications, 0) + 1
    WHERE RequisitionID = @RequisitionID;

    SET @Result = 1;
END
GO

-- SP 8: [ruc].[SP_Ruc_JobApplication_GetAll]
CREATE OR ALTER PROCEDURE [ruc].[SP_Ruc_JobApplication_GetAll]
    @CompanyID INT,
    @PageNumber INT = 1,
    @PageSize INT = 50,
    @RequisitionID INT = NULL,
    @StatusID INT = NULL,
    @SearchTerm NVARCHAR(250) = NULL,
    @TotalCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @TotalCount = COUNT(*)
    FROM dbo.Tbl_Ruc_JobApplication
    WHERE CompanyID = @CompanyID AND IsActive = 1
      AND (@RequisitionID IS NULL OR RequisitionID = @RequisitionID)
      AND (@StatusID IS NULL OR StatusID = @StatusID);

    SELECT 
        A.ApplicationID, A.ApplicationCode, A.CompanyID, A.RequisitionID, A.ApplicantID, A.ApplicationDate,
        A.CurrentStatusID, A.StatusID, A.StatusCode, A.StatusName, A.ResumePath, A.ScreeningScore, A.OverallRating,
        A.Recommendation, A.ResumeParsingID, A.FullName, A.Email, A.MobileNumber,
        R.RequisitionCode, R.JobTitle AS RequisitionJobTitle, R.Location AS RequisitionLocation
    FROM dbo.Tbl_Ruc_JobApplication A
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition R ON A.RequisitionID = R.RequisitionID
    WHERE A.CompanyID = @CompanyID AND A.IsActive = 1
      AND (@RequisitionID IS NULL OR A.RequisitionID = @RequisitionID)
      AND (@StatusID IS NULL OR A.StatusID = @StatusID)
    ORDER BY A.ApplicationID DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- SP 9: [ruc].[SP_Ruc_JobApplication_GetById]
CREATE OR ALTER PROCEDURE [ruc].[SP_Ruc_JobApplication_GetById]
    @ApplicationID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        A.ApplicationID, A.ApplicationCode, A.CompanyID, A.RequisitionID, A.ApplicantID, A.ApplicationDate,
        A.CurrentStatusID, A.StatusID, A.StatusCode, A.StatusName, A.ResumePath, A.ScreeningScore, A.OverallRating,
        A.Recommendation, A.ResumeParsingID, A.FullName, A.Email, A.MobileNumber,
        R.RequisitionCode, R.JobTitle AS RequisitionJobTitle, R.Location AS RequisitionLocation
    FROM dbo.Tbl_Ruc_JobApplication A
    LEFT JOIN dbo.Tbl_Ruc_RecruitmentRequisition R ON A.RequisitionID = R.RequisitionID
    WHERE A.ApplicationID = @ApplicationID;
END
GO

-- SP 10: [ruc].[SP_AI_AutoScreenResume]
CREATE OR ALTER PROCEDURE [ruc].[SP_AI_AutoScreenResume]
    @CompanyID INT,
    @ApplicationID INT,
    @ApplicantID INT,
    @RequisitionID INT,
    @ResumeParsingID INT,
    @IsAutoProcessed BIT = 1,
    @MatchScore INT = 0,
    @Recommendation NVARCHAR(MAX) = NULL,
    @SkillsMatch NVARCHAR(MAX) = NULL,
    @ExperienceMatch NVARCHAR(MAX) = NULL,
    @QualificationsMatch NVARCHAR(MAX) = NULL,
    @RedFlags NVARCHAR(MAX) = NULL,
    @ScreeningProvider NVARCHAR(50) = 'AI',
    @ModelUsed NVARCHAR(200) = NULL,
    @TokensUsed INT = 0,
    @ProcessingTime INT = 0,
    @AutoShortlistThreshold INT = 80,
    @CreatedBy NVARCHAR(100) = 'System',
    @ScreeningID INT OUTPUT,
    @AutoShortlisted BIT OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @AutoShortlisted = CASE WHEN @MatchScore >= @AutoShortlistThreshold THEN 1 ELSE 0 END;

    INSERT INTO dbo.Tbl_RecruitmentAI_Screening
    (
        CompanyID, ApplicationID, ApplicantID, RequisitionID, ResumeParsingID, MatchScore,
        Recommendation, SkillsMatch, ExperienceMatch, QualificationsMatch, RedFlags,
        ScreeningProvider, ModelUsed, TokensUsed, ProcessingTime, AutoShortlistThreshold, AutoShortlisted, CreatedBy, CreatedOn
    )
    VALUES
    (
        @CompanyID, @ApplicationID, @ApplicantID, @RequisitionID, @ResumeParsingID, @MatchScore,
        @Recommendation, @SkillsMatch, @ExperienceMatch, @QualificationsMatch, @RedFlags,
        ISNULL(@ScreeningProvider, 'AI'), @ModelUsed, ISNULL(@TokensUsed, 0), ISNULL(@ProcessingTime, 0),
        ISNULL(@AutoShortlistThreshold, 80), @AutoShortlisted, ISNULL(@CreatedBy, 'System'), GETDATE()
    );

    SET @ScreeningID = SCOPE_IDENTITY();

    UPDATE dbo.Tbl_Ruc_JobApplication
    SET ScreeningScore = @MatchScore,
        Recommendation = @Recommendation
    WHERE ApplicationID = @ApplicationID;

    SET @Result = 1;
END
GO

-- SP 11: [ruc].[SP_AI_AutoShortlistCandidate]
CREATE OR ALTER PROCEDURE [ruc].[SP_AI_AutoShortlistCandidate]
    @CompanyID INT,
    @ApplicationID INT,
    @AIScreeningScore DECIMAL(5,2),
    @Threshold INT = 80,
    @PreviousStatusID INT OUTPUT,
    @PreviousStatusCode NVARCHAR(50) OUTPUT,
    @NewStatusID INT OUTPUT,
    @NewStatusCode NVARCHAR(50) OUTPUT,
    @AutoShortlisted BIT OUTPUT,
    @AutoShortlistDate DATETIME OUTPUT,
    @Result INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 
        @PreviousStatusID = StatusID,
        @PreviousStatusCode = StatusCode
    FROM dbo.Tbl_Ruc_JobApplication
    WHERE ApplicationID = @ApplicationID;

    IF @AIScreeningScore >= @Threshold
    BEGIN
        SET @NewStatusID = 2;
        SET @NewStatusCode = 'SHORTLISTED';
        SET @AutoShortlisted = 1;
        SET @AutoShortlistDate = GETDATE();

        UPDATE dbo.Tbl_Ruc_JobApplication
        SET StatusID = 2,
            StatusCode = 'SHORTLISTED',
            StatusName = 'Shortlisted',
            CurrentStatusID = 2
        WHERE ApplicationID = @ApplicationID;
    END
    ELSE
    BEGIN
        SET @NewStatusID = @PreviousStatusID;
        SET @NewStatusCode = @PreviousStatusCode;
        SET @AutoShortlisted = 0;
        SET @AutoShortlistDate = NULL;
    END

    SET @Result = 1;
END
GO

PRINT 'Demo Recruitment SPs and Tables created in InternDB.';
GO

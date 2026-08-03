/* =============================================================================
   DEMO SCHEMA — NOT THE REAL ONE. DO NOT SHIP. DO NOT MERGE INTO PRODUCTION.
   =============================================================================

   Purpose
   -------
   Unblocks local development while waiting for the supervisor's InternDB.bak.
   It creates ONLY the tables the recruitment-AI settings and job-description
   generation paths touch, so those flows can be exercised end to end and a
   frontend can be built against a real API instead of a mock.

   How these definitions were derived
   ----------------------------------
   By reading the inline SQL in Domain/Repositories/RecruitmentAIRepository.cs
   and taking the union of every column that is SELECTed, INSERTed or UPDATEd.
   Column NAMES are therefore accurate — the queries would fail otherwise.

   Column TYPES, lengths, nullability, indexes, defaults, constraints and
   collation are EDUCATED GUESSES. They are not the production definitions.
   A mismatch (a real NVARCHAR(50) where this says NVARCHAR(MAX), a real NOT
   NULL where this allows NULL) will not show up locally and may hide a bug
   until integration. Treat anything that works here as "not disproven", not
   as "verified against production".

   What is deliberately NOT here
   -----------------------------
   The recruitment module makes 106 stored-procedure calls. None are recreated.
   Only the AI settings / job-description / activity paths use inline SQL, and
   those are the only ones this file supports. Anything backed by a stored
   procedure (requisition lists, applications, interviews, dashboards) will
   still fail with "Could not find stored procedure ..." — correctly, because
   only the real backup can supply them.

   Replacing this
   --------------
   When InternDB.bak arrives:  ./db/restore.sh
   The restore drops and replaces InternDB wholesale. Delete this seed
   afterwards — keeping it risks someone "fixing" a real schema to match a
   guess made here.

   Prepared by Syed Taha, Multinet.
============================================================================= */

SET NOCOUNT ON;
GO

IF DB_ID('InternDB') IS NULL
BEGIN
    CREATE DATABASE [InternDB];
    PRINT 'Created database InternDB.';
END
ELSE
    PRINT 'Database InternDB already exists — leaving it alone.';
GO

USE [InternDB];
GO

/* ---------------------------------------------------------------------------
   Tbl_Ruc_RecruitmentAI_Settings
   One row per company. Holds the AI provider choice, the ENCRYPTED API key,
   the endpoint, and the feature toggles.

   Referenced by: GetApiKeyStatusAsync, GetApiKeySettingsAsync,
                  GetEncryptedApiKeyAsync, SaveApiKeySettingsAsync,
                  DeleteApiKeyAsync, GetSettingsAsync

   Note: AutoParse and AutoShortlistThreshold are SELECTed but never INSERTed
   by SaveApiKeySettingsAsync, so they MUST be nullable or have defaults —
   otherwise every save fails. That asymmetry is in the production code, not a
   mistake here.
--------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Tbl_Ruc_RecruitmentAI_Settings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_Ruc_RecruitmentAI_Settings
    (
        Id                      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID               INT               NOT NULL,

        -- 'openai' | 'anthropic' | 'google' | 'custom' | 'multinetai'
        Provider                NVARCHAR(50)      NOT NULL,

        -- Encrypted at rest by EncryptionHelper.EncryptText. Never plaintext.
        ApiKey                  NVARCHAR(MAX)     NULL,

        -- For MultinetAI this is the BASE URL; the backend appends feature paths.
        ApiEndpoint             NVARCHAR(500)     NULL,
        Model                   NVARCHAR(200)     NULL,

        -- Ignored by the in-house AI service; honoured by third-party providers.
        MaxTokens               INT               NULL CONSTRAINT DF_RucAISettings_MaxTokens   DEFAULT (1000),
        Temperature             DECIMAL(4,2)      NULL CONSTRAINT DF_RucAISettings_Temperature DEFAULT (0.70),

        -- Feature toggles. Portal-side orchestration only: they decide whether
        -- the backend makes a call, nothing more.
        AutoScreening           BIT               NULL CONSTRAINT DF_RucAISettings_AutoScreening   DEFAULT (0),
        AutoMatching            BIT               NULL CONSTRAINT DF_RucAISettings_AutoMatching    DEFAULT (0),
        AutoParse               BIT               NULL CONSTRAINT DF_RucAISettings_AutoParse       DEFAULT (0),
        GenerateQuestions       BIT               NULL CONSTRAINT DF_RucAISettings_GenQuestions    DEFAULT (0),
        EmailNotifications      BIT               NULL CONSTRAINT DF_RucAISettings_EmailNotif      DEFAULT (1),

        -- Sent with screening calls; the AI service echoes it as threshold_used.
        AutoShortlistThreshold  INT               NULL CONSTRAINT DF_RucAISettings_Threshold       DEFAULT (80),

        CreatedBy               NVARCHAR(100)     NULL,
        CreatedOn               DATETIME          NULL CONSTRAINT DF_RucAISettings_CreatedOn DEFAULT (GETDATE()),
        UpdatedBy               NVARCHAR(100)     NULL,
        UpdatedOn               DATETIME          NULL,

        -- Every query filters on IsActive = 1; DeleteApiKey is a soft delete.
        IsActive                BIT               NOT NULL CONSTRAINT DF_RucAISettings_IsActive DEFAULT (1)
    );

    -- Every read is "WHERE CompanyID = @CompanyID AND IsActive = 1".
    CREATE INDEX IX_RucAISettings_Company_Active
        ON dbo.Tbl_Ruc_RecruitmentAI_Settings (CompanyID, IsActive);

    PRINT 'Created Tbl_Ruc_RecruitmentAI_Settings.';
END
ELSE
    PRINT 'Tbl_Ruc_RecruitmentAI_Settings already exists.';
GO

/* ---------------------------------------------------------------------------
   Tbl_Ruc_RecruitmentAI_JobDescriptions
   History of generated job descriptions.

   Referenced by: SaveJobDescriptionAsync, SaveJobDescriptionWithUpdateAsync,
                  GetDashboardStatsAsync

   JobRequisitionID is NULL until a human saves the draft as a requisition —
   that is the advisory model in the schema: generation and commitment are
   separate events.

   PromptUsed holds the exact request payload sent to the AI service. It is
   what the AI team needs to reproduce a bad generation.
--------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions
    (
        Id                   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID            INT               NOT NULL,

        -- Null until a human commits the draft to a requisition.
        JobRequisitionID     INT               NULL,

        GeneratedDescription NVARCHAR(MAX)     NULL,
        PromptUsed           NVARCHAR(MAX)     NULL,
        Model                NVARCHAR(200)     NULL,

        -- Always 0 for the in-house service: it runs a resident local model and
        -- reports wall-clock time rather than metering tokens.
        TokensUsed           INT               NULL CONSTRAINT DF_RucAIJobDesc_Tokens DEFAULT (0),

        CreatedBy            NVARCHAR(100)     NULL,
        CreatedOn            DATETIME          NULL CONSTRAINT DF_RucAIJobDesc_CreatedOn DEFAULT (GETDATE())
    );

    -- SaveJobDescriptionWithUpdateAsync does TOP 1 ... ORDER BY CreatedOn DESC.
    CREATE INDEX IX_RucAIJobDesc_Company_Created
        ON dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions (CompanyID, CreatedOn DESC);

    PRINT 'Created Tbl_Ruc_RecruitmentAI_JobDescriptions.';
END
ELSE
    PRINT 'Tbl_Ruc_RecruitmentAI_JobDescriptions already exists.';
GO

/* ---------------------------------------------------------------------------
   Tbl_Ruc_RecruitmentAI_Activity
   Activity feed shown on the recruitment AI dashboard.

   Referenced by: SaveActivityAsync, UpdateActivityRelatedId,
                  GetDashboardStatsAsync

   RelatedId is backfilled once the generated draft becomes a requisition, so
   it is nullable and is matched on "RelatedId IS NULL OR RelatedId = 0".
--------------------------------------------------------------------------- */
IF OBJECT_ID('dbo.Tbl_Ruc_RecruitmentAI_Activity', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tbl_Ruc_RecruitmentAI_Activity
    (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CompanyID    INT               NOT NULL,

        -- e.g. 'job_description', 'resume_parsing', 'screening'
        ActivityType NVARCHAR(100)     NULL,
        Title        NVARCHAR(500)     NULL,
        Description  NVARCHAR(MAX)     NULL,

        -- Backfilled with the requisition id once a human commits the draft.
        RelatedId    INT               NULL,

        CreatedOn    DATETIME          NULL CONSTRAINT DF_RucAIActivity_CreatedOn DEFAULT (GETDATE())
    );

    CREATE INDEX IX_RucAIActivity_Company_Type_Created
        ON dbo.Tbl_Ruc_RecruitmentAI_Activity (CompanyID, ActivityType, CreatedOn DESC);

    PRINT 'Created Tbl_Ruc_RecruitmentAI_Activity.';
END
ELSE
    PRINT 'Tbl_Ruc_RecruitmentAI_Activity already exists.';
GO

PRINT '';
PRINT '--------------------------------------------------------------';
PRINT ' Demo schema ready in InternDB.';
PRINT '';
PRINT ' NOT seeded here: the AI settings row. Create it through the';
PRINT ' portal API (SaveApiKeySettings) so the API key is encrypted';
PRINT ' by EncryptionHelper exactly as production does. Writing an';
PRINT ' encrypted value straight into the table by hand would either';
PRINT ' fail to decrypt or bake in an assumption about the cipher.';
PRINT '--------------------------------------------------------------';
GO

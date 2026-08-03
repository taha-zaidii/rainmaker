/* =============================================================================
   Watching the AI features work — Azure Data Studio scratchpad
   =============================================================================

   Connection: localhost,1433 · SQL Login · sa · Multinet@123!
               Trust server certificate = TRUE  (the container's cert is
               self-signed, so "Mandatory + False" always fails)
               Database = InternDB

   Run a block with the cursor inside it and press Cmd+Shift+E, or select the
   text and hit Run.

   Prepared by Syed Taha, Multinet.
============================================================================= */

USE [InternDB];
GO


/* ── 1. Is anything configured? ──────────────────────────────────────────
   The portal's AI Settings screen writes here. ApiKey is stored ENCRYPTED
   by EncryptionHelper — if you can read it as plaintext, something is wrong
   and worth raising immediately.                                          */

SELECT
    Id,
    CompanyID,
    Provider,
    CASE
        WHEN ApiKey IS NULL OR ApiKey = '' THEN '(no key)'
        WHEN ApiKey LIKE '%demo%' OR ApiKey LIKE '%placeholder%' THEN '*** PLAINTEXT — INVESTIGATE ***'
        ELSE '(encrypted, ' + CAST(LEN(ApiKey) AS VARCHAR(10)) + ' chars)'
    END                                   AS ApiKeyState,
    ApiEndpoint,
    Model,
    MaxTokens,
    Temperature,
    AutoShortlistThreshold,
    AutoScreening, AutoMatching, AutoParse, GenerateQuestions, EmailNotifications,
    IsActive,
    UpdatedOn
FROM dbo.Tbl_Ruc_RecruitmentAI_Settings
ORDER BY CompanyID;
GO


/* ── 2. Every job description the AI has generated ───────────────────────
   PromptUsed holds the EXACT request payload sent to the AI service. That
   is what the AI team needs to reproduce a bad generation — copy it
   verbatim into a bug report rather than describing it.                   */

SELECT TOP 20
    Id,
    CompanyID,
    JobRequisitionID,                     -- NULL until a human saves the draft
    LEFT(GeneratedDescription, 120) + '…' AS Preview,
    Model,
    TokensUsed,                           -- always 0: the service reports
                                          -- wall-clock time, not token counts
    CreatedBy,
    CreatedOn
FROM dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions
ORDER BY CreatedOn DESC;
GO

/* The full payload of the most recent generation. */
SELECT TOP 1 PromptUsed, GeneratedDescription
FROM dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions
ORDER BY CreatedOn DESC;
GO


/* ── 3. The activity feed the dashboard reads ────────────────────────────
   Generate a job description in the portal, run this, and a new row should
   be here. That is the quickest end-to-end proof that the whole chain —
   browser → API → AI service → database — is intact.                      */

SELECT TOP 20
    Id,
    ActivityType,
    Title,
    Description,
    RelatedId,                            -- backfilled when the draft becomes
                                          -- a requisition
    CreatedOn
FROM dbo.Tbl_Ruc_RecruitmentAI_Activity
ORDER BY CreatedOn DESC;
GO


/* ── 4. Live counters ────────────────────────────────────────────────────
   Re-run after each portal action to watch the numbers move.              */

SELECT
    (SELECT COUNT(*) FROM dbo.Tbl_Ruc_RecruitmentAI_Settings WHERE IsActive = 1)  AS ConfiguredCompanies,
    (SELECT COUNT(*) FROM dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions)              AS JobDescriptions,
    (SELECT COUNT(*) FROM dbo.Tbl_Ruc_RecruitmentAI_Activity)                     AS ActivityRows,
    (SELECT MAX(CreatedOn) FROM dbo.Tbl_Ruc_RecruitmentAI_Activity)               AS LastActivity;
GO


/* ── 5. Reset to a clean slate ───────────────────────────────────────────
   Deliberately commented out. Uncomment the lines you want and run them.

   The first one makes the AI Settings screen show "no key configured"
   again, which is the state to test from when you want to verify the
   first-run experience.                                                    */

-- DELETE FROM dbo.Tbl_Ruc_RecruitmentAI_Settings        WHERE CompanyID = 133;
-- DELETE FROM dbo.Tbl_Ruc_RecruitmentAI_JobDescriptions WHERE CompanyID = 133;
-- DELETE FROM dbo.Tbl_Ruc_RecruitmentAI_Activity        WHERE CompanyID = 133;
GO


/* ── 6. What exists in this database ─────────────────────────────────────
   Only three tables plus one stored procedure. Everything else the
   recruitment module needs — 106 stored-procedure calls — arrives with the
   production backup. A query here returning nothing usually means the
   feature is genuinely not supported locally, not that it is broken.      */

SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

SELECT ROUTINE_NAME, ROUTINE_TYPE
FROM INFORMATION_SCHEMA.ROUTINES
ORDER BY ROUTINE_NAME;
GO

/* =============================================================================
   DEMO STORED PROCEDURE — NOT THE REAL ONE. DO NOT SHIP.
   =============================================================================

   Why this exists
   ---------------
   Program.cs calls sp_Adm_GetAllPermissionNames_v2 during startup, BEFORE the
   web host is built, to register one authorization policy per permission:

       permissions = await dapper.QueryAsync<string>(
           "sp_Adm_GetAllPermissionNames_v2", null, CommandType.StoredProcedure);

   The call is wrapped in try/catch and is meant to degrade gracefully. In
   practice, once InternDB existed the module stopped completing startup —
   it produced no console output and never bound its port. Supplying the
   procedure removes that startup dependency instead of leaving the module
   to fail on it.

   This procedure belongs to the ADMIN module, not recruitment. The real one
   reads the actual permission catalogue. This returns a small fixed list so
   that (a) startup completes, and (b) the RECRUITMENT_* policies referenced
   by [ModuleAuthorize("RECRUITMENT_")] actually exist locally.

   Replaced wholesale by the supervisor's InternDB.bak. Delete this afterwards.

   Prepared by Syed Taha, Multinet.
============================================================================= */

USE [InternDB];
GO

CREATE OR ALTER PROCEDURE dbo.sp_Adm_GetAllPermissionNames_v2
AS
BEGIN
    SET NOCOUNT ON;

    -- Single column of permission names; Program.cs reads it as IEnumerable<string>.
    SELECT PermissionName FROM (VALUES
        ('RECRUITMENT_VIEW'),
        ('RECRUITMENT_CREATE'),
        ('RECRUITMENT_EDIT'),
        ('RECRUITMENT_DELETE'),
        ('RECRUITMENT_APPROVE'),
        ('RECRUITMENT_AI_SETTINGS'),
        ('RECRUITMENT_AI_GENERATE')
    ) AS Permissions(PermissionName);
END
GO

PRINT 'Created demo sp_Adm_GetAllPermissionNames_v2 (7 permissions).';
GO

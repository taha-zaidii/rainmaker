# Local database — Rainmaker HRMS recruitment portal

The ERP backend does **all** of its data access through stored procedures
(Dapper, `CommandType.StoredProcedure`). There is no EF Core model and no
migrations in the repository, so an empty database gets you a process that
starts and then fails every request with `Could not find stored procedure ...`.
A copy of the real schema is therefore a hard prerequisite.

Current state of this machine:

| Thing | Status |
|---|---|
| Company DB `192.168.150.52 / InternDB` | **Unreachable** from this network (`Network is unreachable`) — office LAN or VPN only |
| Local Docker `multinet-db` (azure-sql-edge, SQL 2019 engine) | Running, reachable, **empty** — has an `InternDB` with 0 tables |
| .NET → local SQL connectivity | **Verified working** (login, TLS, database resolution all succeed) |
| Stored procedures needed by the code | **79 distinct** procedures referenced, 0 present |

---

## What to ask your supervisor for

Send exactly this list — each item exists because something in the code needs it.

1. **A full backup of the recruitment/HRMS database** — `InternDB.bak`
   (`BACKUP DATABASE [InternDB] TO DISK = '...' WITH COMPRESSION, INIT`).
   A schema-only script also works, but it must include stored procedures,
   views, functions, and the seed/lookup rows — the module reads reference
   data (statuses, rating scales, nav/permission rows) at runtime.

2. **The engine version the backup was taken on** — the output of
   `SELECT @@VERSION;`. A `.bak` can only be restored onto an engine of the
   same or newer version, and that decides which container image we run.

3. **The dev JWT signing key** — `Jwt:SecretKey` from the Admin/Auth service
   that *issues* tokens. It has to match byte-for-byte or every authenticated
   request 401s. Also ask which service issues tokens and how to get one for a
   test user (login endpoint + a dev account).

4. **A test company + user**: `CompanyID`, a user with the recruitment
   permissions, and an employee record — most endpoints scope by `CompanyID`
   from JWT claims.

5. **Confirmation on `appsettings.central.json`** — `AddCentralConfiguration()`
   walks up to 8 parent directories looking for this file and, if found, loads
   it **last**, overriding everything else. Ask whether the team uses it and
   what belongs in it; otherwise local config silently loses to a stray file.

6. **VPN details** (optional but useful) — so the real DB can be reached when
   comparing behaviour against the shared environment.

> Ask for the backup over an internal channel, and remember it is production
> HR data: it stays on this machine, it is gitignored (`*.bak`, `db/backups/`),
> and it never goes into a commit, a paste, or an external service.

---

## Restoring it when it arrives

```bash
# 1. One-time: enable Rosetta so the amd64 SQL Server 2022 image runs fast.
#    Docker Desktop → Settings → General →
#    "Use Rosetta for x86_64/amd64 emulation on Apple Silicon" → Apply & restart

# 2. Free port 1433 (the old Edge container holds it) and start SQL Server 2022
docker stop multinet-db
export MSSQL_SA_PASSWORD='<pick-a-strong-local-password>'
docker compose -f db/docker-compose.yml up -d

# 3. Drop the backup in and restore it
mkdir -p db/backups && cp ~/Downloads/InternDB.bak db/backups/
./db/restore.sh db/backups/InternDB.bak InternDB

# 4. Point the API at it
#    Backend/RM/Digi.Recruitment.Module/appsettings.Development.json
#      "DefaultConnection":
#        "Server=127.0.0.1,1433;Database=InternDB;User Id=sa;Password=<same>;
#         Encrypt=False;TrustServerCertificate=True;Connect Timeout=10"
```

`restore.sh` handles the two things that usually break a Windows→Linux restore:
it rewrites the backup's internal `D:\...\*.mdf` paths with `MOVE` clauses, and
it uses `WITH REPLACE` so re-restoring over an existing database works. It
finishes by printing object counts and the recruitment objects it found, so you
can see immediately whether the restore actually carried the procedures.

### If the backup turns out to be SQL Server 2019 or older
Then the currently running `multinet-db` (Azure SQL Edge, 15.0.2000) can host it
and you can skip steps 1–2 — just run `restore.sh`, which auto-detects whichever
container is up. Prefer the 2022 image anyway if the company runs 2022: matching
the engine avoids compatibility-level surprises that only show up in production.

---

## Objects the code expects

Useful for sanity-checking a restore, or for scoping a hand-written schema if a
backup never materialises.

- **79** stored procedures are called by name. The recruitment-specific ones:
  `sp_ruc_candidates`, `sp_ruc_jobs`, `sp_ruc_jobapplications`,
  `sp_ruc_interviews`, `sp_ruc_recruiters`, `sp_ruc_recruitmentrequest`,
  `sp_Hr_Ruc_RecruitmentRequisition_{Insert,Update,Delete,Public_Manage}`,
  `sp_HR_Ruc_RecruitmentRequisition_{Get,GetAllInformation}`,
  `sp_Hr_Ruc_GetJobApplicationsByRequisition`,
  `sp_Hr_Ruc_GetJobApplicationsShortlisted`,
  `sp_Hr_Ruc_GetAllShortlistedCandidates`, `sp_Hr_Ruc_GetAllInterviewSchedules`,
  `sp_Hr_Ruc_SaveInterviewSchedule`, `sp_Hr_Ruc_UpdateJobApplicationStatus`,
  `sp_Hr_Ruc_UpdateInterviewScheduleIsHired`,
  `sp_Hr_Ruc_GetCandidateEvaluations`, `sp_Hr_Ruc_CandidateAssignGroup`,
  `sp_Hr_Ruc_ApplicationStatus_*`, `sp_Hr_Ruc_SchedulePanel_GetAssignList`,
  `sp_Hr_Ruc_GetRecruitmentRequisitionJobDetails`, `sp_Dashboard_RecStats`.
- **Cross-cutting**, needed before anything authenticates or logs:
  `sp_Adm_GetAllPermissionNames_v2` (called at startup),
  `sp_Adm_GetUserPermissions_v2`, `sp_Adm_GetCompanySubscription`,
  `sp_Sys_AuditLog_Insert`, `sp_Db_Generic_CRUD`, `sp_Glob_InsertEmailLog`,
  `sp_Wf_GetApproverEmailsByWorkflow`, `sp_Check_ApprovalFlowConfigured`.
- **AI feature tables** the existing `RecruitmentAIService` reads and writes:
  `Tbl_Ruc_RecruitmentAI_Settings`, `Tbl_Ruc_RecruitmentAI_Activity`,
  `Tbl_Ruc_RecruitmentAI_JobDescriptions`,
  `Tbl_Ruc_RecruitmentAI_ResumeScreenings`.
- **Schema-qualified** objects also appear: `[ruc].[Tbl_CandidateEvaluation]`,
  `[ruc].[Tbl_RatingScale]`, `[ruc].[Tbl_Status]` — so the `ruc` schema must
  exist, not just `dbo`.

New objects that this project adds (the parse-job queue and the in-house AI
provider) live in `db/migrations/` as numbered, idempotent scripts, so they can
be applied on top of whatever the supervisor sends without touching his schema.

---

*Prepared by Syed Taha, Multinet.*

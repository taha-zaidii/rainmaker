# Requests for supervisor — Multinet AI integration (recruitment portal)

Prepared by Syed Taha, Multinet.

Context: the MultinetAI provider is integrated into the recruitment portal backend
and verified end to end locally (settings save → Test API Key → JD generation →
database persistence). The items below are what is needed to finish the feature
and hand it over cleanly. Each one says *why*, so nothing here is a nice-to-have.

---

## A. Needed to finish the job-description feature

**A1. `sp_Hr_Ruc_RecruitmentRequisition_Insert` — the procedure definition**
Plus the `Tbl_Hr_Ruc_RecruitmentRequisition` table DDL.

*Why:* "Generate Job Description with AI" now works. The **next** button —
saving that draft as a requisition — goes through
`SaveJobDescriptionWithUpdateAsync` → `CreateJobRequisitionFromRequest`, which
calls this stored procedure. Without it, generation succeeds and saving fails.
This is the single biggest blocker to a complete demo.

**A2. `CREATE TABLE` scripts for three AI tables**

- `Tbl_Ruc_RecruitmentAI_Settings`
- `Tbl_Ruc_RecruitmentAI_JobDescriptions`
- `Tbl_Ruc_RecruitmentAI_Activity`

*Why:* these were recreated locally by reading the inline SQL in
`RecruitmentAIRepository.cs`. The column **names** are certainly right — the
queries would fail otherwise — but the **types, lengths, nullability and
defaults** are educated guesses. A mismatch (a real `NVARCHAR(50)` where the
local copy assumes `NVARCHAR(MAX)`, a real `NOT NULL` where it allows nulls)
will not fail locally and would only surface on integration.

In SSMS: right-click each table → Script Table as → CREATE To. Takes a minute.

**A3. The Job Category dropdown's allowed values**
Either the lookup table name, or the list itself if it is hard-coded in the
frontend.

*Why:* the AI service accepts a `jobCategoryOptions` array and **snaps its answer
to one of those values**, so the result always binds to the dropdown. Without the
list it returns free text the dropdown may reject. This is a real quality
difference, not a formality.

---

## B. Changes needed in the live portal frontend

These are in the deployed build, which is not in this workspace.

**B1. Add `MultinetAI` to the AI Provider dropdown.**
The backend already accepts and routes provider value `multinetai`.

**B2. Fix the API Endpoint helper text.** It currently says
*"Prefer full path `https://ai.rainmaker.pk/hrms/api/query`"*.
**That path returns 404** — verified against the live service. Replace with:

> Base URL only (e.g. `https://ai.rainmaker.pk/hrms/api/v1`) — the backend
> appends the feature path.

The backend now auto-corrects a stored `.../api/query` to `.../api/v1` and returns
a warning, so nothing is broken in the meantime, but the saved value should be
fixed at source.

**B3. Validate the Model field**, or make it read-only. It has been used to hold
arbitrary text (an email address in one case). The AI service ignores it anyway.

**B4. The JD generate spinner must tolerate ~35 seconds.**
Measured: cold ~20–35 s, warm ~13 s, identical repeat ~9 ms (server-side cache).
It needs a clear error path, not a silent timeout.

**B5. Company 133 must be re-pointed to `MultinetAI`.**
It is currently saved as provider `custom`. `custom` is deliberately reserved for
third-party services a client brings themselves (Groq, DeepSeek, self-hosted), so
the backend does **not** treat it as ours — that would silently hijack a client's
own configuration. One dropdown change per affected tenant.

---

## C. Confirmations (quick answers, no work)

**C1. Does `devhrms.rainmaker.pk` run the same source as the folder I was given?**

*Why:* the integration brief says the `TestApiKey` controller "does not exist and
returns 404". In this source it **does** exist — the real defect is that
`TestApiKeyAsync` only handles openai/anthropic/google, so `custom` falls through
to *"Unsupported provider for testing"*, which the UI renders as "API Key
Invalid". Same symptom, different cause. Worth knowing which build was tested.

**C2. Does the team use `appsettings.central.json`?**

*Why:* `AddCentralConfiguration()` walks up to 8 parent directories for this file
and, if found, loads it **last** — overriding everything else, including
connection strings. If a stray copy exists anywhere above the repo, local config
silently loses to it.

**C3. Confirm `CompanyID = 133` and a test user** for local testing, and which
service issues JWTs (so tokens can be obtained the normal way rather than minted).

---

## D. Optional — only if convenient

**D1. `InternDB.bak`** (full backup incl. stored procedures, views, seed rows).
Deliberately deprioritised — local work continues on a demo schema. Without it,
anything stored-procedure backed stays unavailable locally: requisition lists,
applications, interview scheduling, candidate evaluation, dashboards. Item A1
above is the narrow slice needed for the JD feature specifically.

**D2. Access to the portal frontend repo**, if the section B changes should be
made rather than described.

---

## Already have — no action needed

- **The AI service API key** — obtained.
- **AI service contract** — verified directly against production:
  `GET /hrms/api/v1/auth/verify` → 401 with a bad key (endpoint live),
  `GET /hrms/api/query` → 404 (confirms B2),
  `POST /hrms/api/v1/recruitment/jobreq/generate` → 401 with a bad key
  (endpoint live), vs 404 for a nonsense path.

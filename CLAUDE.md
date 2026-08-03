# Rainmaker HRMS — Recruitment Portal (Multinet)

> Master context for anyone (human or agent) picking up this workspace.
> Read this first. It is kept current as work progresses.
>
> Prepared by Syed Taha, Multinet.

---

## 1. Who and what

**Owner:** Syed Taha Zaidi — working on Multinet's ERP, *Rainmaker*, specifically the
**HRMS / Recruitment portal**, backend and frontend.

**Current mission:** integrate Multinet's **in-house AI service** (`hrms-ai-service`)
into the recruitment portal backend, one feature at a time, then build a revamped
Angular frontend for the portal.

**Stack:** ASP.NET Core 8 (`net8.0`, pinned via `global.json`) · Dapper + SQL Server ·
Angular 19 · Docker (local SQL Server) · Azure Data Studio.

---

## 2. Ground rules — read before changing anything

1. **This repo is an OLD build.** The supervisor shared it locally to work in.
   The live portal (`devhrms.rainmaker.pk`) is **ahead of this tree**. Work here is
   handed back as modules the supervisor copy-pastes into the main build.
   → **Consequence:** keep additions *self-contained* and edits to existing files
   *small, additive and clearly marked*. Never refactor senior devs' code.
   Never reformat a file you are only adding to.
2. **Match the surrounding conventions exactly** — `ApiResponse<T>` returns,
   try/catch with `_logger.LogError`, `Task<ApiResponse<X>>` service methods,
   `[HttpPost("PascalCaseName")]` controller routes. New code should be
   indistinguishable in style from what is already there.
3. **Every AI output is ADVISORY.** Responses carry `review_required: true`.
   A human always edits and approves. Never auto-commit AI output to a
   requisition, an application status, or an evaluation of record. This is a
   legal requirement (EU AI Act Annex III, high-risk hiring), not a preference.
4. **Never invent an AI endpoint path.** The full list is in §5. If something is
   needed that is not listed, ask the AI team. (`/api/query` was guessed once and
   cost real debugging time — it 404s.)
5. **Secrets never enter source control.** API keys live in
   `appsettings.Development.json` (gitignored), user-secrets, or env vars.
6. **Local only.** Nothing from this workspace — code, conversations, memory,
   history — is shared to a team plan or any external service.

---

## 3. Layout

```
rainmaker-hrms/
├── CLAUDE.md                                  ← this file
├── PROMPT_ERP_BOOTSTRAP.md                    ← environment bootstrap brief
├── PROMPT_PORTAL_BACKEND_AI_INTEGRATION.md    ← THE AI contract spec (authoritative)
├── Backend/
│   ├── global.json                            ← pins .NET 8 SDK
│   └── RM/
│       ├── Digi.Shared/                       ← shared DTOs + helpers (cross-module)
│       │   └── DTOs/hrm.module/RecruitmentAIDtos.cs
│       ├── Digi.Recruitment.Module/           ← the recruitment API
│       │   ├── Controllers/RecruitmentAIController.cs
│       │   ├── Domain/Services/RecruitmentAIService.cs      ← 2.5k lines, senior devs'
│       │   ├── Domain/Repositories/RecruitmentAIRepository.cs
│       │   └── Domain/AI/Multinet/            ← ★ OUR self-contained AI integration
│       └── Digi.Recruitment.Module.Tests/     ← xUnit, dependency-light
└── Frontend/                                  ← bare Angular 19 scaffold (see §7)
```

**`Domain/AI/Multinet/` is entirely ours.** It does not exist in the supervisor's
build. It ships as one folder plus a short list of additive edits elsewhere.

---

## 4. The AI service — connection facts

**Base URL (production):** `https://ai.rainmaker.pk/hrms/api/v1` — *base only, no
trailing slash*. The backend appends the feature path.

**Auth:** header `X-API-Key: <key>` on every call. Fail-closed: no key → 401.

- **Timeout: 180 s minimum** on generation calls. Not slow — a large local model.
- Latency: cold ~20–35 s, warm ~13 s, identical repeat ~9 ms (server-side cache;
  see the `x-recruitment-cache: hit|miss` response header).
- **Retry** on 429 / 502 / 503 / 504 / timeout, max 2, with backoff.
  **Never retry a 422** — same input yields the same rejection.
- GPU work is **serial**. Never fan out parallel generation calls. A 429 means wait.
- Ops endpoints (`/health`, `/ready`, `/docs`, `/openapi.json`) are **404 at the
  nginx edge** — on-box only. Use `GET {base}/auth/verify` for health checks.
- One resident model (`qwen3.5:27b`) serves everything. No candidate data ever
  leaves Multinet infrastructure — that data-sovereignty property is a selling
  point; do not route anything through third-party APIs.

### Settings-page fields: what actually matters

| Field | Value | Behaviour |
|---|---|---|
| AI Provider | `MultinetAI` (or legacy `Custom API`) | see §6 |
| Model | `qwen3.5:27b` | **ignored** by the service — informational |
| API Key | the configured key | verified working |
| API Endpoint | `https://ai.rainmaker.pk/hrms/api/v1` | **base URL only** |
| Max Tokens | any | **ignored** — service budgets per endpoint (JD needs ~1700) |
| Temperature | any | **ignored** — pinned to 0.0 server-side |
| Auto Shortlist Threshold | `80` | **honoured** — send with screening calls |

---

## 5. Endpoint map (the complete list)

| Portal feature | Path appended to base |
|---|---|
| Test API Key | `/auth/verify` |
| Generate JD with AI | `/recruitment/jobreq/generate` |
| Auto Resume Parse | `/parser/extract` (multipart, field name `file`) |
| Auto Resume Screening | `/recruitment/screening/screen` |
| Auto Candidate Matching | `/matching/rank` |
| Generate Interview Questions | `/recruitment/interview/questions` |
| Candidate Evaluation assist | `/scoring/score` |
| PMP (already live, other team) | `/pmp/goals/generate`, `/pmp/recommendations/generate`, `/pmp/status` |

Canonical constants live in `Domain/AI/Multinet/MultinetAiEndpoints.cs`.
**Never hardcode a path elsewhere.**

### Error contract

| Status | Body | Action |
|---|---|---|
| 401 | `{"detail":{"error":"unauthorized"}}` | "AI key invalid" — do not retry |
| 413 | `file_too_large` | user-facing size error |
| 422 | `{"detail":{"error":"<slug>"}}` | friendly message — **never retry** |
| 429 | `{"error":"busy","retry_after_s":N}` + `Retry-After` | wait and retry |
| 503 | `{"error":"llm_unreachable"}` | retry with backoff; alert ops if sustained |
| 500 | `{"error":"internal_error"}` | log correlation id, generic error |

Note the shape difference: **401/422 nest under `detail`; 429/503/500 put `error`
at the root.** Both must be parsed. Error bodies are deliberately sanitized —
they never contain prompts, file paths or candidate data.

---

## 6. Provider recognition — by NAME only

The dropdown offers `openai | anthropic | google | custom`, and we are adding
**`multinetai`** as a first-class fifth option. A client selects it, enters their
metered key and the base endpoint, and the recruitment AI features light up.

**`custom` is NOT an alias for us.** It is the client's escape hatch for a
third-party service they bring themselves — Groq, DeepSeek, a self-hosted
gateway. `MultinetAiProvider.Matches()` therefore keys on the **provider name
only** and never inspects the endpoint URL. Sniffing the URL to decide "this
looks like ours" would silently hijack a client's own configuration, and nothing
in the settings UI would reveal it.

Any new feature branch must use `MultinetAiProvider.Matches(provider)`.

### Two open items this creates

1. **Company 133 must be re-pointed.** It is currently saved as `custom` with the
   AI service URL. Once the frontend ships the MultinetAI option, that tenant has
   to re-select it, or its AI features will not route anywhere.
2. **`custom` has no backend at all today.** `TestApiKeyAsync` and
   `CallAIAPIAsync` both handle only openai/anthropic/google; `custom` falls to
   `default:` and returns *"Unsupported provider"*. So the "Custom API" option has
   never actually worked for generation — only saving the settings worked. Giving
   it a real OpenAI-compatible implementation (Groq, DeepSeek and most others
   speak `/chat/completions`) is its own task, not part of this integration.

---

## 7. Frontend status

`Frontend/` is a **bare Angular 19 scaffold** (`ng new` output). The real portal UI
— the AI Settings page and the 4-step job-create wizard in the screenshots — lives
in the deployed portal's own repo, which is **not in this workspace**.

Run it: `cd Frontend && npm start` → http://localhost:4200 (verified working).

Planned: a revamped recruitment portal frontend built locally, after the backend
integration is solid.

**Frontend changes owed to the supervisor** (he applies them in the live build):
1. Add `MultinetAI` to the AI Provider dropdown.
2. Fix the helper text under API Endpoint — it currently says *"Prefer full path
   `https://ai.rainmaker.pk/hrms/api/query`"*, which **404s**. Replace with:
   "Base URL only (e.g. `https://ai.rainmaker.pk/hrms/api/v1`); the backend
   appends the feature path."
3. Validate the **Model** field or make it read-only — it has been used to hold
   arbitrary text (an email address in one case).
4. JD spinner must tolerate 35 s with a clear error path.

---

## 8. Findings that correct the spec

The spec (`PROMPT_PORTAL_BACKEND_AI_INTEGRATION.md`) was written against the *live*
build. Two of its claims do not hold for *this* tree:

1. **§3 says the `TestApiKey` controller "does not exist" and returns 404.**
   It exists here — `RecruitmentAIController.cs:107`. The real defect is in
   `RecruitmentAIService.TestApiKeyAsync`: its `switch` handles only
   openai/anthropic/google, so `custom` falls to `default:` and returns
   *"Unsupported provider for testing"*, which the UI renders as "API Key
   Invalid". Same symptom, different cause — the AI service was never contacted.
2. **`SaveApiKeySettingsAsync` validates against `{openai, anthropic, google,
   custom}`.** Adding a dropdown option without adding it there fails the save
   with "Invalid provider".

Neither changes what gets built; both are worth reporting upstream.

**Verified against the live service on 2026-08-01:**

```
GET https://ai.rainmaker.pk/hrms/api/v1/auth/verify   → 401 {"detail":{"error":"unauthorized"}}
GET https://ai.rainmaker.pk/hrms/api/query            → 404
```

So the contract in §4–§5 holds exactly, and the settings page's helper text really
does point at a dead path. `MultinetAiEndpoints.ResolveBaseUrl` therefore rewrites a
stored `.../api/query` to `.../api/v1` and returns a warning, so the live tenant is
not broken while the saved value is being corrected.

Test API Key outcomes, all confirmed end to end against the live service:

| Configured endpoint | Key | `status` |
|---|---|---|
| `…/hrms/api/v1` | wrong | `invalid_key` |
| `…/hrms/api/query` | wrong | `invalid_key` + corrected-endpoint warning |
| `…/hrms/api` | any | `misconfigured` |
| unroutable host | any | `unreachable` |
| `not-a-url` | any | `misconfigured` |
| `custom` provider @ `…/hrms/api/v1` | wrong | `invalid_key` (legacy path still routes) |

---

## 9. Progress

Legend: ✅ done · 🔄 in progress · ⬜ not started

### Environment
- ✅ .NET 8 SDK installed, `global.json` pinned, solution builds
- ✅ Repo hygiene: git init, `.gitignore`, secrets stripped from tracked config
- ✅ Backend runs locally; Swagger up; degraded-mode startup without a DB
- ✅ Local SQL Server 2022 via docker-compose (`multinet-db`, port 1433)
- ✅ Angular dev server verified on :4200

### AI integration
- ✅ Typed client scaffold (`Domain/AI/Multinet/`) — options, Polly, stub mode, tests
- ✅ Realigned to the **production edge** contract — `MultinetAiEndpoints.cs` owns every
  path; root-level 429/503 error bodies parsed; `Retry-After` honoured (header beats
  body, capped at 120 s); per-company base URL override for multi-tenancy
- ✅ `/auth/verify` + working **Test API Key** button, verified against the LIVE service
- ✅ **JD generation** (`/recruitment/jobreq/generate`) → 4-step wizard mapping.
  All 7 binding rules pinned by tests against the contract's own worked example.
  Advisory invariants re-asserted client-side (age limits discarded, status forced
  to Draft, never public). Endpoint confirmed live: 401 with a bad key, vs 404 for a
  nonsense path. **Not yet run end to end over HTTP** — that needs Docker up and a
  `multinetai` settings row for a company (see §12).
- ✅ **Resume parsing** (`/parser/extract` and `/parser/extract-url`) → integrated into `ParseResumeAsync`.
  `MultinetAiProvider.Matches` branch added to route `multinetai` requests to the in-house AI client. Supports both URL-based document extraction and local stream upload. Maps `CandidateProfile` → `ParseResumeResponseDto` (`ParsedData` DB JSON column). Fully verified with 111 passing tests.
- ⬜ Screening (`/recruitment/screening/screen`) with Auto Shortlist Threshold
- ⬜ Ranking (`/matching/rank`) for the RANK column
- ⬜ Interview questions (`/recruitment/interview/questions`)
- ⬜ Rubric scoring (`/scoring/score`) — badge "provisional" until `rubric_signed_off`
- ⬜ Handoff doc for the supervisor

### Frontend
- ⬜ Revamped recruitment portal (Angular 19 + Material 3 + Tailwind)

---

## 10. JD generation — binding rules (do not violate)

The `/recruitment/jobreq/generate` response maps 1:1 onto the 4-step wizard.

1. **`execution_time_ms` binds to `int`/`long`**, never a float —
   `System.Text.Json` throws otherwise.
2. **Nulls are meaningful.** `age_limits`, `benefits`, `justification`,
   `employment_type`, `grade`, `budget_type`, `budget_line_id`, `closing_date`
   are **null by design** — they belong to HR. Never substitute defaults.
   *`age_limits` especially:* age is a protected attribute; an AI proposing an
   age band in a job ad is discriminatory and indefensible under the EU AI Act.
3. `job_title`, `department`, `designation` are **verbatim echoes** — bind
   straight back to the dropdowns.
4. `status` is always `"Draft"`; `is_public_job` always `false`. A human publishes.
5. `vacancies` is always `1` — a starting value for the human.
6. Wizard fields are **pre-filled and editable**, with a subtle
   "AI-generated — please review" affordance driven by `review_required`.
7. `meta` is additive — ignore unknown keys, never fail deserialization.

**Request:** `jobTitle` is the **only required field**. Omit what HR left blank —
never send `"N/A"`, `"-"` or `"string"`. Always send `jobCategoryOptions` (the
dropdown's allowed values) so the answer snaps to a bindable option.

---

## 11. Daily runbook — start and stop a work session

Start in this order (the API reads the DB at startup, so the DB goes first).

### Start

```bash
# 1. DATABASE — Docker Desktop must be running first
docker start rainmaker-mssql                 # ~10 s to accept connections
docker ps --filter name=rainmaker-mssql      # confirm "Up"

# 2. BACKEND — own terminal tab, stays in the foreground
cd ~/Documents/Multinet/rainmaker-hrms/Backend/RM/Digi.Recruitment.Module
dotnet run                                   # → http://localhost:5019/swagger
# healthy when you see:  ✅ Loaded 7 permissions from database
#                        Now listening on: http://localhost:5019

# 3. FRONTEND — another terminal tab
cd ~/Documents/Multinet/rainmaker-hrms/Frontend
npm start                                    # → http://localhost:4200
```

### Stop

```bash
# BACKEND / FRONTEND — Ctrl+C in their terminal tabs (graceful).
# If a tab was closed and something is still holding a port:
lsof -ti:5019 | xargs kill        # backend
lsof -ti:4200 | xargs kill        # frontend

# DATABASE — stop keeps all data; it is on a named volume.
docker stop rainmaker-mssql
```

Quitting Docker Desktop also stops the container cleanly. Data survives either
way — it lives in the `rainmaker-mssql-data` volume, not the container.

**Never `docker rm` the container without meaning it.** `stop` = pause,
`rm` = delete the container (volume survives), `docker volume rm
rainmaker-mssql-data` = delete the data.

### If Docker hangs

A container that fails to start leaves a `docker start`/`docker run` process that
never returns and **serialises every later docker command behind it** — one bad
image then looks like a broken machine. This cost hours once; it is a 20-second fix:

```bash
ps -eo pid,command | grep -E 'docker (run|start)' | grep -v grep   # find PIDs
kill -9 <pid>                                                      # clear them
# then, if still unhappy: quit and relaunch Docker Desktop
```

### Other commands

```bash
cd Backend && dotnet build RM/Digi.Recruitment.Module/Digi.Recruitment.Module.csproj
cd Backend && dotnet test  RM/Digi.Recruitment.Module.Tests/Digi.Recruitment.Module.Tests.csproj

# Query the DB from the shell
docker exec -it rainmaker-mssql /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'Multinet@123!' -C -d InternDB
```

Azure Data Studio connects with: Server `localhost,1433`, SQL Login, user `sa`,
password `Multinet@123!`, **Trust server certificate = true**.

Offline development: set `MultinetAI:StubMode=true` in
`appsettings.Development.json` — the stub returns contract-shaped responses with a
deliberately mixed provenance map, so review-UI flagging is exercised every run.
`StubMode` must never be true in production.

---

## 12. Local database — working setup

**SQL Server 2022, not Azure SQL Edge.** Edge is retired, is the SQL 2019 engine,
and **core-dumps ~8 s into startup on this machine even with a brand-new volume**
(verified twice). SQL Server 2022 is amd64-only, so it runs under Rosetta — which
works fine; an earlier conclusion that it "can't start under emulation" was wrong
and came from testing while the Docker daemon was wedged.

```bash
docker run -d --name rainmaker-mssql --platform linux/amd64 \
  -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=Multinet@123!' -e 'MSSQL_PID=Developer' \
  -p 127.0.0.1:1433:1433 -v rainmaker-mssql-data:/var/opt/mssql --restart=no \
  mcr.microsoft.com/mssql/server:2022-latest

# apply the demo schema (see the DEMO-ONLY warning in each file)
for f in 001_demo_schema 002_demo_permissions; do
  docker cp db/seed/$f.sql rainmaker-mssql:/tmp/$f.sql
  docker exec rainmaker-mssql /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P 'Multinet@123!' -C -b -i /tmp/$f.sql
done
```

Verified: .NET connects from the host in **218 ms**, queries in **102 ms**.

**If Docker hangs:** a failed container start leaves a `docker start`/`docker run`
process that never returns and **serialises every later docker command behind it**,
which makes one bad image look like a broken machine. Fix:
`ps -eo pid,command | grep 'docker \(run\|start\)'` → `kill -9` those PIDs, then
restart Docker Desktop if needed.

**The seed is demo scaffolding.** Column names are exact (taken from the
repository's inline SQL); types are guesses. Only the AI settings / job-description
/ activity paths are supported — everything stored-procedure backed (requisitions,
applications, interviews, dashboards) still needs the real `InternDB.bak`.

---

## 13. Outstanding — pick up here

1. **RESOLVED — end to end verified.** The startup hang was confined to the agent's
   shell environment; `dotnet run` from a normal terminal works. Verified against
   SQL Server 2022 with the demo schema:

   | Step | Result |
   |---|---|
   | `SaveApiKeySettings` provider=`multinetai` | saved; key stored **encrypted** |
   | `CheckApiKeyStatus` | `hasApiKey: true, provider: multinetai` |
   | `TestApiKey` | `status: valid`, 6 capabilities |
   | `GenerateJobDescription` | full 4-step draft; all 7 binding rules held |
   | DB rows written | Settings 1, JobDescriptions 1, Activity 1 |

   Binding rules confirmed live: `executionTimeMs` bound as int; verbatim echoes of
   title/department/designation; `jobCategory` snapped to a supplied dropdown
   option; `vacancies: 1`; **`ageLimits: null`**; every null-by-design field null
   and surfaced via `fieldsForHumanToComplete`; `status: Draft`, `isPublicJob: false`.

   **Caveat: this ran against the STUB AI client**, so a real generation round trip
   is still unproven — it needs the production API key. Tell-tale: the stub's
   naive comma-split returned
   `["JavaScript", "Python and .NET or maybe Angular+C# etc"]`, whereas the real
   service normalises that to `["JavaScript","Python",".NET","Angular","C#"]`.
2. **A real AI key for local use** — the only way to prove an actual generation.
   Ask the supervisor, then set `MultinetAI:StubMode=false` and
   `MultinetAI:ApiKey` in `appsettings.Development.json` (gitignored).
3. **Endpoints hit locally** need a JWT. The module is `[Authorize]` +
   `[ModuleAuthorize("RECRUITMENT_")]`; a token with `UserName: superadmin` signed
   with the dev `Jwt:SecretKey` (issuer `DigiSoftERP`, audience `DigiSoftERPUsers`)
   bypasses the module check.
4. **`custom` provider has no backend** — see §6. Groq/DeepSeek and most others
   speak OpenAI's `/chat/completions`, so one generic implementation covers them.
7. **Multinet AI Resume Parser & Angular Frontend Screens Completed (2026-08-03)**:
   - **Backend**: Implemented `ExtractResumeByUrlAsync` and URL-based resume parsing (`parser/extract-url` endpoint) in `MultinetAiClient.cs` and `RecruitmentAIService.cs`.
   - **Database**: Applied complete Stored Procedures seed `003_demo_recruitment_sps.sql` to SQL Server (`InternDB`), creating tables (`Tbl_Ruc_RecruitmentRequisition`, `Tbl_Ruc_JobApplication`, `Tbl_RecruitmentAI_ResumeParsing`, `Tbl_RecruitmentAI_Screening`) and SPs (`sp_Hr_Ruc_RecruitmentRequisition_Insert`, `SP_Ruc_JobRequisition_Create`, `SP_Ruc_JobApplication_Create`, `SP_Ruc_RecruitmentAI_ResumeParsing_Save`).
   - **Frontend**:
     - Enabled **Save & Publish Requisition** on Step 4 of `/recruitment/job-create` $\rightarrow$ calls `POST /recruitment/api/RecruitmentAI/SaveJobDescription`.
     - Built **Job Requisitions Screen** (`/recruitment/jobs` & `/recruitment/job-requisitions`).
     - Built **Applications Management Screen** (`/recruitment/applications`).
     - Built **Upload Resume & AI Extraction Review Screen** (`/recruitment/upload-resume`) following Google Stitch B1 design spec (`STITCH_PROMPTS.md`).
     - Built **Application Details Screen** (`/recruitment/application-details`) matching live production portal (`devhrms.rainmaker.pk/hr/recruitment-professional/application-details`) with `Details`, `Resume` PDF viewer, and `Timeline` tabs.

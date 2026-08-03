# MASTER PROMPT — Integrate the Multinet HRMS AI Service into the Rainmaker Portal Backend

You are acting as Senior .NET Backend Engineer on **HRMS Rainmaker** (Multinet).
Your mission: wire the portal's recruitment AI features to Multinet's **in-house
AI service**, which is already **LIVE in production**, one feature at a time,
starting with **JD generation**. Everything below about the AI service is
verified against the running production system — treat it as the contract.

You are working in the **portal backend** (ASP.NET / IIS, `stagginghrms` /
`devhrms.rainmaker.pk`) and its Angular frontend. **You do not have access to
the AI service's source code and do not need it** — it is a separate service
owned by another team. This document is your complete specification.

---

## 0. What the AI service is (and what it is NOT)

`hrms-ai-service` is Multinet's **sovereign, in-house AI platform** — the
company's own alternative to OpenAI/Anthropic APIs, running on Multinet's GPU
server. It serves resume parsing, JD generation, candidate screening, ranking,
rubric scoring, interview-question generation, and the Performance Management
(PMP) module already used by the portal.

**It is NOT an OpenAI-compatible provider.** The portal's "AI Recruitment
Settings" page was designed for bring-your-own-OpenAI (Provider / Model /
Max Tokens / Temperature). That model does not apply here, and §2 tells you
exactly which of those fields matter and which are ignored.

**Critical design principle you must respect: every AI output is ADVISORY.**
Responses carry `review_required: true` (and `advisory: true` where a number
could be mistaken for a decision). A human always edits and approves. Never
auto-commit AI output to a requisition, an application status, or an
evaluation of record. This is a legal requirement, not a preference:
AI-assisted hiring is high-risk under the EU AI Act (Annex III) and the
service is built so a human decides. Do not design around it.

---

## 1. Connection basics

**Base URL (production):**
```
https://ai.rainmaker.pk/hrms/api/v1
```

**Authentication:** header `X-API-Key: <key>` on **every** call.
- One key per consuming team; the portal already has a valid key configured
  (verified). The key is stored encrypted portal-side; never log it, never put
  it in a URL, never return it to the browser.
- Missing/wrong key → `401 {"detail": {"error": "unauthorized"}}`.

**HTTP client configuration (non-negotiable):**
- **Timeout: 180 seconds** on all generation calls. This is not a slow service;
  it is a *large local model*, and honest generation takes time.
- Measured production latency: **cold ~20–35 s** (first call after idle),
  **warm ~11–20 s** — including an identical repeat. **Every generation call
  produces a FRESH draft** (changed 2026-08-03): a recruiter clicking Generate
  again on the same inputs gets a new formulation, exactly like the OpenAI /
  Anthropic APIs this platform mirrors, never a byte-identical cached memo.
  `meta.cache_hit` therefore reads `false` in normal operation; treat the
  `x-recruitment-cache` header as diagnostic only. Do NOT render a "cache hit"
  badge in the UI.
- Retry policy: retry on `429`, `502/503/504`, and timeouts with backoff
  (max 2 retries). **Never retry a `422`** — that means the request or the
  generated content was rejected; retrying produces the same result.
- The service processes GPU work **serially**. Do not fan out parallel
  generation calls; queue them. A `429` is the service telling you to wait.

**TLS/edge notes:** `https://ai.rainmaker.pk/hrms/*` is nginx in front of the
service. Ops endpoints (`/health`, `/ready`, `/docs`, `/openapi.json`) are
deliberately **404 at the edge** — they are on-box only. Do not build health
checks against them; use `GET {base}/auth/verify` instead (§3).

---

## 2. Fixing the "AI Recruitment Settings" page

The page currently stores a single "API Endpoint". **That field must hold the
BASE URL**, because the service exposes several feature endpoints and one URL
cannot serve them all. Your backend appends the per-feature path.

| Field | Correct value | Behaviour |
|---|---|---|
| AI Provider | `Custom API` | correct as-is |
| Model | `qwen3.5:27b` | **Ignored by the service.** Informational only. |
| API Key | the existing key | verified working; leave blank to keep current |
| **API Endpoint** | `https://ai.rainmaker.pk/hrms/api/v1` | **base URL, no trailing slash** |
| Max Tokens | any value | **Ignored.** The service budgets tokens per endpoint (JD generation needs ~1700; a client cap of 1000 would truncate it, which is precisely why the service owns this). |
| Temperature | any value | **Ignored.** Pinned to `0.0` server-side: structured extraction must be deterministic, not creative. |
| Auto Shortlist Threshold | `80` | **Honoured** — send it with screening calls; the response echoes `threshold_used`. |

**Two bugs to fix on this page:**
1. The helper text says *"Prefer full path `https://ai.rainmaker.pk/hrms/api/query`"*.
   **There is no `/api/query` endpoint — it returns 404.** Replace the text with:
   "Base URL only (e.g. `https://ai.rainmaker.pk/hrms/api/v1`); the backend
   appends the feature path."
2. The **Model** field has been used to hold arbitrary text (an email address in
   one case). Validate it or make it a read-only display of the configured model.

**Feature toggles** (Auto Resume Parse / Screening / Matching / Interview
Questions) are **portal-side orchestration**. The service is stateless
per-request: a toggle decides whether your backend makes the call, nothing more.

---

## 3. TASK 1 — Fix the "Test API Key" button (do this first, it is 30 minutes)

**Current state: broken, and not the AI service's fault.** The portal POSTs to
its own backend at
`/recruitment/api/RecruitmentAI/TestApiKey`, which **returns 404 — that
controller does not exist.** The AI service is never contacted, and the UI
shows "API Key Invalid" misleadingly.

Implement that controller. It must call:

```
GET  {configured_base}/auth/verify
Header: X-API-Key: {stored key}
```

- `200` ⇒ key is valid. Body:
  ```json
  {
    "valid": true,
    "service": "hrms-ai-service",
    "service_version": "1.1.0",
    "schema_version": "1.2.0",
    "capabilities": ["parser.extract","recruitment.jd.generate",
      "recruitment.jobreq.generate","recruitment.screening.screen",
      "recruitment.interview.questions","matching.rank","scoring.score",
      "pmp.goals.generate","pmp.recommendations.generate"]
  }
  ```
- `401` ⇒ key invalid.
- Anything else / timeout ⇒ report "service unreachable", **not** "key invalid".
  Distinguish these in the UI; conflating them wasted real debugging time.

This endpoint costs **zero GPU** and returns in milliseconds, so it is safe to
call on every settings save. Use the `capabilities` array to enable/disable
feature toggles instead of hard-coding them.

---

## 4. TASK 2 — JD generation (the "Generate Job Description with AI" button)

This is the flagship feature and the one to get working end to end first.

**Endpoint:** `POST {base}/recruitment/jobreq/generate`

### Request (camelCase; snake_case also accepted)

```json
{
  "companyId": 133,
  "jobTitle": "Software Developer",
  "department": "Information Technology",
  "designation": "System Administrator",
  "experienceRequired": "3 - 6 years",
  "keySkills": "JavaScript, Python and .NET or maybe Angular+C# etc",
  "jobCategoryOptions": ["UI/UX","Laravel Developer","Ai Developer",
                         "Dot Net Developer","Python Developer",
                         "Odoo Consultant","Network Engineer"],
  "additional_context": {
    "team_goals": "Building high-performance microservices.",
    "location_preference": "Karachi, Pakistan (Hybrid)"
  }
}
```

- **`jobTitle` is the ONLY required field.** Everything else is optional; send
  what the HR user filled in and omit the rest. The service is deliberately
  tolerant — incomplete ERP payloads must never fail. Do not send placeholder
  strings like `"N/A"`, `"-"`, `"string"`; omit the field or send `null`.
- **`keySkills`** accepts the messy prose HR actually types, or a string array.
  The service normalises it: the example above yields
  `["JavaScript","Python",".NET","Angular","C#"]` — conjunctions and hedge words
  ("and", "or", "maybe", "etc") removed, compound tokens preserved
  (`.NET`, `C#`, `C++`, `CI/CD`, `Node.js`, `ASP.NET Core`, `Vue 3`), and the
  recruiter's casing kept verbatim.
- **`experienceRequired`** is free text: `"3 - 6 years"`, `"1-2"`, `"5+ years"`,
  `"Fresh"` all parse. **When you send it, the service uses YOUR numbers** — it
  will not contradict a value a human typed. Omit it and the model derives a
  sensible range from the role and designation.
- **`jobCategoryOptions`** — SEND THE DROPDOWN'S ALLOWED VALUES. The service
  then snaps its answer to a real option so it always binds, or returns `null`
  when genuinely ambiguous. Without this list you get free text that your
  dropdown may not accept.
- **`additional_context`** is a free-form object (any keys) or a string or
  `null`. Use it for anything extra the portal knows.

### Response — maps 1:1 onto the 4-step wizard

```json
{
  "status": "success",
  "execution_time_ms": 20909,
  "companyId": 133,
  "review_required": true,
  "data": {
    "step_1_basic_info": {
      "job_title": "Software Developer",
      "department": "Information Technology",
      "designation": "System Administrator",
      "job_summary": "We are seeking a Software Developer to ...",
      "job_category": "Software Engineering",
      "vacancies": 1,
      "employment_type": null,
      "grade": null
    },
    "step_2_requirements": {
      "experience_years": { "minimum": 3, "maximum": 6 },
      "age_limits": { "minimum": null, "maximum": null },
      "key_responsibilities": ["...", "..."],
      "requirements": ["...", "..."],
      "qualifications": ["...", "..."],
      "skills": ["JavaScript","Python",".NET","Angular","C#"]
    },
    "step_3_compensation": {
      "location": "Karachi, Pakistan",
      "benefits": null,
      "budget_type": null,
      "budget_line_id": null
    },
    "step_4_publishing": {
      "justification": null,
      "is_public_job": false,
      "status": "Draft",
      "closing_date": null
    }
  },
  "meta": { "service_version": "...", "cache_hit": false,
            "experience_source": "parsed_from_request",
            "job_category_source": "selected_from_options",
            "job_title_source": "verbatim_request",
            "requested_job_title": "Software Developer",
            "work_mode": "Hybrid", "repairs": { } }
}
```

### Binding rules you MUST honour

1. **`execution_time_ms` is an `int`.** Bind it to `int`/`long`.
   (`System.Text.Json` throws on a float here — this was deliberately fixed.)
2. **Nulls are meaningful and intentional.** `age_limits`, `benefits`,
   `justification`, `employment_type`, `grade`, `budget_type`,
   `budget_line_id`, `closing_date` are **null by design** — those fields
   belong to HR, and the AI is forbidden from filling them. Leave the wizard
   inputs empty for the human. **Never** substitute your own defaults and
   never ask the AI team to "just generate them".
   *Why `age_limits` in particular:* age is a protected attribute; an AI
   proposing an age band in a job ad is discriminatory and, under the EU AI
   Act's high-risk hiring rules, indefensible. The service enforces this.
3. **`department` and `designation` are verbatim echoes** of what you sent —
   bind them straight back to the dropdowns; they will always match.
   **`job_title` may come back CORRECTED** (added 2026-08-03): the recruiter
   types the title free-text and fast — "hr office", "snr softwre developr" —
   and the service returns the professionally formatted form ("HR Officer",
   "Senior Software Developer"). The correction is deterministic-guard
   verified: it can fix spelling, casing and abbreviations only, never change
   the role, and every rejected proposal falls back to the verbatim echo.
   Bind `data.step_1_basic_info.job_title` into the Job Title field as an
   AI-suggested (orange) value the human reviews. Provenance is in
   `meta.job_title_source` (`"verbatim_request"` | `"corrected_from_request"`)
   and `meta.requested_job_title` always carries what the recruiter typed —
   log the pair if you record anything about the requisition.
4. **`status` is always `"Draft"`** and `is_public_job` always `false`. A human
   publishes; the AI never does.
5. **`vacancies` is always `1`** — a starting value for the human to change.
6. Populate the wizard fields as *pre-filled and editable*. The HR user must be
   able to change every single one before saving. Show a subtle
   "AI-generated — please review" affordance, driven by `review_required`.
7. `meta` is additive and may gain keys; ignore unknown ones rather than
   failing deserialization (`JsonSerializerOptions` should not be strict here).

### Step-by-step for this task
1. Add a typed client (`IHttpClientFactory` + Polly) with base address from
   settings, `X-API-Key` header, 180 s timeout, retry-on-429/5xx.
2. Add a DTO set mirroring the response above (nullable reference types on the
   null-by-design fields).
3. Controller action: take the AI-form fields → build the request → call → map
   into the wizard view-model → return.
4. Test with the exact payload above and confirm every binding rule.
5. Only then wire the Angular button, with a spinner that tolerates 35 s and a
   clear error path.

---

## 5. TASK 3+ — the remaining features (same client, same auth)

Do these one at a time, in this order, each fully working before the next.

### 5.1 Resume parsing — `POST {base}/parser/extract-url` (primary) and `POST {base}/parser/extract`
**`/parser/extract-url` is the route built for the portal's actual flow**
(added 2026-08-03): the portal stores the uploaded résumé on its own web
storage and sends the service the URL — no re-upload of the bytes.

```json
POST {base}/parser/extract-url
{ "documentUrl": "https://stagginghrms.rainmaker.pk/storage/<tenant>/recruitment/resumes/documents/<file>.pdf",
  "candidateId": "…", "applicationId": "…", "requisitionId": "…", "companyId": "…" }
```
- `documentUrl` is the only required field; the ids are optional and echoed
  back verbatim at the top level so you can bind the result to your own row.
- The URL's host must be portal storage (`*.rainmaker.pk` subdomains); the
  service verifies scheme, host, resolved addresses, redirects, size (20 MB)
  and the file's magic bytes before anything reaches the parser.
- Extra errors beyond the upload route: `422 document_url_invalid` /
  `document_url_not_allowed` (the URL is wrong — fix the caller),
  `502 document_unreachable` (your storage did not answer — retryable),
  `503 document_fetch_not_configured` (instance has URL ingestion off).
- Success adds `meta.source` (`kind/host/bytes/redirects/content_type`).
- Call it **asynchronously with a spinner**: a scanned résumé takes ~40 s
  (OCR + vision); text-layer PDFs are faster. One document at a time by
  design — bulk import must queue, never fan out.

`/parser/extract` remains for raw bytes: `multipart/form-data`, field name
**`file`**. PDF/DOCX/PNG/JPG, ≤ 20 MB (portal caps at 5 MB, which is fine).
Both return the same body:
`{ "status": "success", "data": { …candidate profile… }, "meta": { … } }`.
- `data` is the parsed profile: `name, email, phone, location, headline,
  summary, spoken_languages[], links[], skills[], education[], experience[],
  projects[], certifications_and_awards[]`.
- **`meta.field_provenance`** tells you which fields came from a deterministic
  backstop rather than the model — **use it to highlight fields for reviewer
  attention** in the review UI. This is a quality feature; don't drop it.
- Accuracy is certified at ~95% field-level on a labelled corpus. It is
  excellent, not perfect: a human review step is required.
- Errors: `413 file_too_large`, `422 unsupported_file_type` /
  `content_type_mismatch` (content sniffed, not just the extension) /
  `extraction_failed`.

### 5.2 Screening — `POST {base}/recruitment/screening/screen`
Request: `candidate_profile` (the `data` object from parsing — pass it through
as-is), `job_title`, `job_requirements` (string or array of lines), optional
`key_skills[]`, `experience_required`, **`threshold`** (send the portal's Auto
Shortlist Threshold), and optional `requisition_id` / `application_id` /
`candidate_id` (echoed back for correlation).

Response: `match_score` (0–100 int), **`shortlisted`** (bool),
`threshold_used`, `matched_skills[]`, `missing_skills[]`,
`reasons[{kind: "match"|"gap", detail, evidence}]`, plus
`review_required: true` and `advisory: true`.
- **`shortlisted` is computed by the service as `match_score >= threshold`** —
  arithmetic, not a model opinion. Changing the threshold changes the decision
  with no AI involvement.
- **It is a suggestion.** Render it as a recommendation a recruiter accepts or
  overrides in Applications Management. Do **not** auto-reject anyone.
- `reasons[].evidence` is verified to appear in the candidate's own profile
  text — show it, it is what makes the score trustworthy.

### 5.3 Candidate ranking — `POST {base}/matching/rank`
For the RANK column in Applications Management. Embeddings-based, fast (no
large generation). Send the JD text and the candidates; use the returned rank
and 0–100 score. Advisory — it is a sort aid, not a decision.

### 5.4 Interview questions — `POST {base}/recruitment/interview/questions`
Request: `job_title`, `job_description` (string or array), optional
`key_skills[]`, `candidate_profile` (for candidate-specific questions),
`questions_per_category` (default 5), `categories[]` from
`technical | behavioral | role_specific`.
Response: `question_bank` keyed by category; each item is
`{ question, what_to_listen_for, grounded_in: "jd"|"candidate_profile" }`.
Panel members pick and edit; nothing is auto-asked.

### 5.5 Rubric scoring — `POST {base}/scoring/score`
Candidate-evaluation assist. ~60 s. Response includes per-criterion scores with
justifications and `rubric_signed_off`. **While `rubric_signed_off` is `false`,
badge the score "provisional" in the UI** — the scoring rubric is pending HR
sign-off. The evaluation of record remains the human panel's.

---

## 6. Error handling — implement once, centrally

| Status | Body | Meaning | Your action |
|---|---|---|---|
| `401` | `{"detail":{"error":"unauthorized"}}` | bad/missing key | surface "AI key invalid"; do not retry |
| `413` | `file_too_large` | upload too big | user-facing size error |
| `422` | `{"detail":{"error":"<slug>"}}` | rejected input or unusable generation (`generation_failed`, `unsupported_file_type`, `content_type_mismatch`, `extraction_failed`, `malformed_request`) | show a friendly message; **never retry** |
| `429` | `{"error":"busy","retry_after_s":N}` + `Retry-After` | GPU saturated | wait and retry; UI: "AI is busy, retrying…" |
| `503` | `{"error":"llm_unreachable"}` | model backend down | retry with backoff; alert ops if sustained |
| `500` | `{"error":"internal_error"}` | unexpected | log correlation id, show generic error |

Error bodies are **deliberately sanitized** — they never contain prompts,
file paths, model internals, or candidate data. Do not expect detail there;
the AI service keeps full detail in its own server-side logs.

---

## 7. How the AI service is deployed (context for debugging)

- Runs on Multinet's own GPU server as a systemd service on `127.0.0.1:8020`,
  fronted by nginx at `https://ai.rainmaker.pk/hrms/*` (TLS terminated there).
  The service is never exposed directly.
- One resident local model (`qwen3.5:27b`) serves all features — no per-token
  vendor cost, and **no candidate data ever leaves Multinet infrastructure**.
  That data-sovereignty property is a selling point; don't undermine it by
  routing anything through third-party APIs.
- The model is held resident (24 h keep-alive) with a startup warm-up, so
  latency is predictable. First call after a service restart may still be slow.
- The PMP module on the same service is already live and consumed by the
  portal's Performance Management screens:
  `POST {base}/pmp/goals/generate`, `POST {base}/pmp/recommendations/generate`
  (alias `POST {base}/pmp/insights/evaluate`), `GET {base}/pmp/status`.
  Follow the same client/auth/error patterns — don't invent a second style.

---

## 8. Working agreement for this session

1. **One feature at a time**, verified end to end before moving on. JD
   generation first (§4), Test API Key before that (§3) since it unblocks
   settings.
2. **Verify against the real service** with curl/Postman before writing Angular
   code, so you know whether a bug is yours or a mapping error.
3. **Never invent an endpoint path.** Everything is listed above; if you need
   something not listed, ask — do not guess (`/api/query` was guessed, and it
   cost debugging time).
4. **Never weaken the advisory model** to make a flow smoother: no
   auto-publishing requisitions, no auto-rejecting candidates, no overwriting
   the null-by-design fields.
5. Secrets: the API key lives in secure configuration (user-secrets / env /
   vault), never in source, never in appsettings committed to git, never sent
   to the browser.
6. Log AI calls with correlation ids, duration, status, and
   `meta.cache_hit` — you will want that when someone asks why a call took
   30 seconds.
7. When the AI service's behaviour looks wrong (a bad field, an odd score,
   discriminatory text), **report it with the exact request payload** — the AI
   team's quality gates are driven by reproducible cases.

Author header for git/docs: "Prepared by Syed Taha, Multinet".

# MASTER PROMPT — Build HRMS Rainmaker ERP (.NET backend + Angular frontend)

You are acting as Principal Full-Stack Engineer on HRMS Rainmaker at Multinet.
Your mission: build the **`hrms-backend`** (.NET) and **`hrms-frontend`**
(Angular) applications in this repo from their current empty-placeholder
state to a working, error-free, integrated product — consuming the existing
AI microservice through the FROZEN contract in §3. Work step-by-step, verify
each step before the next, and never mark a step done without running it.

---

## 1. Context: what this is part of

Multinet is building an in-house "AI brain": a suite of sovereign AI
microservices on company GPUs, exposed to consumers as metered API keys
(the OpenAI-style model, but in-house). The first service,
**hrms-ai-service** (this repo, Python/FastAPI), is a resume-parsing +
candidate-matching + rubric-scoring engine:

- **Certified 93.9% field-level accuracy** on a 19-resume eval corpus
  (every layout class: multi-page, multi-column, scanned, creative), with
  an eval-driven methodology (gates, sentinels, per-field scoring) — the
  engine keeps improving in a PARALLEL session; you do not touch it.
- Production-hardened API (v1.1.0): fail-closed X-API-Key auth, /ready
  probe, sanitized errors, magic-byte upload validation, 49-test suite.
- Inference runs on an in-house GPU server (shared box) via Ollama
  (qwen3.5:27b, version-pinned). A sibling service (lms-ai-service) is
  already live in production behind nginx at its own hostname — the same
  go-live playbook will apply to hrms-ai-service later.

The ERP you are building is the human-facing half: recruiters upload
resumes, review AI-parsed profiles, rank candidates against JDs, and see
rubric scores. Tracker tasks 1.8 (.NET parsing service) and 1.9 (Angular
review UI) govern scope; their DoDs are in §5.

## 2. Working rules (non-negotiable)

- **Git**: commit with the user (Syed Taha Zaidi) as SOLE author. NO AI
  co-author trailers. Small green commits, one concern each.
- **Local-only**: nothing leaves this machine. No published artifacts, no
  external services, no telemetry. Handoff docs are local files with the
  byline "Prepared by Syed Taha, Multinet".
- **PII firewall**: never commit resume files, parsed candidate JSON, or
  anything derived from them. Check .gitignore before every commit. Use
  synthetic/dummy resumes for demos and tests (generate your own PDFs).
- **Secrets**: API keys live in .env / appsettings.Development.json
  (gitignored) / user-secrets. Never in committed code or docs.
- **Do NOT modify `hrms-ai-service/`** — it is owned by a parallel session.
  If the contract in §3 seems wrong or missing something, STOP and tell
  the user what you need changed; do not work around it.
- Step-by-step verification: every phase ends with a runnable check
  (build passes, tests green, curl succeeds, page renders).

## 3. THE INTEGRATION CONTRACT (frozen — build against exactly this)

### 3.1 Service location & auth
- Base URL: env-configured. Local dev default: `http://127.0.0.1:8000`.
  (Production hostname/port comes later; treat as config, never hardcode.)
- Every business endpoint requires header **`X-API-Key: <key>`** → 401
  `{"detail":{"error":"unauthorized"}}` otherwise. `/health`, `/ready`,
  `/version` are open.
- `GET /health` → `{status, service, version}` (liveness only)
- `GET /ready` → 200 `{status:"ready", llm_backend:{reachable,version},
  model, backend}` or 503 `{status:"not_ready", ...}` — gate parse
  submissions on this.
- `GET /version` → `{service_version, schema_version, model, backend}`

### 3.2 Parse endpoint (the core integration)
`POST /api/v1/parser/extract` — multipart form, field name **`file`**.
Accepted: `.pdf .docx .png .jpg .jpeg`, ≤ 20 MB.

Success 200:
```json
{
  "status": "success",
  "data": { /* ProfileSchema v1.2.0 — see 3.4 */ },
  "meta": {
    "schema_version": "1.2.0",
    "extraction_route": "text | text+raw_fallback | ocr+vision_hybrid | vision | text+vision_escalated",
    "field_provenance": { "phone": "regex", "skills": "vision_escalation", "...": "..." },
    "stage1_docling_ms": 0.0, "stage3_ollama_ms": 0.0, "total_wall_ms": 0.0,
    "prompt_tokens": 0, "output_tokens": 0, "retries_used": 0,
    "docling_coverage": 1.0, "validation_passed": true
  }
}
```
Errors (body always `{"detail": {"error": <code>, "message": <safe text>}}`):
- 400 no filename · 401 unauthorized · 413 `file_too_large`
- 422 `unsupported_file_type` | `content_type_mismatch` |
  `extraction_failed` | `file_processing_error`
- 500 `internal_error`

### 3.3 CRITICAL runtime semantics (design the gateway around these)
- **Single-flight GPU lock**: the AI service processes ONE parse at a
  time. A parse takes **~40–90 s** (scans at the high end). Concurrent
  requests queue inside the service but DO NOT fan out requests.
- The .NET gateway therefore implements the queue (tracker 1.8): accept
  upload → persist blob + SHA-256 hash-dedupe → enqueue → background
  worker submits to the AI service ONE AT A TIME → persist result →
  expose job status. HTTP client timeout ≥ 180 s, retry on 5xx/timeouts
  (with backoff, max 2), never retry a 422.
- `meta.field_provenance` is the review-UI signal: any field whose
  provenance is not the LLM itself (e.g. `regex`, `vision_escalation`,
  `llm_unverified`) should be visually flagged for reviewer attention.
- `data` fields may legitimately be null/empty — sparse resumes are valid.

### 3.4 ProfileSchema v1.2.0 (response `data` shape)
```
name: str                          headline: str|null
email: str|null                    summary: str|null
phone: str|null                    spoken_languages: string[]
location: str|null                 links: string[]
skills: string[]
education:   [{institution, degree, duration|null, gpa|null}]
experience:  [{company, role, duration, location|null, achievements: string[]}]
projects:    [{name, technologies|null (comma-joined string), description: string[]}]
certifications_and_awards: string[]
```

### 3.5 Matching & scoring (secondary integration, same auth)
- `GET /api/v1/candidates` → `{count, candidates:[{profile_id, name}]}`
- `POST /api/v1/matching/rank` `{jd_text (≥30 chars), top_k (1–50)}` →
  `{model_version, section_weights, ranking:[...]}` (fast, embeddings)
- `POST /api/v1/scoring/score` `{profile_id, jd_text}` → rubric-scored
  result (**~60 s**, shares the GPU lock; response includes
  `rubric_signed_off: false` while the HR rubric is a placeholder — show
  scores as ADVISORY in the UI until that flag is true)

## 4. What to build

### 4.1 `hrms-backend/` — .NET 8 Web API ("gateway")
- Solution + single Web API project (controllers or minimal APIs — pick
  one style and stay consistent), EF Core + **SQLite** for the demo
  (swappable provider — keep all data access behind an interface).
- Features: resume upload endpoint (streams to blob dir + hash dedupe);
  parse-job queue (System.Threading.Channels; ONE consumer) + job status
  endpoint; typed AI-service client (HttpClientFactory + Polly, X-API-Key
  from config, timeout/retry per §3.3); candidates CRUD (persisted parsed
  profiles + review status); JD matching + scoring pass-through endpoints;
  `/healthz` that aggregates its own DB + AI-service `/ready`.
- The gateway is the auth boundary for the UI (its own simple auth is out
  of scope v1 — localhost demo; note it in README as a TODO for SSO).
- Unit tests for: hash dedupe, queue single-flight, AI-client error
  mapping (401/413/422/500 → domain errors), provenance pass-through.

### 4.2 `hrms-frontend/` — Angular (latest stable)
- Screens: (1) Upload — drag-drop, progress, job status polling;
  (2) Review — parsed profile form BESIDE the original document preview,
  fields flagged per provenance, inline edit, one-click Accept (DoD:
  recruiter completes review in < 60 s); (3) Candidates list — status
  chips (parsed/reviewed), search; (4) Match & Score — JD textarea, ranked
  results, per-candidate advisory score with rubric-pending banner.
- Clean, professional, fast. No UI library sprawl — Angular Material OR
  plain Tailwind-style CSS, one choice. Loading/error states everywhere;
  the 40–90 s parse must feel alive (status from the job endpoint), never
  frozen.

### 4.3 Integration & handoff artifacts
- `docs/INTEGRATION_GUIDE.html` (local file): endpoints consumed, config,
  run instructions, error handling, byline "Prepared by Syed Taha,
  Multinet".
- Root `README` additions: one-command dev bring-up for each app + the AI
  service env vars needed (`HRMS_API_KEYS`, `HRMS_REQUIRE_API_KEY`).
- A **stub mode** in the gateway (config flag): serves a canned
  ProfileSchema response so frontend/backend development NEVER blocks on
  the AI service being busy — and demos degrade gracefully.

## 5. Definition of done (verify each)
1. `dotnet build` + `dotnet test` green; `ng build` green.
2. With the AI service running locally (the user will start it and hand
   you a dev API key): upload a SYNTHETIC resume PDF end-to-end → parsed
   profile appears in the review UI with provenance flags → Accept →
   candidate listed. No errors in any console.
3. Stub mode demo works with the AI service completely offline.
4. Queue proof: submit 3 uploads at once → they process sequentially,
   statuses visible, none lost.
5. Error-path proof: wrong API key → clean UI error (not a spinner);
   oversized file → clean 413 message; AI service down → job parks as
   `waiting_for_service`, resumes when `/ready` returns 200.
6. `git log` — small commits, sole author, no PII, no secrets.

## 6. Ask the user when you need
- The dev API key (they generate it; never invent or commit one).
- Whether the demo DB should stay SQLite or move to a company DB engine.
- Any UI branding assets (logo/colors) — otherwise use a clean neutral
  Multinet-ish theme and note it's placeholder.

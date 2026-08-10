# Refactor State

**Last updated:** 2026-08-10
**Directive:** `RAINMAKER_MASTER_CONTEXT.md` (MASTER ARCHITECT DIRECTIVE — full text lives there; this file tracks status only, so the directive never needs to be re-pasted). Session-start protocol: read this file, and only the files it names below.

## Standing principle
This codebase is the foundation for a multi-year, multi-team enterprise ERP — not a one-off portal. Every structural decision optimizes for: easy to understand for a new engineer, easy to extend without touching unrelated modules, easy to migrate into the live build, aligned with mainstream .NET/Angular enterprise convention over bespoke patterns. Ambiguous choice → prefer boring and conventional.

## Whole-repo cleanliness sweep (2026-08-10) — done, not just the 4 phases' own scope
The 4 phases above only ever removed bloat their own audits happened to surface. This was a separate, exhaustive pass (2 parallel agents + independent verification of every finding via direct grep before touching anything) to confirm the rest of the tree is clean too.

**Removed, all confirmed zero live references, rebuild+test green after each step:**
- Backend: 8 orphaned files (unused `EmailSender`/`EmailTemplateService`, and a fully dead Firebase push-notification subsystem — service/repository/helpers/DTO/registration-helper, none wired into DI, one chain link the first pass missed and a rebuild caught).
- Backend: 4 dead repository methods (`GetAssignListAsync`, `GetAssignListJobReqAsync`, `CompleteRoundAndMoveToNextAsync`, `GetApplicationWorkflowStatusAsync` — each superseded by a live equivalent) plus their interface declarations and 6 cascading-orphan DTOs this created (`SchedulePanelAssignListDto`, `ScheduleAssignInterviewListDto`, `CandidateEvaluationCriteria_V1Dto`, `CompleteRoundRequestDto`/`ResponseDto`, `ApplicationWorkflowStatusDto`+`InterviewRoundStatusDto`) plus 2 leftover fully-commented-out duplicate DTO blocks plus their dead commented-out service-layer callers.
- Backend: 5 unused NuGet packages (`AutoMapper`, `ClosedXML`, `CsvHelper`, `ExcelDataReader`, `ExcelDataReader.DataSet`) — zero code references. Removing them broke 2 leftover `using DocumentFormat.OpenXml.*` directives that had no actual type usage (transitively resolved before only because one of the removed packages pulled that assembly in); removed those too rather than adding the package back — confirmed `RecruitmentAIService.cs`'s real DOCX handling uses reflection (`Type.GetType(...)`), same pattern as its PDF fallback, no compile-time package needed at all.
- Frontend: 4 fully unused primitive components (`RmCardComponent`/`RmButtonComponent`/`RmBadgeComponent`/`RmAiCardComponent` — an earlier primitive-library pass superseded by the `.rm-*` CSS-class approach used everywhere else), 2 unused exports (`PublicRequisitionQuery`, `AI_CAPABILITY`), 1 unused npm dependency (`@angular/platform-browser-dynamic` — bootstrap uses `platform-browser` directly).

**Also fixed:** `CLAUDE.md` itself had significant staleness predating this whole engagement — §7 still described the frontend as "a bare scaffold living in a different repo" (false), §6 referenced a renamed method, the endpoint table showed screening/interview-questions as unbuilt, §13 still listed the `custom`-provider gap as outstanding. All corrected — see the file directly, not duplicated here.

**Verified:** `dotnet build` — 0 errors throughout, including after the transitive-dependency break was caught and fixed. `dotnet test` — 138/138 held at every step. `ng build` — clean.

## Phase status

| Phase | Status |
|---|---|
| 1 — System Audit & Directory Reorganization | ✅ COMPLETE |
| 2 — Decoupled Universal AI Provider Gateway | ✅ COMPLETE |
| 3 — UI/UX Design System & Theme Engine | ✅ COMPLETE |
| 4 — Database & Stored Procedure Hardening | ✅ COMPLETE |

All four phases in the master directive are done and verified. Nothing is queued. Next action is whatever you direct next — this is a genuine stopping point, not a partial one.

---

### Phase 1 — System Audit & Directory Reorganization
Deliverable `docs/AUDIT_PHASE1.md` written and closed out (including both items originally marked Ambiguous, resolved by checking actual usage rather than guessing — see that file). Bloat scan, modular structure (`Digi.Shared`/`Digi.Core.AI`/`Digi.Recruitment.Module`, and `core`/`shared`/`features` on the frontend), and file-system check all done. **Already-verified, do not re-check:** no committed `bin/`/`obj/`, no `.orig`/`.bak` files, no duplicate DTOs found, `Digi.Core.AI` correctly wired into `RM.sln` and referenced by `Digi.Recruitment.Module.csproj`.

### Phase 2 — Decoupled Universal AI Provider Gateway
`IAIServiceProvider` (13 methods) implemented by 6 providers: `MultinetAiProvider`/`StubMultinetAiProvider` (pre-existing, real), and `OpenAiProvider`/`AnthropicProvider`/`GoogleGeminiProvider`/`CustomAiProvider` (built this engagement — previously 100% `NotImplementedException` stubs with hardcoded `Valid = true` key verification). Shared plumbing in `Digi.Core.AI/Providers/Generic/` (prompt templates, tolerant JSON parsing, client-side re-assertion of advisory invariants — age limits/draft status/etc. forced regardless of what the model returns). `RecruitmentAIService.cs`'s 5 AI-facing entry points (JD generation, resume parse, screening, interview questions, test-key) all route every provider through `_aiResolver`, replacing the old per-vendor dispatch that only Multinet had ever been migrated off of. `custom` has real working support for the first time (CLAUDE.md §6/§8 gap closed).

- **Resilience:** Polly retry (2 attempts, exponential backoff, excludes 4xx so 422 is never retried) on all 6 providers. **180s minimum timeout on every provider**, including the 4 generic ones — these had drifted to 60s during initial implementation; caught and fixed against this exact directive line (Phase 2 doesn't scope the 180s floor to Multinet only).
- **Transport normalization:** `ToTransportFileName` (ASCII ) still governs Multinet's multipart upload path, untouched. Generic providers never put a filename on an external wire at all (they extract text locally via `ResumeTextExtractor` and send it as a JSON prompt), so there is no filename-transport surface for them to fail on.
- **Model routing:** each company's saved `Model` setting now reaches every provider (`model` parameter added to the 5 generation methods, additive/non-breaking — appended last, before `cancellationToken`). Multinet ignores it (one resident model, by contract); generic providers use it or their own default.
- **Deliberately unsupported, not faked:** `RankAsync`/`ScoreAsync`/`ListCandidatesAsync` return `AiErrorCode.NotSupportedByProvider` on the 4 generic providers — no generic equivalent to Multinet's embeddings corpus or rubric engine exists, and approximating one would look real while resting on nothing.

### Phase 3 — UI/UX Design System & Theme Engine
Stack recon confirmed: Tailwind v4 (CSS-first, no `tailwind.config.js`), `tokens.css` **is** the `@theme` block, no competing component library, no parallel SCSS. `<rm-table>`/`<rm-drawer>` use content projection + Signals, consumed everywhere, no duplicates. Long-running AI operations (resume parsing) already had non-blocking signal-driven loading states (`isUploading`/`isParsing`) before this engagement — confirmed, not rebuilt.

- **Enforcement (§4.4) — was missing, now built.** `Frontend/scripts/check-design-tokens.js`, dependency-free (no stylelint added, per §4.0's dependency-approval rule), run via `npm run lint:tokens` and gated into `npm run build`.
- **Correction to this file's own prior claim:** an earlier pass here said "0 raw-hex/arbitrary-value matches" — that check was scoped to colors only. A full scan found 98 real violations (~55 arbitrary pixel font sizes, ~35 one-off page-container/component dimensions).
- **Text sizes: fully migrated.** 8 new tokens (`--text-10/11/13/15/17/19/28/38`, named by literal pixel value — inventing a semantic role like "caption" would be false precision when the same value serves different roles on different screens). Values matching Tailwind's own default scale exactly (12/18/20/24px) point at `text-xs/lg/xl/2xl` directly instead of a new token. All ~55 call sites migrated with identical pixel values — zero visual change.
- **One-off dimensions: deliberately excluded from enforcement, not tokenized.** A careers-page container width and a modal's width don't share a "change together" meaning — tokenizing them invents structure the design doesn't have. Documented inline in the script.
- **Verified:** `npm run lint:tokens` → 0 violations. `npm run build` → succeeds (also caught a real TypeScript error from the `createdOn`/`totalApplications` DTO rename below — the compiler is part of the verification loop too).

### Phase 4 — Database & Stored Procedure Hardening
Audited `RecruitmentAIRepository.cs` (26 SP calls) and `RecruitmentRepository.cs` (~90 SP calls) against every `db/seed/*.sql` definition — via 3 parallel read-only agents, then fixed everything in scope, then **functionally re-tested against the real running `rainmaker-mssql` container** (not just read — actual `EXEC` calls with assertions), including re-syncing that container, which had never had `005`/`006` fully applied (3-day-old drift from disk).

**Fixed, all live-tested:**
1. `SaveSettingsAsync` (feature toggles) was calling `SaveApiKeySettingsAsync`'s SP with an incompatible parameter set — threw on every call, and had it not thrown would have NULLed a company's saved API key. New `ruc.SP_Ruc_RecruitmentAI_FeatureSettings_Save` touches only the 6 toggle columns; refuses if no settings row exists yet.
2. `CreateJobRequisitionAsync`/`UpdateJobRequisitionAsync` (FK-normalized requisition DTOs) collided with `SaveAsync`/`UpdateAsync`'s legacy-shape SP names — only the legacy shape survived, so the normalized path always threw. New `ruc.SP_Ruc_JobRequisition_CreateDetailed`/`_UpdateDetailed`, draft-by-default (matches `006`'s discipline).
3. `AutoShortlistCandidateAsync` called a nonexistent SP name; wired to the real one (`SP_AI_AutoShortlistCandidate`, already in `003`) with its actual parameter/output set.
4. Two Dapper parameter-name bugs (trailing whitespace, missing `@` prefix).
5. `SaveActivityAsync`'s raw inline INSERT (the one write in `RecruitmentAIRepository.cs` still bypassing the stored-procedure-only convention against a table that actually exists) → new `ruc.SP_Ruc_RecruitmentAI_Activity_Save`.
6. HR's Step-4 justification text was silently dropped on every requisition save (`SaveJobDescriptionRequestDto` had no field for it, and `SaveAsync`'s own pass-through was commented out even though the SP already accepts it) — now persisted end to end.
7. Job requisitions grid always showed blank date/application-count, and the resume-parse review screen always showed blank job title/education duration+GPA — both were frontend↔backend field-name mismatches (`createdOn`/`totalApplications` vs `createdDate`/`applicationCount`; `Position`/`Field`/`Year` vs `role`/`duration`/`gpa`), not missing data. Aligned.
8. New/changed SPs benchmarked via `SET STATISTICS TIME ON`: 0-21ms elapsed per call — comfortably under the 50ms target at current demo data volume (a production-scale volume/index check is a separate exercise this workspace's demo DB can't meaningfully stand in for).

**Confirmed out of scope, not fabricated** (matches CLAUDE.md §12's own caveat that only AI-settings/job-description/activity paths are demo-ready): interview scheduling, panel assignment, evaluations, and the hire flow call SPs with no definition anywhere in `db/seed/*.sql` and some need schema this workspace doesn't have (employee onboarding tables). Also out of scope for the same reason: JobBank candidate matching/ranking (`SaveCandidateAIMatchAsync` etc. — no underlying table exists at all; separately, CLAUDE.md §5 already says `/matching/rank` isn't wired to live candidates regardless).

**Verified across every fix above:** `dotnet build` — 0 errors. `dotnet test` — 138/138. `ng build` — succeeds.

---

## Files most likely to matter next session
- `Backend/RM/Digi.Core.AI/Providers/` — the 6 provider implementations, if extending AI features.
- `Backend/RM/Digi.Recruitment.Module/Domain/Services/RecruitmentAIService.cs` — the 5 gateway-routed entry points, if changing AI-facing behavior.
- `db/seed/007_demo_realism_audit_fixes.sql` — every SP added this round; add new demo SPs here or a new numbered file, never edit `001`-`006` in place (they may already be applied elsewhere).
- `Frontend/src/app/core/theme/tokens.css` + `Frontend/scripts/check-design-tokens.js` — the design-system source of truth and its enforcement.

## Open items (none blocking)
1. Real AI provider API key still needed for a genuine non-stub end-to-end test (CLAUDE.md §13). Endpoints confirmed live and reachable; a key hasn't been provisioned for local dev.
2. `features/admin/`/`features/hrms/` remain deliberate scaffold shells — building real screens needs wireframes/Stitch prompts (§4.5), not a code task.
3. `core/api/recruitment*.ts` confirmed correctly placed in `core/` (used by both `careers` and `recruitment` features) — not moving.

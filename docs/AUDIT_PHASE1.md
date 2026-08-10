# Phase 1 — System Audit

> **Historical snapshot — Phase 1 only.** Phases 2-4 (AI providers, design tokens,
> stored-procedure hardening) have since been completed and are NOT reflected
> below. See `docs/REFACTOR_STATE.md` for current status; this file is kept for
> the record of what Phase 1 specifically found and fixed.

**Date:** 2026-08-09
**Scope:** directory reorg, module boundaries, bloat scan — per `RAINMAKER_MASTER_CONTEXT.md` §2.
**Verdict up front:** most of Phase 1 and a large chunk of Phase 2/3 are already done manually
(uncommitted). This audit found the seams, not a blank slate. Two real defects need fixing before
anything else: a broken test build and one dead template file. Everything else is confirm-only.

## Backend (`Backend/RM/`)

| Path | Finding | Bucket |
|---|---|---|
| `Digi.Recruitment.Module/Domain/AI/Multinet/` | Fully removed from working tree; superseded by `Digi.Core.AI`. | **Already in place** |
| `Digi.Recruitment.Module.Tests/Multinet/*.cs` (6 files) | Still `using Digi.Recruitment.Module.Domain.AI.Multinet`, reference `MultinetAiClient`/`StubMultinetAiClient`/`MultinetAiOptions` — types that no longer exist anywhere in the solution. **Confirmed: `dotnet build` on the test project fails with CS0234/CS0246** (main app project builds clean). | **Needs change** — delete or rewrite against `Digi.Core.AI.Providers.MultinetAiProvider`/`StubMultinetAiProvider` |
| `Digi.Core.AI/Class1.cs` | Stock `dotnet new classlib` template stub (`public class Class1 {}`), dead code. | **Needs change** — delete |
| `Digi.Core.AI/Configuration/`, `/Contracts/`, `/Providers/` | Coherent: `IAIServiceProvider` (13-method contract) implemented by 6 providers; DI wired via `ServiceCollectionExtensions` + `AiServiceProviderResolver`; csproj referenced from `Digi.Recruitment.Module.csproj`; `RM.sln` includes the project. Verified with a live build. | **Already in place** |
| `Digi.Core.AI/Providers/OpenAiProvider.cs`, `AnthropicProvider.cs`, `GoogleGeminiProvider.cs`, `CustomAiProvider.cs` | Every method is `=> throw new NotImplementedException(...)`. Only `MultinetAiProvider` and `StubMultinetAiProvider` have real logic. | **Needs change** — but this is Phase 2 scope (§3 of the master directive), not Phase 1. Flagging here, not fixing here. |
| `RecruitmentAIService.cs` | Routes every AI call through `IAIServiceProviderResolver` (e.g. `_aiResolver.Resolve(request.Provider).VerifyKeyAsync(...)`), no direct old-client references left. | **Already in place** |
| `RecruitmentAIRepository.cs` | Pure Dapper/SQL persistence; never called the AI client directly, nothing to migrate. | **Already in place** |
| `Digi.Shared/Middlerware/` (typo'd, old) → `Digi.Shared/Middleware/` (fixed) | Old folder gone; new folder has all 5 migrated files byte-identical except the namespace fix, plus 3 pre-existing files that were always there. Clean rename, not a duplication. | **Already in place** |
| `IDapperServices.cs`/`DapperServices.cs` (deleted) vs `IDapperService.cs`/`DapperService.cs` (kept) | Genuine dedup of a duplicate singular/plural pair; new interface retains all old methods. Self-documented with a comment explaining the removal. | **Already in place** |
| `bin/`/`obj/` under git | None committed (`git ls-files` confirms). | **Already in place** |
| `.orig`/`.bak`/`*_old*`/`*Copy*` files | None found under `Backend/RM`. | **Already in place** |

## Frontend (`Frontend/src/app/`)

| Path | Finding | Bucket |
|---|---|---|
| Tailwind wiring | v4.3.3, CSS-first config (no `tailwind.config.js`). `tokens.css` **is** the `@theme` block — not a restatement, the same file. Wired via `styles.css` → `angular.json`. | **Already in place** |
| `core/theme/tokens.css` | 129 lines. Color/status/shape tokens, semantic names (`--color-surface`, not `--color-blue-500`), full `.dark` override block. No spacing or shadow tokens defined yet. | **Already in place** (gap noted, not urgent) |
| Raw hex / arbitrary Tailwind values outside tokens.css | 0 matches across all templates/styles. | **Already in place** |
| `core/api/recruitment*.ts`, `recruitment-ai.service.ts` | **Resolved 2026-08-09.** Checked actual consumers: imported by both `features/recruitment/*` (8 components) **and** `features/careers/*` (2 components — the public job board reads the same recruitment domain API). It is genuinely cross-feature, not recruitment-exclusive. Moving it into `features/recruitment/` would create a `careers → recruitment` feature-to-feature import, which is the exact coupling the `core/shared/features` boundary exists to prevent. | **Already in place** — correct as-is, no move. |
| `features/admin/`, `features/hrms/` | **Resolved 2026-08-09.** Read the component: it explicitly states its own purpose in-template — *"The admin portal is a separate shell module. This enforces strict boundary separation from HRMS and Recruitment."* This is a deliberate boundary marker, not an abandoned stub. Building real dashboards would mean inventing layout with no wireframe/Stitch spec, which the master directive explicitly rules out (§4.5: "Do not invent layout"). | **Already in place** — correct as designed. Building real screens is separate future work, blocked on wireframes, not a Phase 1 concern. |
| Angular Material / PrimeNG / parallel SCSS | None found. Single styling system. | **Already in place** |
| `shared/components/ui/drawer/`, `/table/` | Both use `<ng-content>`; drawer uses Signals (`input()`/`output()`); table is a stateless wrapper (no signals needed). Matches the component API standard in §4.2 of the master directive. | **Already in place** |
| Duplication vs new primitives | `applications-management` and `job-requisitions` already consume `<rm-table>`/`<rm-drawer>` — no hand-rolled duplicates left in `features/recruitment/`. | **Already in place** |

## Docs (`docs/`)

| Path | Finding | Bucket |
|---|---|---|
| `docs/ARCHITECTURE.md`, `AI_CONTRACT_SPEC.md`, `DESIGN_SYSTEM_TOKENS.md` | Already exist (untracked), short, accurate at the level of intent. Two drift points: `AI_CONTRACT_SPEC.md`'s `IAIServiceProvider` snippet shows a 4-method interface; the real one has 13 methods. `DESIGN_SYSTEM_TOKENS.md` refers to `--rm-color-primary`; actual tokens are named `--color-primary` (no `rm-` prefix). | **Needs change** — small doc-sync edits, not a rewrite |
| `db/seed/*.sql` (6 numbered files) | Sequential, no duplicates, no stray/orphaned scripts. | **Already in place** |
| `CLAUDE.md` | 3-line addition pointing to `RAINMAKER_MASTER_CONTEXT.md` — already the current state, no action. | **Already in place** |

## Bottom line

- **Needs change (real defects, recommend fixing before Phase 2 work continues):**
  1. Delete or rewrite the 6 broken test files in `Digi.Recruitment.Module.Tests/Multinet/` — the test project does not currently compile.
  2. Delete `Digi.Core.AI/Class1.cs`.
  3. Two one-line doc corrections in `AI_CONTRACT_SPEC.md` and `DESIGN_SYSTEM_TOKENS.md`.
- **Needs change (deferred, correctly out of Phase-1 scope):** the four placeholder AI providers (Phase 2), the two scaffold dashboards (no phase claims them yet).
- **Already in place:** everything else audited above, including both previously-ambiguous items (resolved 2026-08-09) — no further reorg needed anywhere in the tree.

Phase 1 fully closed. Proceeding to Phase 2.

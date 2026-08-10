1. System Philosophy & Vision
Rainmaker ERP is a next-generation, AI-native enterprise resource planning platform engineered from the ground up. It merges the modularity of Odoo, the customization and workflow engine of HubSpot/GoHighLevel, and the data integrity, auditability, and scale of Oracle ERP.

                           +-------------------------------------------------------+
                           |               RAINMAKER UI / UX SYSTEM                |
                           |   (Angular 19 · Atomic Design Tokens · Theme Engine)  |
                           +---------------------------+---------------------------+
                                                       |
                                                       v
                           +-------------------------------------------------------+
                           |              ENTERPRISE BACKEND GATEWAY               |
                           |       (ASP.NET Core 8 · Modular Monolith / Micro)     |
                           +---------------------------+---------------------------+
                                                       |
                             +-------------------------+-------------------------+
                             |                                                   |
                             v                                                   v
         +---------------------------------------+               +-------------------------------+
         |       UNIVERSAL AI PROVIDER BUS       |               |    RELATIONAL & VECTOR DATA    |
         |  (Multinet / OpenAI / Gemini / Custom)|               | (MSSQL SPs + Vector DB / RAG) |
         +---------------------------------------+               +-------------------------------+
Core Architectural Mandates
AI-Native from Ground Zero: AI is not an afterthought or iframe widget. Every module (Recruitment, HRMS, LMS, PMP, CRM) operates with embedded intelligence (parsing, generation, matching, predictive analytics, live voice bots).

Provider-Agnostic Orchestration: The backend acts as an abstraction bus. Switching from Multinet's in-house GPU (qwen3.5:27b) to OpenAI, Anthropic, Google Gemini, or a custom local endpoint requires zero code edits—only configuration changes.

Advisory & Regulatory Governance: All AI actions operating on high-risk domains (hiring, appraisals, compliance) carry review_required: true. AI output is advisory; human confirmation is legally required (EU AI Act Annex III compliance).

Single Source of Truth Design Tokens: The UI/UX relies on central UI primitives and CSS variables. Updating a component, card layout, or theme attribute in the core library propagates synchronously across every portal (Admin, HRMS, Careers, LMS).

2. Global Repository & Workspace Structure
rainmaker-erp/
├── docs/                                    ← Enterprise RFCs, Architecture Specs, Case Studies
│   ├── ARCHITECTURE.md
│   ├── AI_CONTRACT_SPEC.md
│   └── DESIGN_SYSTEM_TOKENS.md
├── Backend/                                 ← ASP.NET Core 8 Solution Root
│   └── RM/
│       ├── Digi.Shared/                     ← Cross-module DTOs, Core Interfaces, Audit Log Attributes
│       ├── Digi.Core.AI/                    ← Universal AI Provider Bus Engine
│       │   ├── Contracts/                   ← Provider-agnostic wire contracts
│       │   ├── Providers/                   ← MultinetAI, OpenAI, Anthropic, Gemini, Custom
│       │   └── Vector/                      ← Vector Store & RAG Abstraction (Qdrant/PgVector)
│       ├── Digi.Recruitment.Module/         ← HRMS Recruitment Business Engine
│       │   ├── Controllers/                 ← REST Endpoints ([Route("recruitment/api/[controller]")])
│       │   ├── Domain/                      ← Domain Models & Services
│       │   └── Repositories/                ← Dapper Data Access & Stored Procedure Executors
│       └── Digi.Recruitment.Module.Tests/   ← Contract, Unit & Integration Test Suites
├── Frontend/                                ← Angular 19 + Tailwind CSS Enterprise SPA
│   └── src/app/
│       ├── core/                            ← Global Infrastructure
│       │   ├── api/                         ← Generic API Clients & Base Interceptors
│       │   ├── state/                       ← Low-latency State Engine (Signals / NgRx)
│       │   ├── theme/                       ← Dynamic Theme Configuration & Token Engine
│       │   └── guards/                      ← Role & Module Auth Guards
│       ├── shared/                          ← Atomic Component Library (Single Source of Truth)
│       │   ├── ui/                          ← Cards, Badges, Buttons, Tables, Drawers, Inputs
│       │   └── icons/                       ← SVG Component Library
│       └── features/                        ← Business Modules
│           ├── admin/                       ← System Admin & Portal Configuration
│           ├── hrms/                        ← HRMS Core Portal
│           └── recruitment/                 ← Recruitment Engine (Jobs, CVs, AI Screening)
└── db/
    ├── migrations/                          ← EF Core / DbUp Schema Versioning
    └── seed/                                ← Stored Procedures, Idempotent Scripts, Seed Data
3. Frontend Architecture & Design System Rules
Atomic UI Components & Theme Synchronicity
Zero Component Duplication: Never create feature-specific duplicates of basic UI primitives (e.g., applicant-card, job-card). Implement a master <rm-card> component inside shared/ui/card/ with customizable content slots.

Token-Driven CSS Engine: Color schemes, typography scale, border radii, and density spacing must derive strictly from global CSS variables defined in core/theme/tokens.css.

CSS
/* Core Design Token Specification */
:root {
  --rm-primary: #1e3a8a;
  --rm-primary-accent: #3b82f6;
  --rm-surface-card: #ffffff;
  --rm-text-main: #0f172a;
  --rm-border-radius-base: 0.5rem;
  --rm-transition-fast: 150ms ease-in-out;
}
Low-Latency (redux) State Engine: Utilize Angular 19 Signals for local/component state and NgRx / RxState for global application state to eliminate unnecessary DOM re-renders.

4. Backend & AI Architecture Guidelines
Universal AI Gateway (Digi.Core.AI)
Every AI call must run through an abstract strategy engine:

C#
public interface IAIServiceProvider 
{
    string ProviderName { get; }
    Task<VerifyKeyResponse> VerifyKeyAsync(CancellationToken ct);
    Task<JobRequisitionResponse> GenerateJobDescriptionAsync(JobRequisitionRequest request, CancellationToken ct);
    Task<ResumeParseResponse> ParseResumeAsync(ResumeParseRequest request, CancellationToken ct);
    Task<ScreeningResponse> ScreenCandidateAsync(ScreeningRequest request, CancellationToken ct);
}
Execution Invariants
Network Fault Tolerance: HttpClients must use Polly policies configured for:

Timeout: minimum 180 seconds on large model inference.

Retries: Exponential backoff on 429 (Too Many Requests) and 5xx (Server Error).

Strict Rule: Never retry 422 (Unprocessable Entity).

Filenames on the Wire: All file uploads submitted over multipart forms must process through ASCII transport encoding (ToTransportFileName) to prevent HTTP header serialization errors on non-Latin characters.

Database Integrity: Primary database operations must run via optimized MSSQL Stored Procedures through Dapper. High-frequency queries must execute in under 50ms.

Shared Services: 
- Email: ALWAYS use `ICentralizedEmailService`. Legacy wrappers like `EmailSender` or `EmailService` are strictly deprecated.
- Files: Use `FileService` for disk I/O operations and `FileStorageService` for external AI upload contracts. Do NOT merge them.

5. Protocol for AI Agents Working on This Codebase
When an AI agent (Claude, Cursor, Windsurf) picks up any task within this project, it must strictly follow this execution workflow:

Phase 1: Deep Research & Case Study Analysis
Before writing code for any feature, the agent must:

Search and evaluate SOTA implementations across industry standards (e.g., Odoo's module pattern, HubSpot's custom properties, Oracle HRMS security structures).

Document architectural trade-offs in code comments or local docs prior to code generation.

Phase 2: Non-Destructive Code Edits
Surgical Precision: Add new features in isolated files or module directories (e.g., Domain/AI/Multinet/).

Legacy Preservation: Never refactor existing, production-tested enterprise code unless explicitly requested.

Convention Matching: Match exact formatting, naming patterns (ApiResponse<T>, PascalCase routes, error logging conventions), and parameter structures.

Phase 3: Self-Verification Checklist
Before marking any task as complete, the agent must run the following checks:

[ ] Code compiles with zero warnings/errors (dotnet build, ng build).

[ ] Null-by-design fields remain null (e.g., age_limits, benefits in AI responses).

[ ] UI components pull directly from shared/ui/ design tokens.

[ ] Sensitive data (keys, connection strings) is kept out of source control.

[ ] Unit and integration tests pass cleanly.

6. Prompt Protocols for Development Agents
Copy and paste the appropriate prompt into your dev agent based on the layer you are building:

Option A: Complete System Master Prompt (For System-Wide Development)
Plaintext
You are acting as the Principal Full-Stack & AI Systems Architect on Rainmaker ERP (ASP.NET Core 8, Angular 19, SQL Server, Multinet AI Engine).

READ AND OBEY `RAINMAKER_MASTER_CONTEXT.md` FIRST.

Your Objective: Build clean, enterprise-grade, fully functional code following our master architecture.

Strict Execution Directives:
1. Conduct research on enterprise case studies (Odoo, HubSpot, Oracle) before submitting designs.
2. Maintain single source of truth design tokens. Never duplicate UI components.
3. AI outputs must carry `review_required: true`.
4. Ensure the backend uses Polly resilience with a 180s timeout, retrying on 429/5xx, but NEVER on 422.
5. All DB modifications must use clean, idempotent SQL scripts and MSSQL Stored Procedures.
6. Verify your implementation step-by-step. Never output placeholders or uncompiled code.
Option B: Frontend Focused Prompt (Angular 19 & Design System)
Plaintext
You are acting as the Lead Frontend Architect on Rainmaker ERP.

Task: Build scalable, reactive Angular 19 components using Tailwind CSS and our central Design Token System.

Rules:
1. Every component must pull from `shared/ui/` primitives (Cards, Badges, Buttons, Data Tables).
2. Implement low-latency state using Angular Signals.
3. Support dynamic runtime theme changes via CSS Variables.
4. Provide comprehensive error handling, loading states, and responsive views.
Option C: Backend & AI Engine Prompt (.NET 8 & Microservices)
Plaintext
You are acting as the Senior Principal Backend & AI Engineer on Rainmaker ERP.

Task: Implement backend API features in ASP.NET Core 8 and wire them to the Universal AI Gateway.

Rules:
1. Follow Clean Architecture. Keep AI provider implementations isolated in provider classes implementing `IAIServiceProvider`.
2. Configure HTTP resilience via Polly: 180s timeout, exponential backoff for 429/5xx, no retries on 422.
3. All write operations must execute via Dapper and MSSQL Stored Procedures under `[ruc].*`.
4. Ensure non-ASCII filenames are converted via `ToTransportFileName` prior to multipart transmission.
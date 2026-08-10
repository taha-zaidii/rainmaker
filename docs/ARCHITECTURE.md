# Architecture Spec: Rainmaker ERP

## 1. High-Level Backend Strategy (`Backend/RM/`)
The Rainmaker ASP.NET Core 8 backend enforces strict modularity and boundary contexts. All cross-cutting concerns (DTOs, attributes) live in `Digi.Shared`. AI capabilities are completely decoupled inside `Digi.Core.AI`. Business features remain firmly situated in `Digi.Recruitment.Module`.

### **Module Breakdown**
- **`Digi.Shared`**: Generic models, API response wrappers (`ApiResponse<T>`), and authentication tokens.
- **`Digi.Core.AI`**: The unified, provider-agnostic engine. It contains `IAIServiceProvider`, HTTP Polly Resilience extensions, and data transport normalizations (e.g. `ToTransportFileName`).
- **`Digi.Recruitment.Module`**: Domain services (`RecruitmentAIService.cs`), controllers routing `recruitment/api/[controller]`, and `Dapper` Repositories mapping SQL to C# Models.

## 2. Database Integrity (`db/seed/`)
All database write logic is rigorously guarded through:
- **`[ruc]` Schema**: The single schema encompassing recruitment features.
- **Stored Procedures Only**: Inline `INSERT` or `UPDATE` queries are strictly forbidden. Repositories must utilize Dapper `CommandType.StoredProcedure` pointing to `ruc.SP_*` implementations.
- **Idempotency & Execution Speed**: SPs must handle `NOCOUNT ON` efficiently, striving for <50ms query times on standard reads/writes.
- **Draft by Default**: AI operations creating domain models (like Requisitions) explicitly default to a `Draft` status (e.g. `IsPublished = 0`) unless a human deliberately overrides and publishes them.

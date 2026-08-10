# AI Contract Spec

## 1. Universal AI Provider Gateway (`Digi.Core.AI`)
The Rainmaker ERP treats all AI execution engines—whether the local Multinet GPU (`qwen3.5:27b`), OpenAI, Anthropic, or Gemini—as abstract third-party entities. No backend logic is tightly coupled to a single AI vendor.

### **`IAIServiceProvider` Strategy**
All AI actions run through implementations of the interface in
`Digi.Core.AI/Providers/IAIServiceProvider.cs` — 13 methods covering key
verification, JD generation, resume extraction (by bytes and by URL),
screening, interview questions, candidate listing, ranking, and scoring. See
that file for the authoritative signatures. All 6 providers are real, working
implementations: `MultinetAiProvider`/`StubMultinetAiProvider` (purpose-built
pipeline), and `OpenAiProvider`/`AnthropicProvider`/`GoogleGeminiProvider`/
`CustomAiProvider` (general-purpose chat models prompted for the identical
JSON contract shapes — shared plumbing in `Digi.Core.AI/Providers/Generic/`).
`RankAsync`/`ScoreAsync`/`ListCandidatesAsync` are the one deliberate
exception: the 4 generic providers return `AiErrorCode.NotSupportedByProvider`
for these — there is no generic equivalent to Multinet's embeddings corpus or
rubric engine, and approximating one would look real while resting on nothing.
`RecruitmentAIService.cs`'s 5 AI-facing entry points (JD generation, resume
parsing, screening, interview questions, key testing) route every company's
configured provider through this gateway — including `custom`, which had no
backend at all before this.

## 2. Execution Invariants & Resilience
- **Polly Resilience**: All outgoing `HttpClient` calls to an AI provider must utilize a robust Polly policy.
- **Timeout**: The HTTP request timeout is strictly set to **180 seconds** to accommodate large generative inferences (e.g. 90-second resume parsing).
- **Retries**: Configured for exponential backoff on `429 (Too Many Requests)` or `5xx (Server Error)`.
- **Fatal Status Codes**: The system must NEVER retry on `422 Unprocessable Entity` (indicating bad prompt schema).
- **Transport Security**: Binary file uploads (e.g., PDF resumes) must always use `ToTransportFileName` encoding in `MultipartFormDataContent` to ensure non-Latin names (Urdu, Arabic) do not break the network boundary.

## 3. Human-in-the-Loop Advisory Compliance
- The system operates under the principle that AI output is exclusively **Advisory**.
- In high-risk workflows (hiring, candidate screening), every AI-generated response carries a `review_required: true` flag and is automatically staged as a `"Draft"`. Human confirmation is an enforced legal safeguard.

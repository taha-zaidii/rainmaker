using System.Net;
using System.Text;
using Digi.Core.AI.Configuration;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers;
using Digi.Recruitment.Module.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Digi.Recruitment.Module.Tests.Multinet
{
    /// <summary>
    /// The AI service is fail-closed, single-flight, and answers 40–90 seconds
    /// later. That makes its error surface the part most likely to be wrong in
    /// production and the least likely to be exercised by hand, so it is pinned
    /// here: every documented status maps to one domain code, with the correct
    /// retryability, and a success that violates the contract is refused.
    /// </summary>
    public class MultinetAiClientTests
    {
        private const string ValidPdfHeader = "%PDF-1.7\n";

        /// <summary>
        /// The on-box form of the base URL, which — like the production one —
        /// already carries the API version. The client appends bare feature
        /// paths to it; anything that re-adds "api/v1" would double-prefix and
        /// 404, which is what the path assertions below exist to catch.
        /// </summary>
        private const string OnBoxBase = "http://127.0.0.1:8020/api/v1";

        private static MultinetAiOptions Options(Action<MultinetAiOptions>? tweak = null)
        {
            var options = new MultinetAiOptions
            {
                BaseUrl = OnBoxBase,
                ApiKey = "test-key-not-a-real-secret",
                TimeoutSeconds = 180,
                MaxRetries = 2,
                MaxUploadMegabytes = 20,
                Enabled = true
            };
            tweak?.Invoke(options);
            return options;
        }

        private static (MultinetAiProvider Client, StubHttpMessageHandler Handler) Build(
            Action<MultinetAiOptions>? tweak = null)
        {
            var handler = new StubHttpMessageHandler();
            var http = new HttpClient(handler)
            {
                BaseAddress = new Uri(OnBoxBase + "/")
            };

            var client = new MultinetAiProvider(
                http,
                new FakeOptionsSnapshot<MultinetAiOptions>(Options(tweak)),
                NullLogger<MultinetAiProvider>.Instance);

            return (client, handler);
        }

        private static MemoryStream Pdf(int paddingBytes = 512)
        {
            var stream = new MemoryStream();
            stream.Write(Encoding.ASCII.GetBytes(ValidPdfHeader));
            stream.Write(new byte[paddingBytes]);
            stream.Position = 0;
            return stream;
        }

        private static string SuccessBody(string schemaVersion = "1.2.0") => $$"""
        {
          "status": "success",
          "data": {
            "name": "Test Candidate",
            "email": "test@example.test",
            "phone": "+92 300 0000000",
            "location": null,
            "headline": null,
            "summary": null,
            "spoken_languages": ["English"],
            "links": [],
            "skills": ["C#", "Angular"],
            "education": [{"institution":"Test University","degree":"BE","duration":null,"gpa":null}],
            "experience": [{"company":"Test Co","role":"Engineer","duration":"2022 - Present","location":null,"achievements":["Did a thing"]}],
            "projects": [{"name":"Thing","technologies":"C#, SQL","description":["Built it"]}],
            "certifications_and_awards": []
          },
          "meta": {
            "schema_version": "{{schemaVersion}}",
            "extraction_route": "text",
            "field_provenance": {"name":"llm","phone":"regex","skills":"vision_escalation"},
            "total_wall_ms": 41230.5,
            "validation_passed": true
          }
        }
        """;

        // ── Error mapping ────────────────────────────────────────────────────

        [Fact]
        public async Task Unauthorized_maps_to_Unauthorized_and_is_never_retried()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.Unauthorized, """{"detail":{"error":"unauthorized"}}""");

            var result = await client.ListCandidatesAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.Unauthorized, result.Error!.Code);
            Assert.False(result.Error.Retryable);
            // A revoked key will not fix itself; hammering a fail-closed service is pointless.
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task Payload_too_large_maps_to_FileTooLarge()
        {
            var (client, handler) = Build();
            handler.Respond(
                HttpStatusCode.RequestEntityTooLarge,
                """{"detail":{"error":"file_too_large","message":"Upload exceeds the 20 MB limit."}}""");

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.FileTooLarge, result.Error!.Code);
            Assert.False(result.Error.Retryable);
            Assert.Equal("file_too_large", result.Error.ServiceErrorCode);
        }

        [Theory]
        [InlineData("unsupported_file_type", AiErrorCode.UnsupportedFileType)]
        [InlineData("content_type_mismatch", AiErrorCode.ContentTypeMismatch)]
        [InlineData("extraction_failed", AiErrorCode.ExtractionFailed)]
        [InlineData("file_processing_error", AiErrorCode.FileProcessingError)]
        public async Task Each_422_slug_maps_to_its_own_domain_code_and_is_never_retryable(
            string serviceCode, AiErrorCode expected)
        {
            var (client, handler) = Build();
            var body = """{"detail":{"error":"__CODE__","message":"safe text"}}"""
                .Replace("__CODE__", serviceCode);
            handler.Respond(HttpStatusCode.UnprocessableEntity, body);

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            Assert.True(result.IsFailure);
            Assert.Equal(expected, result.Error!.Code);
            // The contract is explicit: a 422 is a verdict about the document.
            // Retrying burns 40–90 s of single-flight GPU time for the same answer.
            Assert.False(result.Error.Retryable);
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task Server_error_is_marked_retryable()
        {
            var (client, handler) = Build();
            handler.Respond(
                HttpStatusCode.InternalServerError,
                """{"detail":{"error":"internal_error","message":"An unexpected error occurred."}}""");

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.InternalError, result.Error!.Code);
            Assert.True(result.Error.Retryable);
        }

        [Fact]
        public async Task Connection_failure_maps_to_Unreachable_and_is_retryable()
        {
            var (client, handler) = Build();
            handler.Throw(new HttpRequestException("Connection refused"));

            var result = await client.GetHealthAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.Unreachable, result.Error!.Code);
            Assert.True(result.Error.Retryable);
        }

        [Fact]
        public async Task Client_timeout_maps_to_Timeout_and_is_retryable()
        {
            var (client, handler) = Build();
            // How HttpClient surfaces its own timeout on .NET 5+.
            handler.Throw(new TaskCanceledException("timed out", new TimeoutException()));

            var result = await client.GetHealthAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.Timeout, result.Error!.Code);
            Assert.True(result.Error.Retryable);
        }

        [Fact]
        public async Task Caller_cancellation_propagates_instead_of_becoming_a_service_error()
        {
            var (client, handler) = Build();
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            handler.Throw(new OperationCanceledException(cts.Token));

            // Shutdown or a disconnected browser is not the AI service failing;
            // reporting it as one would park healthy jobs as broken.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => client.GetHealthAsync(cts.Token));
        }

        [Fact]
        public async Task Unknown_profile_id_on_scoring_maps_to_its_own_code()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.NotFound, """{"detail":{"error":"unknown_profile_id"}}""");

            var result = await client.ScoreAsync("nope", new string('j', 40));

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.UnknownProfileId, result.Error!.Code);
        }

        [Fact]
        public void Bare_string_detail_is_tolerated()
        {
            // POST /parser/extract with no filename returns detail as a STRING,
            // not an object. Reading it must not throw and must not lose the text.
            var (code, message) = MultinetAiProvider.ReadErrorDetail("""{"detail":"No filename provided."}""");

            Assert.Null(code);
            Assert.Equal("No filename provided.", message);
        }

        [Fact]
        public void Unparseable_error_body_does_not_mask_the_status_code()
        {
            var error = MultinetAiProvider.MapError(HttpStatusCode.Unauthorized, "<html>gateway error</html>");

            Assert.Equal(AiErrorCode.Unauthorized, error.Code);
            Assert.Equal(401, error.HttpStatus);
        }

        // ── Success path and contract enforcement ────────────────────────────

        [Fact]
        public async Task Successful_parse_returns_profile_and_preserves_provenance()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, SuccessBody());

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            Assert.True(result.IsSuccess);
            var parsed = result.ValueOrThrow;
            Assert.Equal("Test Candidate", parsed.Data!.Name);
            Assert.Null(parsed.Data.Location);                       // sparse resumes are valid
            Assert.Equal("C#, SQL", parsed.Data.Projects[0].Technologies);   // comma-joined, per contract

            // Provenance must survive untouched — it is the review UI's only
            // signal for which fields a human has to check.
            Assert.Equal("regex", parsed.Meta!.FieldProvenance["phone"]);
            Assert.Equal("vision_escalation", parsed.Meta.FieldProvenance["skills"]);
        }

        [Fact]
        public async Task Fields_needing_review_are_derived_from_provenance()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, SuccessBody());

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");
            var flagged = result.ValueOrThrow.Meta!.FieldsNeedingReview();

            Assert.Contains("phone", flagged);            // regex, not the model
            Assert.Contains("skills", flagged);           // vision escalation
            Assert.DoesNotContain("name", flagged);       // model-produced and verified
        }

        [Fact]
        public async Task Incompatible_schema_version_is_refused_rather_than_stored()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, SuccessBody(schemaVersion: "2.0.0"));

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            // Writing a candidate record from a schema we do not understand is
            // worse than failing: the data would be silently wrong.
            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.ContractViolation, result.Error!.Code);
        }

        [Fact]
        public async Task Patch_level_schema_bump_is_accepted()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, SuccessBody(schemaVersion: "1.2.7"));

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task Success_envelope_without_data_is_refused()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """{"status":"success","meta":{"schema_version":"1.2.0"}}""");

            var result = await client.ExtractResumeAsync(Pdf(), "resume.pdf");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.ContractViolation, result.Error!.Code);
        }

        // ── Request shape ────────────────────────────────────────────────────

        [Fact]
        public async Task Api_key_is_sent_as_the_X_Api_Key_header()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """{"count":0,"candidates":[]}""");

            await client.ListCandidatesAsync();

            Assert.True(handler.Received[0].Headers.TryGetValues("X-API-Key", out var values));
            Assert.Equal("test-key-not-a-real-secret", values!.Single());
        }

        [Fact]
        public async Task Per_call_key_overrides_the_platform_key()
        {
            // How a company's own metered key is used instead of the platform one.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """{"count":0,"candidates":[]}""");

            await client.ListCandidatesAsync(apiKey: "company-specific-key");

            handler.Received[0].Headers.TryGetValues("X-API-Key", out var values);
            Assert.Equal("company-specific-key", values!.Single());
        }

        [Fact]
        public async Task Upload_uses_the_frozen_field_name_and_multipart_encoding()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, SuccessBody());

            await client.ExtractResumeAsync(Pdf(), "my resume.pdf");

            var request = handler.Received[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/parser/extract", request.RequestUri!.AbsolutePath);
            Assert.StartsWith("multipart/form-data", request.Content!.Headers.ContentType!.MediaType!);
            // Field name "file" is frozen by the contract.
            Assert.Contains("name=file", handler.ReceivedBodies[0].Replace("\"", string.Empty));
        }

        [Fact]
        public async Task Readiness_503_is_NotReady_and_retryable_not_a_hard_failure()
        {
            var (client, handler) = Build();
            handler.Respond(
                HttpStatusCode.ServiceUnavailable,
                """{"status":"not_ready","llm_backend":{"reachable":false}}""");

            var result = await client.GetReadinessAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.NotReady, result.Error!.Code);
            // Jobs park and resume; they must not be marked failed.
            Assert.True(result.Error.Retryable);
        }

        [Fact]
        public async Task Readiness_200_reports_the_model_and_backend()
        {
            var (client, handler) = Build();
            handler.Respond(
                HttpStatusCode.OK,
                """{"status":"ready","llm_backend":{"reachable":true,"version":"0.24.0"},"model":"qwen3.5:27b","backend":"ollama"}""");

            var result = await client.GetReadinessAsync();

            Assert.True(result.IsSuccess);
            Assert.True(result.ValueOrThrow.IsReady);
            Assert.Equal("qwen3.5:27b", result.ValueOrThrow.Model);
        }

        // ── Local rejection: never spend a GPU slot on a predictable no ───────

        [Fact]
        public async Task Oversized_file_is_rejected_without_a_network_call()
        {
            var (client, handler) = Build(o => o.MaxUploadMegabytes = 1);
            using var big = Pdf(paddingBytes: 2 * 1024 * 1024);

            var result = await client.ExtractResumeAsync(big, "big.pdf");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.FileTooLarge, result.Error!.Code);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task Disallowed_extension_is_rejected_without_a_network_call()
        {
            var (client, handler) = Build();
            using var stream = Pdf();

            var result = await client.ExtractResumeAsync(stream, "resume.exe");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.UnsupportedFileType, result.Error!.Code);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task File_renamed_to_pdf_is_rejected_on_magic_bytes_without_a_network_call()
        {
            var (client, handler) = Build();
            using var notAPdf = new MemoryStream(Encoding.ASCII.GetBytes("MZ\0 this is an executable"));

            var result = await client.ExtractResumeAsync(notAPdf, "payload.pdf");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.ContentTypeMismatch, result.Error!.Code);
            Assert.Equal(0, handler.CallCount);
        }

        [Fact]
        public async Task Short_job_description_is_rejected_before_dispatch()
        {
            var (client, handler) = Build();

            var result = await client.RankAsync("too short", topK: 10);

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.BadRequest, result.Error!.Code);
            Assert.Equal(0, handler.CallCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(51)]
        public async Task TopK_outside_the_contract_range_is_rejected_before_dispatch(int topK)
        {
            var (client, handler) = Build();

            var result = await client.RankAsync(new string('j', 40), topK);

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.BadRequest, result.Error!.Code);
            Assert.Equal(0, handler.CallCount);
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Digi.Recruitment.Module.Domain.AI.Multinet;
using Digi.Recruitment.Module.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digi.Recruitment.Module.Tests.Multinet
{
    /// <summary>
    /// Pins the PRODUCTION EDGE contract, as distinct from the on-box one the
    /// client was first written against.
    ///
    /// The difference is not cosmetic. In production the base URL already ends
    /// in <c>/api/v1</c>, so a client that also prefixes <c>api/v1/</c> composes
    /// <c>/hrms/api/v1/api/v1/parser/extract</c> and 404s on every call. The ops
    /// endpoints are blocked at nginx, so a readiness gate built on <c>/ready</c>
    /// can never open. And a 429 puts its retry hint at the ROOT of the body
    /// rather than under <c>detail</c>, so reading only <c>detail</c> throws away
    /// the one number that matters when queuing behind a serial GPU.
    ///
    /// Each of those is a silent, total failure in production and invisible in
    /// development. Hence these tests.
    /// </summary>
    public class MultinetAiEdgeContractTests
    {
        private const string ProductionBase = "https://ai.rainmaker.pk/hrms/api/v1";

        private static (MultinetAiClient Client, StubHttpMessageHandler Handler) Build()
        {
            var handler = new StubHttpMessageHandler();
            var http = new HttpClient(handler) { BaseAddress = new Uri(ProductionBase + "/") };

            var options = new MultinetAiOptions
            {
                BaseUrl = ProductionBase,
                ApiKey = "test-key-not-a-real-secret",
                TimeoutSeconds = 180,
                Enabled = true
            };

            return (
                new MultinetAiClient(http, new OptionsWrapper<MultinetAiOptions>(options),
                    NullLogger<MultinetAiClient>.Instance),
                handler);
        }

        // ── Base URL resolution ──────────────────────────────────────────────

        [Theory]
        [InlineData("https://ai.rainmaker.pk/hrms/api/v1")]
        [InlineData("https://ai.rainmaker.pk/hrms/api/v1/")]
        [InlineData("  https://ai.rainmaker.pk/hrms/api/v1  ")]
        public void Documented_base_url_resolves_cleanly_in_every_form(string configured)
        {
            var resolution = MultinetAiEndpoints.ResolveBaseUrl(configured);

            Assert.True(resolution.IsUsable);
            Assert.Null(resolution.Problem);
            Assert.Null(resolution.Warning);
            Assert.False(resolution.WasCorrected);

            // The trailing slash is what makes relative composition keep the path.
            Assert.Equal("https://ai.rainmaker.pk/hrms/api/v1/", resolution.BaseUri!.ToString());
        }

        [Fact]
        public void Base_url_composes_with_a_feature_path_without_losing_its_prefix()
        {
            var resolution = MultinetAiEndpoints.ResolveBaseUrl(ProductionBase);
            var composed = MultinetAiEndpoints.Combine(resolution.BaseUri!, MultinetAiEndpoints.GenerateJobRequisition);

            Assert.Equal(
                "https://ai.rainmaker.pk/hrms/api/v1/recruitment/jobreq/generate",
                composed.ToString());
        }

        [Fact]
        public void The_known_bad_api_query_endpoint_is_corrected_rather_than_left_to_404()
        {
            // The settings page's own helper text recommended this for months and
            // it is still stored for at least one live tenant. It returns 404.
            var resolution = MultinetAiEndpoints.ResolveBaseUrl("https://ai.rainmaker.pk/hrms/api/query");

            Assert.True(resolution.IsUsable);
            Assert.True(resolution.WasCorrected);
            Assert.Equal("https://ai.rainmaker.pk/hrms/api/v1/", resolution.BaseUri!.ToString());

            // Corrected, but never silently: the tenant must fix it at source.
            Assert.NotNull(resolution.Warning);
            Assert.Contains("404", resolution.Warning);
        }

        [Fact]
        public void A_base_url_without_a_version_segment_is_usable_but_flagged()
        {
            // The service could legitimately move, so this is a warning and not a
            // refusal — but it is overwhelmingly the reason for a blanket 404.
            var resolution = MultinetAiEndpoints.ResolveBaseUrl("https://ai.rainmaker.pk/hrms/api");

            Assert.True(resolution.IsUsable);
            Assert.NotNull(resolution.Warning);
            Assert.Contains("v1", resolution.Warning);
        }

        [Fact]
        public void A_query_string_on_the_base_url_is_stripped_before_it_can_corrupt_every_path()
        {
            var resolution = MultinetAiEndpoints.ResolveBaseUrl(ProductionBase + "?key=leaked");

            Assert.True(resolution.IsUsable);
            Assert.True(resolution.WasCorrected);
            Assert.Equal("https://ai.rainmaker.pk/hrms/api/v1/", resolution.BaseUri!.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("ai.rainmaker.pk/hrms/api/v1")]   // no scheme
        [InlineData("ftp://ai.rainmaker.pk/hrms")]    // wrong scheme
        public void An_unusable_endpoint_is_refused_with_an_actionable_message(string? configured)
        {
            var resolution = MultinetAiEndpoints.ResolveBaseUrl(configured);

            Assert.False(resolution.IsUsable);
            Assert.NotNull(resolution.Problem);
            Assert.Contains("ai.rainmaker.pk", resolution.Problem);
        }

        // ── Provider recognition ─────────────────────────────────────────────

        [Theory]
        [InlineData("multinetai")]
        [InlineData("MultinetAI")]
        [InlineData("  multinetai  ")]
        public void The_dedicated_provider_is_recognised_by_name(string provider)
        {
            Assert.True(MultinetAiProvider.Matches(provider));
        }

        [Theory]
        [InlineData("custom")]
        [InlineData("openai")]
        [InlineData("anthropic")]
        [InlineData("google")]
        [InlineData("")]
        [InlineData(null)]
        public void No_other_provider_is_ever_claimed_as_ours(string? provider)
        {
            Assert.False(MultinetAiProvider.Matches(provider));
        }

        [Fact]
        public void Custom_pointing_at_our_own_host_is_still_NOT_treated_as_ours()
        {
            // "custom" is the client's escape hatch for a third-party service they
            // bring themselves — Groq, DeepSeek, a self-hosted gateway. Deciding
            // "this URL looks like ours, so I will handle it" would silently
            // hijack their configuration, and nothing in the settings UI would
            // show it happening. The dropdown selection is the only signal.
            Assert.False(MultinetAiProvider.Matches("custom"));
        }

        // ── Feature paths ────────────────────────────────────────────────────

        [Fact]
        public async Task Key_verification_hits_auth_verify_under_the_versioned_base()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """
            {
              "valid": true,
              "service": "hrms-ai-service",
              "service_version": "1.1.0",
              "schema_version": "1.2.0",
              "capabilities": ["parser.extract","recruitment.jobreq.generate","matching.rank"]
            }
            """);

            var result = await client.VerifyKeyAsync();

            Assert.True(result.IsSuccess);
            Assert.Equal(
                "https://ai.rainmaker.pk/hrms/api/v1/auth/verify",
                handler.Received[0].RequestUri!.ToString());

            var verification = result.Value!;
            Assert.True(verification.Valid);
            Assert.Equal("1.1.0", verification.ServiceVersion);
            Assert.True(verification.Supports(MultinetAiEndpoints.Capabilities.JobRequisitionGenerate));
            Assert.False(verification.Supports(MultinetAiEndpoints.Capabilities.ScoringScore));
        }

        [Fact]
        public async Task Feature_paths_are_not_double_prefixed_with_api_v1()
        {
            // The regression this whole file exists for.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """{"model_version":"v1","section_weights":{},"ranking":[]}""");

            await client.RankAsync(new string('j', 40), topK: 5);

            var path = handler.Received[0].RequestUri!.AbsolutePath;
            Assert.Equal("/hrms/api/v1/matching/rank", path);
            Assert.DoesNotContain("v1/api/v1", path);
        }

        [Fact]
        public async Task An_unknown_capability_in_the_response_does_not_break_deserialization()
        {
            // meta and capabilities are additive; the service gains features
            // without asking the portal to redeploy.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """
            {
              "valid": true,
              "service": "hrms-ai-service",
              "capabilities": ["parser.extract","something.invented.later"],
              "a_field_added_next_quarter": {"nested": true}
            }
            """);

            var result = await client.VerifyKeyAsync();

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.Supports("something.invented.later"));
        }

        // ── Multi-tenancy ────────────────────────────────────────────────────

        [Fact]
        public async Task A_per_company_endpoint_overrides_the_configured_base_url()
        {
            // Each company stores its own endpoint, so a fixed BaseAddress cannot
            // serve the portal on its own.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """{"valid":true,"capabilities":[]}""");

            var tenantBase = MultinetAiEndpoints.ResolveBaseUrl("https://tenant-ai.example.pk/hrms/api/v1");
            await client.VerifyKeyAsync(apiKey: "tenant-key", baseUriOverride: tenantBase.BaseUri);

            Assert.Equal(
                "https://tenant-ai.example.pk/hrms/api/v1/auth/verify",
                handler.Received[0].RequestUri!.ToString());
            Assert.Equal("tenant-key", handler.Received[0].Headers.GetValues("X-API-Key").Single());
        }

        // ── Error bodies at the root ─────────────────────────────────────────

        [Fact]
        public void A_429_body_yields_Busy_and_the_wait_the_service_asked_for()
        {
            var error = MultinetAiClient.MapError(
                HttpStatusCode.TooManyRequests, """{"error":"busy","retry_after_s":12}""");

            Assert.Equal(AiErrorCode.Busy, error.Code);
            Assert.True(error.Retryable);
            Assert.Equal("busy", error.ServiceErrorCode);
            Assert.Equal(TimeSpan.FromSeconds(12), error.RetryAfter);

            // Worth telling the recruiter: it is a queue, not an outage.
            Assert.Contains("12", error.Message);
        }

        [Fact]
        public async Task The_Retry_After_header_wins_over_the_body_hint()
        {
            var (client, handler) = Build();
            handler.RespondWith(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent(
                        """{"error":"busy","retry_after_s":12}""", Encoding.UTF8, "application/json")
                };
                response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
                return response;
            });

            var result = await client.VerifyKeyAsync();

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.Busy, result.Error!.Code);
            Assert.Equal(TimeSpan.FromSeconds(30), result.Error.RetryAfter);
        }

        [Fact]
        public void A_503_names_the_model_backend_rather_than_blaming_the_service_generally()
        {
            var error = MultinetAiClient.MapError(
                HttpStatusCode.ServiceUnavailable, """{"error":"llm_unreachable"}""");

            Assert.Equal(AiErrorCode.NotReady, error.Code);
            Assert.True(error.Retryable);
            Assert.Equal("llm_unreachable", error.ServiceErrorCode);
            Assert.Contains("model backend", error.Message);
        }

        [Fact]
        public void Root_level_and_nested_error_shapes_are_both_understood()
        {
            var nested = MultinetAiClient.ReadErrorPayload("""{"detail":{"error":"unauthorized"}}""");
            Assert.Equal("unauthorized", nested.Code);

            var root = MultinetAiClient.ReadErrorPayload("""{"error":"internal_error"}""");
            Assert.Equal("internal_error", root.Code);

            // The 400 path still returns detail as a bare string.
            var bare = MultinetAiClient.ReadErrorPayload("""{"detail":"No filename provided."}""");
            Assert.Null(bare.Code);
            Assert.Equal("No filename provided.", bare.Message);

            // An nginx HTML page must not mask the status code.
            var junk = MultinetAiClient.ReadErrorPayload("<html>502 Bad Gateway</html>");
            Assert.Null(junk.Code);
            Assert.Null(junk.RetryAfterSeconds);
        }

        [Fact]
        public void A_404_is_a_configuration_problem_and_is_never_retried()
        {
            // What a wrong base URL actually looks like from here.
            var error = MultinetAiClient.MapError(HttpStatusCode.NotFound, "");

            Assert.Equal(AiErrorCode.BadRequest, error.Code);
            Assert.False(error.Retryable);
        }
    }
}

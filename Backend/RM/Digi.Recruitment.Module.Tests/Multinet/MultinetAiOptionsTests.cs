using Digi.Recruitment.Module.Domain.AI.Multinet;
using Xunit;

namespace Digi.Recruitment.Module.Tests.Multinet
{
    /// <summary>
    /// Configuration mistakes here are silent and expensive: an empty key means
    /// every upload 401s after a recruiter has already waited, and a short timeout
    /// aborts legitimate 40–90 s parses. Both must fail at start-up instead.
    /// </summary>
    public class MultinetAiOptionsTests
    {
        private static MultinetAiOptions Valid() => new()
        {
            Enabled = true,
            BaseUrl = "http://127.0.0.1:8000",
            ApiKey = "not-a-real-key",
            TimeoutSeconds = 240
        };

        [Fact]
        public void A_correctly_configured_provider_validates()
        {
            Assert.Empty(Valid().Validate());
        }

        [Fact]
        public void Missing_platform_key_is_allowed_because_tenants_bring_their_own()
        {
            // The portal is multi-tenant. The key that actually authenticates a
            // call is the per-company one in Tbl_Ruc_RecruitmentAI_Settings,
            // entered on the AI Settings screen and stored encrypted; the
            // configured value is only a fallback for callers with no company
            // context.
            //
            // Failing startup on an empty fallback would reject the normal,
            // correct deployment — every tenant holding its own key and the
            // platform holding none.
            var options = Valid();
            options.ApiKey = "";

            Assert.Empty(options.Validate());
        }

        [Fact]
        public void Stub_mode_needs_no_api_key_because_no_call_leaves_the_process()
        {
            var options = Valid();
            options.ApiKey = "";
            options.StubMode = true;

            Assert.Empty(options.Validate());
        }

        [Fact]
        public void A_disabled_provider_is_never_validated()
        {
            // Off is off: an unconfigured provider must not block start-up.
            var options = new MultinetAiOptions { Enabled = false, BaseUrl = "nonsense", ApiKey = "" };

            Assert.Empty(options.Validate());
        }

        [Theory]
        [InlineData("")]
        [InlineData("localhost:8000")]      // no scheme
        [InlineData("ftp://host/")]         // wrong scheme
        [InlineData("not a url")]
        public void A_base_url_that_is_not_an_absolute_http_url_is_rejected(string baseUrl)
        {
            var options = Valid();
            options.BaseUrl = baseUrl;

            Assert.Contains(options.Validate(), problem => problem.Contains("BaseUrl"));
        }

        [Fact]
        public void A_timeout_below_the_contract_floor_is_rejected()
        {
            var options = Valid();
            options.TimeoutSeconds = 30;   // shorter than a normal parse

            Assert.Contains(options.Validate(), problem => problem.Contains("TimeoutSeconds"));
        }

        [Fact]
        public void Upload_ceiling_is_expressed_in_bytes_for_stream_checks()
        {
            var options = Valid();
            options.MaxUploadMegabytes = 20;

            Assert.Equal(20L * 1024 * 1024, options.MaxUploadBytes);
        }
    }

    /// <summary>
    /// Provenance drives the review UI. If a field the model never verified stops
    /// being flagged, a recruiter accepts a regex guess as if a human checked it.
    /// </summary>
    public class FieldProvenanceTests
    {
        [Theory]
        [InlineData("llm")]
        [InlineData("LLM")]              // case must not matter
        [InlineData("llm_verified")]
        [InlineData("text")]
        [InlineData("docling")]
        public void Model_verified_provenance_does_not_need_review(string provenance)
        {
            Assert.False(FieldProvenance.NeedsReview(provenance));
        }

        [Theory]
        [InlineData("regex")]
        [InlineData("vision_escalation")]
        [InlineData("llm_unverified")]
        [InlineData("heuristic")]
        [InlineData("something_new_the_pipeline_invents_later")]
        public void Anything_else_needs_review(string provenance)
        {
            // Unknown provenance defaults to "flag it". A new extraction route
            // added upstream must never silently pass as trusted.
            Assert.True(FieldProvenance.NeedsReview(provenance));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Absent_provenance_needs_review(string? provenance)
        {
            Assert.True(FieldProvenance.NeedsReview(provenance));
        }

        [Theory]
        [InlineData("1.2.0", true)]
        [InlineData("1.2.9", true)]      // patch bump cannot change shape
        [InlineData("1.3.0", false)]     // new fields
        [InlineData("2.0.0", false)]
        [InlineData("1.2", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Schema_compatibility_is_major_minor(string? version, bool expected)
        {
            Assert.Equal(expected, ProfileSchemaVersions.IsCompatible(version));
        }
    }
}

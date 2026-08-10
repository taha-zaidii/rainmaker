using System.Net;
using System.Text.Json;
using Digi.Core.AI.Configuration;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers;
using Digi.Recruitment.Module.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Digi.Recruitment.Module.Tests.Multinet
{
    /// <summary>
    /// Job description generation — the flagship feature.
    ///
    /// The binding rules tested here are not stylistic preferences. Several
    /// fields come back null BY DESIGN because they belong to a human, and one of
    /// them — age limits — is null because an AI proposing an age band in a job
    /// advert is discriminatory and indefensible under the EU AI Act's high-risk
    /// hiring rules. A well-meaning "sensible default" anywhere in this mapping
    /// would turn a compliant integration into a liability, silently.
    ///
    /// The response fixture is the contract's own worked example, used verbatim.
    /// </summary>
    public class MultinetJobRequisitionTests
    {
        private const string ProductionBase = "https://ai.rainmaker.pk/hrms/api/v1";

        private static (MultinetAiProvider Client, StubHttpMessageHandler Handler) Build()
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
                new MultinetAiProvider(http, new FakeOptionsSnapshot<MultinetAiOptions>(options),
                    NullLogger<MultinetAiProvider>.Instance),
                handler);
        }

        /// <summary>The contract's worked example, verbatim.</summary>
        private const string ContractResponse = """
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
              "key_responsibilities": ["Build services", "Review changes"],
              "requirements": ["Proven delivery experience"],
              "qualifications": ["Bachelor's degree"],
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
          "meta": {
            "service_version": "1.1.0",
            "cache_hit": false,
            "experience_source": "parsed_from_request",
            "job_category_source": "selected_from_options",
            "work_mode": "Hybrid",
            "repairs": {}
          }
        }
        """;

        private static JobRequisitionRequest SampleRequest() => new()
        {
            CompanyId = 133,
            JobTitle = "Software Developer",
            Department = "Information Technology",
            Designation = "System Administrator",
            ExperienceRequired = "3 - 6 years",
            KeySkills = "JavaScript, Python and .NET or maybe Angular+C# etc",
            JobCategoryOptions = new List<string> { "UI/UX", "Dot Net Developer", "Python Developer" }
        };

        // ── The contract's worked example ────────────────────────────────────

        [Fact]
        public async Task The_contract_example_binds_exactly_as_documented()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse);

            var result = await client.GenerateJobRequisitionAsync(SampleRequest());

            Assert.True(result.IsSuccess);
            var generated = result.Value!;

            Assert.Equal("/hrms/api/v1/recruitment/jobreq/generate", handler.Received[0].RequestUri!.AbsolutePath);

            // Rule 1: execution_time_ms is an integer and binds to a long.
            Assert.Equal(20909L, generated.ExecutionTimeMs);

            // Every AI output is advisory.
            Assert.True(generated.ReviewRequired);

            // Rule 3: verbatim echoes — these always bind back to the dropdowns.
            var basicInfo = generated.Data!.BasicInfo!;
            Assert.Equal("Software Developer", basicInfo.JobTitle);
            Assert.Equal("Information Technology", basicInfo.Department);
            Assert.Equal("System Administrator", basicInfo.Designation);

            // Rule 5: vacancies is a starting value of 1.
            Assert.Equal(1, basicInfo.Vacancies);

            // Rule 4: a human publishes.
            Assert.Equal("Draft", generated.Data.Publishing!.Status);
            Assert.False(generated.Data.Publishing.IsPublicJob);

            Assert.Equal(3, generated.Data.Requirements!.ExperienceYears!.Minimum);
            Assert.Equal(6, generated.Data.Requirements.ExperienceYears.Maximum);
            Assert.Equal(5, generated.Data.Requirements.Skills.Count);

            Assert.Equal("parsed_from_request", generated.Meta!.ExperienceSource);
            Assert.Equal("selected_from_options", generated.Meta.JobCategorySource);
        }

        [Fact]
        public async Task Fields_that_belong_to_HR_stay_null_and_are_never_defaulted()
        {
            // Rule 2. Anything filled in here would be the portal putting words in
            // HR's mouth on a document that becomes a legal advertisement.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse);

            var generated = (await client.GenerateJobRequisitionAsync(SampleRequest())).Value!;

            Assert.Null(generated.Data!.BasicInfo!.EmploymentType);
            Assert.Null(generated.Data.BasicInfo.Grade);
            Assert.Null(generated.Data.Compensation!.Benefits);
            Assert.Null(generated.Data.Compensation.BudgetType);
            Assert.Null(generated.Data.Compensation.BudgetLineId);
            Assert.Null(generated.Data.Publishing!.Justification);
            Assert.Null(generated.Data.Publishing.ClosingDate);
        }

        [Fact]
        public async Task An_all_null_age_range_is_treated_as_absent()
        {
            // The contract sends {"minimum": null, "maximum": null} rather than
            // omitting the object, and that must read as "no age limits".
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse);

            var generated = (await client.GenerateJobRequisitionAsync(SampleRequest())).Value!;

            Assert.False(generated.Data!.Requirements!.AgeLimits!.HasValue);
        }

        // ── Enforcing the advisory model at our own boundary ─────────────────

        [Fact]
        public async Task Age_limits_are_discarded_if_the_service_ever_returns_them()
        {
            // Defence in depth. The service enforces this; if a regression, a
            // proxy or a future version ever let an age band through, it must not
            // reach a recruiter's screen — that is a discriminatory job advert.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse.Replace(
                "\"age_limits\": { \"minimum\": null, \"maximum\": null }",
                "\"age_limits\": { \"minimum\": 25, \"maximum\": 35 }"));

            var generated = (await client.GenerateJobRequisitionAsync(SampleRequest())).Value!;

            Assert.Null(generated.Data!.Requirements!.AgeLimits);
        }

        [Fact]
        public async Task A_requisition_the_service_marked_public_is_forced_back_to_draft()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse
                .Replace("\"is_public_job\": false", "\"is_public_job\": true")
                .Replace("\"status\": \"Draft\"", "\"status\": \"Published\""));

            var generated = (await client.GenerateJobRequisitionAsync(SampleRequest())).Value!;

            Assert.False(generated.Data!.Publishing!.IsPublicJob);
            Assert.Equal("Draft", generated.Data.Publishing.Status);
        }

        // ── What we send ─────────────────────────────────────────────────────

        [Fact]
        public async Task Only_a_job_title_is_required()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse);

            var result = await client.GenerateJobRequisitionAsync(new JobRequisitionRequest
            {
                CompanyId = 133,
                JobTitle = "Software Developer"
            });

            Assert.True(result.IsSuccess);

            // Absent fields are OMITTED, not sent as null — the contract asks for
            // omission, and an explicit null is a different statement.
            var body = handler.ReceivedBodies[0];
            Assert.Contains("jobTitle", body);
            Assert.DoesNotContain("department", body);
            Assert.DoesNotContain("keySkills", body);
            Assert.DoesNotContain("null", body);
        }

        [Fact]
        public async Task A_missing_job_title_is_refused_before_any_network_call()
        {
            // Saves a recruiter a 30-second wait for a rejection we can predict.
            var (client, handler) = Build();

            var result = await client.GenerateJobRequisitionAsync(new JobRequisitionRequest
            {
                CompanyId = 133,
                JobTitle = "   "
            });

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.BadRequest, result.Error!.Code);
            Assert.Equal(0, handler.CallCount);
        }

        [Theory]
        [InlineData("N/A")]
        [InlineData("n/a")]
        [InlineData("-")]
        [InlineData("string")]
        [InlineData("TBD")]
        [InlineData("   ")]
        public void Placeholder_values_are_dropped_rather_than_asserted_as_facts(string placeholder)
        {
            // ERP forms are full of these: "-" typed to satisfy a required field,
            // Swagger's "string" default left in place. Forwarded, they become a
            // real constraint on the generated advert.
            Assert.Null(MultinetAiText.Clean(placeholder));
        }

        [Theory]
        [InlineData("Software Developer")]
        [InlineData("Nil Desperandum Analyst")]   // contains "nil" but is not a placeholder
        [InlineData("None of the Above Ltd")]     // starts with "none"
        public void Real_content_that_merely_contains_a_placeholder_word_is_kept(string value)
        {
            Assert.Equal(value, MultinetAiText.Clean(value));
        }

        [Fact]
        public void Cleaning_a_list_drops_junk_and_returns_null_rather_than_an_empty_list()
        {
            // An empty array reads as "there are no valid options", which is a
            // different claim from "I am not telling you the options".
            Assert.Null(MultinetAiText.Clean(new[] { "N/A", "-", "  " }));

            var cleaned = MultinetAiText.Clean(new[] { "UI/UX", "N/A", "UI/UX", "Python Developer" });
            Assert.Equal(new[] { "UI/UX", "Python Developer" }, cleaned);
        }

        // ── Failure handling ─────────────────────────────────────────────────

        [Fact]
        public async Task A_success_envelope_with_no_data_is_refused_rather_than_shown_as_an_empty_draft()
        {
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, """{"status":"success","execution_time_ms":12}""");

            var result = await client.GenerateJobRequisitionAsync(SampleRequest());

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.ContractViolation, result.Error!.Code);
        }

        [Fact]
        public async Task A_422_generation_failure_is_never_retried()
        {
            // Same input, same rejection — retrying costs 30 s of GPU for nothing.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.UnprocessableEntity,
                """{"detail":{"error":"generation_failed"}}""");

            var result = await client.GenerateJobRequisitionAsync(SampleRequest());

            Assert.True(result.IsFailure);
            Assert.False(result.Error!.Retryable);
        }

        [Fact]
        public async Task Unknown_meta_keys_do_not_break_a_good_generation()
        {
            // meta is additive by contract. A new timing field must not cost the
            // recruiter a job description that was generated perfectly well.
            var (client, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ContractResponse.Replace(
                "\"work_mode\": \"Hybrid\",",
                "\"work_mode\": \"Hybrid\", \"a_field_added_next_quarter\": {\"nested\": [1,2,3]},"));

            var result = await client.GenerateJobRequisitionAsync(SampleRequest());

            Assert.True(result.IsSuccess);
            Assert.Equal("Hybrid", result.Value!.Meta!.WorkMode);
            Assert.True(result.Value.Meta.Additional!.ContainsKey("a_field_added_next_quarter"));
        }

        [Fact]
        public void Execution_time_is_bound_as_an_integer_type_not_a_float()
        {
            // System.Text.Json throws binding 20909.0 to an int, and the service
            // deliberately emits an integer. Pinned so nobody "tidies" it to a
            // double and discovers the exception in production.
            var property = typeof(JobRequisitionResult).GetProperty(nameof(JobRequisitionResult.ExecutionTimeMs))!;
            Assert.Equal(typeof(long?), property.PropertyType);

            var parsed = JsonSerializer.Deserialize<JobRequisitionResult>(
                """{"execution_time_ms": 20909}""", MultinetAiProvider.Json);
            Assert.Equal(20909L, parsed!.ExecutionTimeMs);
        }
    }
}

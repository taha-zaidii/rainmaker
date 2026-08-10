using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers.Generic;
using Xunit;

namespace Digi.Recruitment.Module.Tests.GenericProviders
{
    /// <summary>
    /// A general-purpose chat model has no server-side enforcement of the
    /// portal's advisory rules the way Multinet's purpose-built service does —
    /// these client-side re-assertions ARE the enforcement for OpenAI, Anthropic,
    /// Gemini and custom endpoints. If they regress, a model is free to invent an
    /// age range or mark its own draft as published, silently.
    /// </summary>
    public class LlmJsonSupportTests
    {
        [Fact]
        public void Fenced_json_is_extracted_from_markdown()
        {
            var raw = "Sure, here you go:\n```json\n{\"a\": 1}\n```\nLet me know if you need anything else.";
            Assert.Equal("{\"a\": 1}", LlmJsonSupport.ExtractJsonObject(raw));
        }

        [Fact]
        public void Bare_json_surrounded_by_prose_is_extracted()
        {
            var raw = "Here is the result: {\"a\": 1} — hope that helps!";
            Assert.Equal("{\"a\": 1}", LlmJsonSupport.ExtractJsonObject(raw));
        }

        private sealed class SimplePayload
        {
            public int A { get; set; }
        }

        [Fact]
        public void Well_formed_json_parses_successfully()
        {
            var result = LlmJsonSupport.ParseAsContract<SimplePayload>("{\"a\": 5}", "TestProvider");

            Assert.True(result.IsSuccess);
            Assert.Equal(5, result.ValueOrThrow.A);
        }

        [Fact]
        public void Unparseable_text_is_a_contract_violation_not_an_exception()
        {
            var result = LlmJsonSupport.ParseAsContract<SimplePayload>("I cannot help with that request.", "TestProvider");

            Assert.True(result.IsFailure);
            Assert.Equal(AiErrorCode.ContractViolation, result.Error!.Code);
        }
    }

    public class GenericResultInvariantsTests
    {
        private static JobRequisitionRequest Request() => new()
        {
            JobTitle = "Senior .NET Engineer",
            Department = "Engineering",
            Designation = "Senior Engineer",
        };

        private static JobRequisitionResult ResultWithModelSupplied(JobRequisitionRange? ageLimits, bool isPublicJob, string status) => new()
        {
            Data = new JobRequisitionData
            {
                BasicInfo = new JobRequisitionBasicInfo
                {
                    JobTitle = "something the model paraphrased",
                    Department = "a different department",
                    Vacancies = 4,
                    EmploymentType = "Full-time",
                    Grade = "L5",
                },
                Requirements = new JobRequisitionRequirements { AgeLimits = ageLimits },
                Compensation = new JobRequisitionCompensation { Benefits = "Health insurance", BudgetType = "Opex" },
                Publishing = new JobRequisitionPublishing { IsPublicJob = isPublicJob, Status = status, Justification = "because" },
            },
        };

        [Fact]
        public void Age_limits_are_stripped_even_if_the_model_filled_them_in()
        {
            var result = ResultWithModelSupplied(new JobRequisitionRange { Minimum = 25, Maximum = 35 }, isPublicJob: false, status: "Draft");

            GenericResultInvariants.Enforce(result, Request());

            Assert.Null(result.Data!.Requirements!.AgeLimits);
        }

        [Fact]
        public void A_model_cannot_mark_its_own_draft_as_published()
        {
            var result = ResultWithModelSupplied(null, isPublicJob: true, status: "Published");

            GenericResultInvariants.Enforce(result, Request());

            Assert.False(result.Data!.Publishing!.IsPublicJob);
            Assert.Equal("Draft", result.Data.Publishing.Status);
        }

        [Fact]
        public void Title_department_and_designation_are_verbatim_echoes_not_paraphrases()
        {
            var request = Request();
            var result = ResultWithModelSupplied(null, isPublicJob: false, status: "Draft");

            GenericResultInvariants.Enforce(result, request);

            Assert.Equal(request.JobTitle, result.Data!.BasicInfo!.JobTitle);
            Assert.Equal(request.Department, result.Data.BasicInfo.Department);
        }

        [Fact]
        public void Null_by_design_fields_are_forced_null_regardless_of_the_model()
        {
            var result = ResultWithModelSupplied(null, isPublicJob: false, status: "Draft");

            GenericResultInvariants.Enforce(result, Request());

            Assert.Null(result.Data!.BasicInfo!.EmploymentType);
            Assert.Null(result.Data.BasicInfo.Grade);
            Assert.Null(result.Data.Compensation!.Benefits);
            Assert.Null(result.Data.Compensation.BudgetType);
            Assert.Null(result.Data.Publishing!.Justification);
            Assert.Equal(1, result.Data.BasicInfo.Vacancies);
        }

        [Theory]
        [InlineData(85, 80, true)]   // above threshold
        [InlineData(80, 80, true)]   // exactly at threshold
        [InlineData(60, 80, false)]  // below threshold
        public void Shortlisted_is_derived_from_the_threshold_not_the_models_own_claim(int matchScore, int threshold, bool expectedShortlisted)
        {
            // A model might say "shortlisted: true" for a low score out of
            // over-eagerness; the portal's own >= comparison is what actually governs it.
            var result = new ScreenCandidateResult { MatchScore = matchScore, Shortlisted = !expectedShortlisted };

            GenericResultInvariants.Enforce(result, threshold, executionTimeMs: 123);

            Assert.Equal(expectedShortlisted, result.Shortlisted);
            Assert.True(result.ReviewRequired);
            Assert.True(result.Advisory);
            Assert.Equal(123, result.ExecutionTimeMs);
        }
    }
}

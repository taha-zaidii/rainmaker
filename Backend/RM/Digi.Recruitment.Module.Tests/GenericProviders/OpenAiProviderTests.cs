using System.Net;
using Digi.Core.AI.Contracts;
using Digi.Core.AI.Providers;
using Digi.Recruitment.Module.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Digi.Recruitment.Module.Tests.GenericProviders
{
    /// <summary>
    /// Pins that a company's saved "Model" setting actually reaches the wire for
    /// a generic provider — the interface carries it, but only this proves
    /// OpenAiProvider does not silently ignore it in favor of its own default.
    /// </summary>
    public class OpenAiProviderTests
    {
        private const string ChatCompletionsSuccessBody = """
        {
          "choices": [{ "message": { "content": "{\"status\":\"success\",\"review_required\":true,\"data\":{\"step_1_basic_info\":{\"job_title\":\"Senior .NET Engineer\",\"vacancies\":1},\"step_2_requirements\":{},\"step_3_compensation\":{},\"step_4_publishing\":{\"status\":\"Draft\",\"is_public_job\":false}}}" } }],
          "usage": { "total_tokens": 42 }
        }
        """;

        private static (OpenAiProvider Provider, StubHttpMessageHandler Handler) Build()
        {
            var handler = new StubHttpMessageHandler();
            var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
            return (new OpenAiProvider(http, NullLogger<OpenAiProvider>.Instance), handler);
        }

        private static JobRequisitionRequest Request() => new() { JobTitle = "Senior .NET Engineer" };

        [Fact]
        public async Task A_company_configured_model_is_sent_on_the_wire_instead_of_the_default()
        {
            var (provider, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ChatCompletionsSuccessBody);

            var result = await provider.GenerateJobRequisitionAsync(Request(), apiKey: "test-key", model: "gpt-4-turbo");

            Assert.True(result.IsSuccess);
            Assert.Contains("\"model\":\"gpt-4-turbo\"", handler.ReceivedBodies[0]);
        }

        [Fact]
        public async Task No_model_configured_falls_back_to_the_providers_own_default()
        {
            var (provider, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ChatCompletionsSuccessBody);

            var result = await provider.GenerateJobRequisitionAsync(Request(), apiKey: "test-key");

            Assert.True(result.IsSuccess);
            Assert.DoesNotContain("gpt-4-turbo", handler.ReceivedBodies[0]);
            Assert.Contains("\"model\":\"gpt-4o-mini\"", handler.ReceivedBodies[0]);
        }

        [Fact]
        public async Task Blank_model_is_treated_the_same_as_no_model()
        {
            var (provider, handler) = Build();
            handler.Respond(HttpStatusCode.OK, ChatCompletionsSuccessBody);

            var result = await provider.GenerateJobRequisitionAsync(Request(), apiKey: "test-key", model: "   ");

            Assert.True(result.IsSuccess);
            Assert.Contains("\"model\":\"gpt-4o-mini\"", handler.ReceivedBodies[0]);
        }
    }
}

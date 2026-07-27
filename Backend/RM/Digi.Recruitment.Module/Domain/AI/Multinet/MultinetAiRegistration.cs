using System.Net;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Digi.Recruitment.Module.Domain.AI.Multinet
{
    /// <summary>
    /// Wires up Multinet's in-house AI service: options binding with start-up
    /// validation, a typed HttpClient with a resilience policy shaped by the
    /// service's runtime semantics, and the stub/real switch.
    /// </summary>
    public static class MultinetAiRegistration
    {
        /// <summary>Logical name of the HttpClient, used by the typed client and by health checks.</summary>
        public const string HttpClientName = "multinet-ai";

        public static IServiceCollection AddMultinetAiService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddOptions<MultinetAiOptions>()
                .Bind(configuration.GetSection(MultinetAiOptions.SectionName))
                .ValidateOnStart();

            // Named validator so a misconfiguration fails the build-up with a
            // message that says what to fix, instead of a 401 on first upload.
            services.AddSingleton<IValidateOptions<MultinetAiOptions>, MultinetAiOptionsValidator>();

            services
                .AddHttpClient<MultinetAiClient>(HttpClientName, (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<MultinetAiOptions>>().Value;

                    // Trailing slash matters: without it, BaseAddress composition
                    // drops the last path segment of the base URL.
                    var baseUrl = options.BaseUrl.TrimEnd('/') + "/";
                    client.BaseAddress = new Uri(baseUrl);

                    // Total budget for one logical call, INCLUDING retries. A
                    // parse legitimately runs 40–90 s, so the contract's 180 s
                    // floor is the real constraint here. Retries only fire on
                    // fast transient failures (refused connection, 5xx), so they
                    // do not meaningfully eat into this; once the budget is spent
                    // the cancellation stops further attempts, which is correct —
                    // re-queuing is the job of the parse queue, not the client.
                    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

                    client.DefaultRequestHeaders.Accept.Add(
                        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Digi.Recruitment.Module/1.0 (+multinet-erp)");
                })
                .AddPolicyHandler(BuildRetryPolicy)
                // The GPU is single-flight and a parse can hold the connection for
                // 90 s, so pooled handlers must not be recycled aggressively.
                .SetHandlerLifetime(TimeSpan.FromMinutes(10));

            services.AddSingleton<StubMultinetAiClient>();

            // One resolution point for "which client is in play". Everything
            // upstream depends on the interface and never learns which it got,
            // except through IMultinetAiClient.IsStub for banner purposes.
            services.AddTransient<IMultinetAiClient>(serviceProvider =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MultinetAiOptions>>().Value;
                return options.StubMode
                    ? serviceProvider.GetRequiredService<StubMultinetAiClient>()
                    : serviceProvider.GetRequiredService<MultinetAiClient>();
            });

            return services;
        }

        /// <summary>
        /// Retry policy. The critical rule from the contract: a 422 is a verdict
        /// about the DOCUMENT, so retrying it wastes 40–90 s of GPU time on a
        /// guaranteed identical answer. Only genuinely transient conditions are
        /// replayed, with exponential backoff plus jitter so parallel workers do
        /// not resynchronise onto the single GPU lock.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy(
            IServiceProvider serviceProvider,
            HttpRequestMessage request)
        {
            var options = serviceProvider.GetRequiredService<IOptions<MultinetAiOptions>>().Value;
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(MultinetAiRegistration));

            // Deterministic jitter seed per policy instance; Random.Shared is
            // thread-safe and we only need spread, not unpredictability.
            return HttpPolicyExtensions
                .HandleTransientHttpError()                                     // 5xx and 408
                .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    options.MaxRetries,
                    retryAttempt =>
                    {
                        var backoff = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 750));
                        return backoff + jitter;
                    },
                    onRetry: (outcome, delay, attempt, _) =>
                    {
                        var reason = outcome.Result is not null
                            ? $"HTTP {(int)outcome.Result.StatusCode}"
                            : outcome.Exception?.GetType().Name ?? "unknown";

                        logger.LogWarning(
                            "AI service call to {Path} failed ({Reason}); retry {Attempt}/{Max} in {Delay}.",
                            request.RequestUri?.PathAndQuery, reason, attempt, options.MaxRetries, delay);
                    });
        }
    }

    /// <summary>
    /// Turns <see cref="MultinetAiOptions.Validate"/> into a start-up failure with
    /// an actionable message. Fail fast beats a service that starts happily and
    /// 401s on the first resume a recruiter uploads.
    /// </summary>
    internal sealed class MultinetAiOptionsValidator : IValidateOptions<MultinetAiOptions>
    {
        public ValidateOptionsResult Validate(string? name, MultinetAiOptions options)
        {
            var problems = options.Validate();
            return problems.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(problems);
        }
    }
}

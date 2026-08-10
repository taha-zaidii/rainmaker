using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Digi.Core.AI.Providers;

namespace Digi.Core.AI.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreAI(this IServiceCollection services, IConfiguration configuration)
        {
            // Options
            services.Configure<MultinetAiOptions>(configuration.GetSection(MultinetAiOptions.SectionName));

            // Factory/Resolver
            services.AddSingleton<IAIServiceProviderResolver, AiServiceProviderResolver>();

            // Generic providers take their base URL and key per call (from the
            // company's own AI settings row), so they need no options section of
            // their own — only an HttpClient with a timeout and the same
            // transient-fault retry policy Multinet's client uses. 180s matches
            // the master directive's Phase 2 floor for every provider, not just
            // Multinet — a generic provider doing resume extraction from a large
            // document is not guaranteed to be fast just because it usually is.
            var genericProviderTimeout = TimeSpan.FromSeconds(180);
            services.AddHttpClient<OpenAiProvider>(client => client.Timeout = genericProviderTimeout)
                .AddPolicyHandler(GenericProviderRetryPolicy());
            services.AddHttpClient<AnthropicProvider>(client => client.Timeout = genericProviderTimeout)
                .AddPolicyHandler(GenericProviderRetryPolicy());
            services.AddHttpClient<GoogleGeminiProvider>(client => client.Timeout = genericProviderTimeout)
                .AddPolicyHandler(GenericProviderRetryPolicy());
            services.AddHttpClient<CustomAiProvider>(client => client.Timeout = genericProviderTimeout)
                .AddPolicyHandler(GenericProviderRetryPolicy());

            // Register MultinetAI Client with HttpClient and Polly
            services.AddHttpClient<MultinetAiProvider>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<MultinetAiOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            })
            .AddPolicyHandler((sp, request) =>
            {
                var options = sp.GetRequiredService<IOptions<MultinetAiOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<MultinetAiProvider>>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .WaitAndRetryAsync(
                        retryCount: options.MaxRetries,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            logger.LogWarning("Delaying for {Delay}ms, then making retry {Retry}. Error: {Error}",
                                timespan.TotalMilliseconds, retryAttempt, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                        });
            });

            return services;
        }

        /// <summary>
        /// Two retries with exponential backoff on transient HTTP failures
        /// (5xx, request timeout) and connection errors — mirrors Multinet's own
        /// policy. <c>HandleTransientHttpError</c> already excludes 4xx, so an
        /// invalid key or a malformed request is never retried, only genuinely
        /// transient conditions are.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> GenericProviderRetryPolicy() =>
            HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutRejectedException>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}

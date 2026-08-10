using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Digi.Core.AI.Contracts;

namespace Digi.Core.AI.Providers.Generic
{
    /// <summary>
    /// The wire protocol OpenAI itself, and — per the portal's own "custom
    /// provider" design — most third-party gateways (Groq, DeepSeek, a
    /// self-hosted vLLM/Ollama front end) all speak: POST {base}/chat/completions
    /// with a bearer key. One implementation covers <see cref="OpenAiProvider"/>
    /// and <see cref="CustomAiProvider"/> rather than duplicating this for both.
    /// </summary>
    internal static class OpenAiCompatibleChat
    {
        public static async Task<AiResult<string>> CompleteAsync(
            HttpClient httpClient,
            Uri baseUri,
            string apiKey,
            string model,
            string systemPrompt,
            string userPrompt,
            string providerLabel,
            CancellationToken cancellationToken)
        {
            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt },
                },
                temperature = 0.2,
                response_format = new { type = "json_object" },
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, CombineChatCompletionsUrl(baseUri))
            {
                Content = JsonContent.Create(requestBody),
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(httpRequest, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return AiResult<string>.Fail(AiErrorCode.Timeout, $"{providerLabel} did not respond in time.", retryable: true);
            }
            catch (HttpRequestException ex)
            {
                return AiResult<string>.Fail(AiErrorCode.Unreachable, $"Could not reach {providerLabel}: {ex.Message}", retryable: true);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return AiResult<string>.Fail(MapError(response.StatusCode, body, providerLabel));
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
                return string.IsNullOrWhiteSpace(text)
                    ? AiResult<string>.Fail(AiErrorCode.ContractViolation, $"{providerLabel} returned an empty completion.")
                    : AiResult<string>.Ok(text!);
            }
            catch (Exception ex)
            {
                return AiResult<string>.Fail(
                    AiErrorCode.ContractViolation,
                    $"{providerLabel}'s response did not match the expected chat-completions shape: {ex.Message}");
            }
        }

        private static Uri CombineChatCompletionsUrl(Uri baseUri)
        {
            var basePath = baseUri.ToString();
            if (!basePath.EndsWith('/'))
            {
                basePath += "/";
            }

            return new Uri(basePath + "chat/completions");
        }

        private static AiError MapError(HttpStatusCode status, string body, string providerLabel)
        {
            string? message = null;
            string? code = null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var err))
                {
                    message = err.TryGetProperty("message", out var m) ? m.GetString() : null;
                    code = err.TryGetProperty("code", out var c)
                        ? c.GetString()
                        : err.TryGetProperty("type", out var t) ? t.GetString() : null;
                }
            }
            catch (JsonException)
            {
                // Not every gateway returns OpenAI's exact error envelope; fall through to a generic message.
            }

            var safeMessage = message ?? $"{providerLabel} returned HTTP {(int)status}.";

            return status switch
            {
                HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                    new AiError(AiErrorCode.Unauthorized, $"{providerLabel} rejected the API key.", (int)status, false, code),
                HttpStatusCode.TooManyRequests =>
                    new AiError(AiErrorCode.Busy, safeMessage, (int)status, true, code),
                HttpStatusCode.RequestEntityTooLarge =>
                    new AiError(AiErrorCode.FileTooLarge, safeMessage, (int)status, false, code),
                _ when (int)status >= 500 =>
                    new AiError(AiErrorCode.InternalError, safeMessage, (int)status, true, code),
                _ =>
                    new AiError(AiErrorCode.BadRequest, safeMessage, (int)status, false, code),
            };
        }
    }
}

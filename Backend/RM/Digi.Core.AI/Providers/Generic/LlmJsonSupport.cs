using System.Text.Json;
using System.Text.RegularExpressions;
using Digi.Core.AI.Contracts;

namespace Digi.Core.AI.Providers.Generic
{
    /// <summary>
    /// General-purpose chat models (OpenAI, Anthropic, Gemini, and OpenAI-compatible
    /// "custom" backends) are asked to answer with pure JSON, but nothing enforces
    /// that the way Multinet's purpose-built endpoints do: a model can still wrap
    /// the object in a markdown fence, add a leading sentence, or trail off after
    /// truncation. This is the one place that tolerates that, so every generic
    /// provider gets the same forgiving-but-honest parse instead of four
    /// slightly-different regexes.
    /// </summary>
    internal static class LlmJsonSupport
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        /// <summary>Pulls the JSON object out of raw model text, tolerating a ```json fence or stray prose around it.</summary>
        public static string ExtractJsonObject(string rawText)
        {
            var fenced = Regex.Match(rawText, @"```(?:json)?\s*(\{[\s\S]*\})\s*```", RegexOptions.IgnoreCase);
            if (fenced.Success)
            {
                return fenced.Groups[1].Value;
            }

            var start = rawText.IndexOf('{');
            var end = rawText.LastIndexOf('}');
            return start >= 0 && end > start ? rawText[start..(end + 1)] : rawText;
        }

        /// <summary>
        /// Deserializes the model's answer into the contract shape, or a
        /// <see cref="AiErrorCode.ContractViolation"/> result if the model did not
        /// return usable JSON — the same domain code Multinet's client uses for
        /// "a 2xx whose body did not match the contract."
        /// </summary>
        public static AiResult<T> ParseAsContract<T>(string rawText, string providerName) where T : class
        {
            string json;
            try
            {
                json = ExtractJsonObject(rawText);
                var value = JsonSerializer.Deserialize<T>(json, DeserializeOptions);
                if (value is null)
                {
                    return AiResult<T>.Fail(
                        AiErrorCode.ContractViolation,
                        $"{providerName} returned an empty or unusable response.");
                }

                return AiResult<T>.Ok(value);
            }
            catch (JsonException ex)
            {
                return AiResult<T>.Fail(
                    AiErrorCode.ContractViolation,
                    $"{providerName}'s response could not be read as the expected JSON shape: {ex.Message}");
            }
        }
    }
}

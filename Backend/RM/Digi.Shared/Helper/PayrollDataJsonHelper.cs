using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digi.Shared.Helper
{
    public class PayrollDataItemDto
    {
        [JsonPropertyName("n")]
        public string N { get; set; } = string.Empty;

        [JsonPropertyName("v")]
        public string? V { get; set; }

        [JsonPropertyName("t")]
        public string? T { get; set; }

        [JsonPropertyName("r")]
        public string? R { get; set; }
    }

    public static class PayrollDataJsonHelper
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static List<PayrollDataItemDto> ParseItems(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<PayrollDataItemDto>();

            var trimmed = json.TrimStart();
            if (trimmed.StartsWith("["))
            {
                return JsonSerializer.Deserialize<List<PayrollDataItemDto>>(json, DeserializeOptions)
                    ?? new List<PayrollDataItemDto>();
            }

            if (trimmed.StartsWith("{"))
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, DeserializeOptions);
                if (dict == null || dict.Count == 0)
                    return new List<PayrollDataItemDto>();

                return dict.Select(kv => new PayrollDataItemDto
                {
                    N = kv.Key,
                    V = FormatJsonElementValue(kv.Value),
                    T = "Info",
                    R = "Info"
                }).ToList();
            }

            return new List<PayrollDataItemDto>();
        }

        private static string? FormatJsonElementValue(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => value.GetString(),
            _ => value.ToString()
        };
    }
}

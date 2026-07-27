using System.Text.Json;
using System.Text.Json.Serialization;

namespace Digi.Shared.Helper
{
    public class JsonTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var timeString = reader.GetString();
                return ParseTimeString(timeString);
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString(@"hh\:mm\:ss"));
            else
                writer.WriteNullValue();
        }

        private TimeSpan? ParseTimeString(string timeString)
        {
            if (string.IsNullOrWhiteSpace(timeString))
                return null;

            // Standard TimeSpan parse
            if (TimeSpan.TryParse(timeString, out TimeSpan result))
                return result;

            // Custom parsing logic
            return ParseCustomTimeFormat(timeString);
        }

        private TimeSpan? ParseCustomTimeFormat(string timeString)
        {
            // Remove any whitespace
            timeString = timeString.Trim();

            // Handle HH:MM or HH:MM:SS format
            if (timeString.Contains(':'))
            {
                var parts = timeString.Split(':');

                if (parts.Length >= 2)
                {
                    if (int.TryParse(parts[0], out int hours) && int.TryParse(parts[1], out int minutes))
                    {
                        int seconds = 0;
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int sec))
                            seconds = sec;

                        // For time of day, validate hours 0-23, but allow up to 23:59:59
                        // For duration, allow any positive hours
                        if (hours >= 0 && 
                            IsValidTimeComponent(minutes, 0, 59) &&
                            IsValidTimeComponent(seconds, 0, 59))
                        {
                            return new TimeSpan(hours, minutes, seconds);
                        }
                    }
                }
            }

            // Handle 24-hour format without colon (e.g., "1430" or "0930")
            if (timeString.Length == 4 && int.TryParse(timeString, out int timeNumber))
            {
                int hours = timeNumber / 100;
                int minutes = timeNumber % 100;

                if (hours >= 0 && hours <= 23 && IsValidTimeComponent(minutes, 0, 59))
                    return new TimeSpan(hours, minutes, 0);
            }

            // Handle HHMM format (3 or 4 digits)
            if ((timeString.Length == 3 || timeString.Length == 4) && int.TryParse(timeString, out int timeNum))
            {
                if (timeString.Length == 3)
                {
                    // "930" -> 9:30
                    int hours = timeNum / 100;
                    int minutes = timeNum % 100;
                    if (hours >= 0 && hours <= 9 && IsValidTimeComponent(minutes, 0, 59))
                        return new TimeSpan(hours, minutes, 0);
                }
            }

            return null;
        }

        private bool IsValidTimeComponent(int value, int min, int max)
        {
            return value >= min && value <= max;
        }
    }
}


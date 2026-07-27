using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public sealed class JsonTimeOnlyConverter : JsonConverter<TimeOnly>
    {
        private const string Format = "HH:mm:ss";
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                return TimeOnly.ParseExact(reader.GetString(), Format, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }

        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}

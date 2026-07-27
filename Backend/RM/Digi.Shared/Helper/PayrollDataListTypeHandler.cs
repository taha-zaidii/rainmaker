using Dapper;
using System.Data;

namespace Digi.Shared.Helper
{
    public sealed class PayrollDataListTypeHandler : SqlMapper.TypeHandler<List<PayrollDataItemDto>>
    {
        public override List<PayrollDataItemDto> Parse(object value)
        {
            if (value == null || value is DBNull)
                return new List<PayrollDataItemDto>();

            return PayrollDataJsonHelper.ParseItems(value.ToString());
        }

        public override void SetValue(IDbDataParameter parameter, List<PayrollDataItemDto>? value)
        {
            parameter.Value = value == null || value.Count == 0
                ? DBNull.Value
                : System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}

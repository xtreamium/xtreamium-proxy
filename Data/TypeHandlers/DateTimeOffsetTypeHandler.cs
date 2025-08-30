using Dapper;

namespace Xtreamium.Proxy.Data.TypeHandlers;

public class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset> {
  public override DateTimeOffset Parse(object value) {
    return Convert.ToDateTime(value.ToString());
  }

  public override void SetValue(System.Data.IDbDataParameter parameter,
    DateTimeOffset value) {
    parameter.Value = value;
  }
}

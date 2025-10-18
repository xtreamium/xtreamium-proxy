using Dapper.Contrib.Extensions;

namespace Xtreamium.Proxy.Data.Models;

[Table("settings")]
public record Setting {
  [Key]
  public int Id { get; set; }

  public required string Key { get; set; }
  public required string Value { get; set; }
}

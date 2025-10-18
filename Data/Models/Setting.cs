using Dapper.Contrib.Extensions;

namespace Xtreamium.Proxy.Data.Models;

[Table("settings")]
public record Setting {
  [Key]
  public int Id { get; set; }

  public required string MediaPlayerPath { get; set; }
  public required string MediaPlayerArguments { get; set; }
  public required string RecordingsPath { get; set; }
  public int Port { get; set; } = 8080;
}

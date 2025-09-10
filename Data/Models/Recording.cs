using Dapper.Contrib.Extensions;

namespace Xtreamium.Proxy.Data.Models;

[Table("recordings")]
public record Recording {
  [Key]
  public int Id { get; set; }

  public required string JobId { get; set; }

  public required string Url { get; set; }
  public required string Title { get; set; }
  public DateTimeOffset StartTime { get; set; }
  public int Duration { get; set; }
  public bool IsRecorded { get; set; } = false;
  public string? FilePath { get; set; }
}

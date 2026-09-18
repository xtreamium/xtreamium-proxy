namespace Xtreamium.Tray.Models;

// Local mirrors of xtreamium-proxy's Data/Models/Recording.cs and Models/RecordingEvents.cs.
// Kept as plain DTOs rather than a project/package reference, since xtreamium-proxy and
// xtreamium-tray are independent repos with independent release cadences.

public record RecordingDto {
  public Guid Id { get; init; }
  public string JobId { get; init; } = "";
  public string Title { get; init; } = "";

  /// <summary>"pending" | "recording" | "complete" | "partial" | "failed"</summary>
  public string Status { get; init; } = "pending";
}

public record RecordingChangedEventDto {
  public Guid Id { get; init; }
  public string JobId { get; init; } = "";
  public string Title { get; init; } = "";
  public string Status { get; init; } = "";

  /// <summary>"created" | "status" | "deleted"</summary>
  public string Change { get; init; } = "";
}

public record RecordingProgressEventDto {
  public Guid Id { get; init; }
  public string JobId { get; init; } = "";
  public double CapturedSeconds { get; init; }
  public double ElapsedSeconds { get; init; }
  public double DurationSeconds { get; init; }
}

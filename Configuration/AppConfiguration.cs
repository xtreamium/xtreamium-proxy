namespace Xtreamium.Proxy.Configuration;

/// <summary>
/// Strongly-typed configuration for application settings
/// </summary>
public class AppConfiguration {
  public const string SectionName = "App";

  public VideoPlayerConfiguration VideoPlayer { get; set; } = new();
  public RecordingsConfiguration Recordings { get; set; } = new();
  public List<string> AllowedCorsOrigins { get; set; } = new();
}

public class VideoPlayerConfiguration {
  /// <summary>
  /// Default MPV arguments constant for use in migrations and fallback scenarios
  /// </summary>
  public const string DefaultMpvArguments = "--no-border --ontop --screen=2 --cache=yes --demuxer-max-bytes=5GiB --demuxer-max-back-bytes=5GiB {{URL}}";

  public string Executable { get; set; } = "/usr/bin/mpv";

  public string DefaultArguments { get; set; } = string.Empty;
}

public class RecordingsConfiguration {
  private static readonly string DefaultRecordingsPath = GetDefaultRecordingsPath();

  private static string GetDefaultRecordingsPath() {
    // For Windows Services running as system account, use CommonApplicationData
    // For regular user applications, use MyDocuments
    if (OperatingSystem.IsWindows() && 
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("USERPROFILE"))) {
      // Running as Windows Service (LOCAL SYSTEM)
      return System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Xtreamium",
        "Recordings");
    }
    
    // Regular user context
    return System.IO.Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
      "XtreamiumRecordings");
  }

  public string Path { get; set; } = DefaultRecordingsPath;
  public int MinDurationMinutes { get; set; } = 1;
  public int MaxDurationMinutes { get; set; } = 600;
}

/// <summary>
/// CORS configuration
/// </summary>
public class CorsConfiguration {
  public const string SectionName = "Cors";

  public List<string> AllowedOrigins { get; set; } = new();
}

using System.Diagnostics;
using System.Security;
using Xtreamium.Proxy.Data.Repositories;

namespace Xtreamium.Proxy.Services;

public class VideoPlayerService(
  ILogger<VideoPlayerService> logger,
  ISettingsRepository settingsRepository)
  : IVideoPlayerService {
  public async Task<bool> PlayFromUrl(string url, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(url)) {
      logger.LogWarning("PlayFromUrl called with empty URL");
      return false;
    }

    try {
      var exe = await settingsRepository.GetSettingAsync("MediaPlayerPath");
      var args = SanitizeMpvArguments(
        await settingsRepository.GetSettingAsync("MediaPlayerArguments"),
        url);

      if (string.IsNullOrWhiteSpace(exe)) {
        logger.LogError("Video player executable not configured");
        return false;
      }

      if (!File.Exists(exe)) {
        logger.LogWarning("Configured video player executable not found: {Path}", exe);
      }

      var psi = new ProcessStartInfo {
        FileName = exe,
        Arguments = args,
        CreateNoWindow = true,
        UseShellExecute = false
      };

      logger.LogDebug("Starting player: {FileName} {Arguments}", psi.FileName, psi.Arguments);

      using var process = Process.Start(psi);
      if (process != null) {
        return true;
      }

      logger.LogError("Failed to start video player process");
      return false;
    } catch (OperationCanceledException) {
      logger.LogInformation("PlayFromUrl canceled");
      return false;
    } catch (Exception ex) {
      logger.LogError(ex, "Error while starting video player");
      return false;
    }
  }

  /// <summary>
  /// Safely quote and sanitize command arguments
  /// </summary>
  private static string QuoteArgument(string arg) {
    if (string.IsNullOrEmpty(arg))
      return "\"\"";

    // Remove potentially dangerous characters
    var sanitized = arg.Replace("\"", "\\\"")
      .Replace(";", "")
      .Replace("&", "")
      .Replace("|", "")
      .Replace("`", "")
      .Replace("$", "")
      .Replace("(", "")
      .Replace(")", "");

    // Always quote to prevent injection
    return $"\"{sanitized}\"";
  }

  /// <summary>
  /// Validate and sanitize MPV arguments template
  /// </summary>
  private static string SanitizeMpvArguments(string argumentTemplate, string url) {
    if (string.IsNullOrWhiteSpace(argumentTemplate)) {
      return QuoteArgument(url);
    }

    // Check for potentially dangerous argument patterns
    var dangerous = new[] {"--input-terminal", "--terminal", "--script", "--load-scripts"};
    if (dangerous.Any(d => argumentTemplate.Contains(d, StringComparison.OrdinalIgnoreCase))) {
      throw new SecurityException("Potentially dangerous MPV arguments detected");
    }

    // Replace URL placeholder safely
    if (argumentTemplate.Contains("{{URL}}")) {
      return argumentTemplate.Replace("{{URL}}", QuoteArgument(url));
    }

    // If no placeholder, append URL safely
    return $"{argumentTemplate} {QuoteArgument(url)}";
  }
}

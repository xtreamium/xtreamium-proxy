using System.Diagnostics;
using System.Security;
using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Services;

public class VideoPlayerService(
  ILogger<VideoPlayerService> logger,
  IOptions<AppConfiguration> config)
  : IVideoPlayerService {
  private readonly AppConfiguration _config = config.Value;

  public Task<bool> PlayFromUrl(string url, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(url)) {
      logger.LogWarning("PlayFromUrl called with empty URL");
      return Task.FromResult(false);
    }

    try {
      var exe = _config.VideoPlayer.MediaPlayerPath;

      if (string.IsNullOrWhiteSpace(exe)) {
        logger.LogError("Video player executable not configured");
        return Task.FromResult(false);
      }

      if (!File.Exists(exe)) {
        logger.LogWarning("Configured video player executable not found: {Path}", exe);
        // Still attempt to start in case it's on PATH; remove check if you want that behavior.
      }

      var args = SanitizeMpvArguments(_config.VideoPlayer.MediaPlayerArguments, url);

      var psi = new ProcessStartInfo {
        FileName = exe,
        Arguments = args,
        CreateNoWindow = true,
        UseShellExecute = false
      };

      logger.LogDebug("Starting player: {FileName} {Arguments}", psi.FileName, psi.Arguments);

      using var process = Process.Start(psi);
      if (process == null) {
        logger.LogError("Failed to start video player process");
        return Task.FromResult(false);
      }
      return Task.FromResult(true);
    } catch (OperationCanceledException) {
      logger.LogInformation("PlayFromUrl canceled");
      return Task.FromResult(false);
    } catch (Exception ex) {
      logger.LogError(ex, "Error while starting video player");
      return Task.FromResult(false);
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

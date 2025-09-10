using System.Diagnostics;
using Xtreamium.Proxy.Data.Services;

namespace Xtreamium.Proxy.Services;

public class VideoPlayerService(ILogger<VideoPlayerService> logger, IConfiguration config) {
  public async Task<bool> PlayFromUrl(string url, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(url)) {
      logger.LogWarning("PlayFromUrl called with empty URL");
      return false;
    }

    try {
      var settings = await SettingsHelper.GetSettings();
      var exe = config["Tools:VideoPlayer:Executable"];

      if (string.IsNullOrWhiteSpace(exe)) {
        logger.LogError("Video player executable not configured at Tools:VideoPlayer:Executable");
        return false;
      }

      if (!File.Exists(exe)) {
        logger.LogWarning("Configured video player executable not found: {Path}", exe);
        // Still attempt to start in case it's on PATH; remove check if you want that behavior.
      }

      var args = settings.MpvArguments != null && settings.MpvArguments.Contains("{{URL}}")
        ? settings.MpvArguments.Replace("{{URL}}", QuoteArgument(url))
        : $"{settings.MpvArguments ?? string.Empty} {QuoteArgument(url)}";

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
        return false;
      }

      // Do not wait for exit by default — player runs independently.
      // If you want to wait: await process.WaitForExitAsync(cancellationToken);

      return true;
    } catch (OperationCanceledException) {
      logger.LogInformation("PlayFromUrl canceled");
      return false;
    } catch (Exception ex) {
      logger.LogError(ex, "Error while starting video player");
      return false;
    }
  }

  private static string QuoteArgument(string arg) {
    if (string.IsNullOrEmpty(arg))
      return "\"\"";

    // Simple quoting for spaces and preserving existing quotes.
    if (arg.Contains(' ') || arg.Contains('"')) {
      return "\"" + arg.Replace("\"", "\\\"") + "\"";
    }

    return arg;
  }
}

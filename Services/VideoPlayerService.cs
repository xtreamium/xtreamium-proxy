using System.Diagnostics;
using System.Security;
using System.Collections.Concurrent;
using Xtreamium.Proxy.Data.Repositories;

namespace Xtreamium.Proxy.Services;

public class VideoPlayerService(
  ILogger<VideoPlayerService> logger,
  ISettingsRepository settingsRepository)
  : IVideoPlayerService {
  private static readonly ConcurrentDictionary<string, TrackedPlayerProcess> ActivePlayersByExecutable =
    new(StringComparer.OrdinalIgnoreCase);
  private static readonly SemaphoreSlim PlayerLaunchLock = new(1, 1);

  public async Task<bool> PlayFromUrl(string url, CancellationToken cancellationToken = default) {
    if (string.IsNullOrWhiteSpace(url)) {
      logger.LogWarning("PlayFromUrl called with empty URL");
      return false;
    }

    try {
      var pathOrUrl = url;
      if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) {
        //This is a recording, need to sanitise the path
        pathOrUrl = url.Substring(7); // Remove "file://" prefix

        // On Windows, handle file:///C:/path format
        if (OperatingSystem.IsWindows() && pathOrUrl.StartsWith($"/") &&
            pathOrUrl.Length > 2 && pathOrUrl[2] == ':') {
          pathOrUrl = pathOrUrl[1..]; // Remove leading slash for Windows paths
          //TODO: Revisit all this on an actual Windows installation
        }

        if (!File.Exists(pathOrUrl)) {
          logger.LogError("Local file not found: {Path}", pathOrUrl);
          return false;
        }

        logger.LogDebug("Playing local file: {Path}", pathOrUrl);
      }

      var exe = await settingsRepository.GetSettingAsync("MediaPlayerPath");
      var args = SanitizeMpvArguments(
        await settingsRepository.GetSettingAsync("MediaPlayerArguments"),
        pathOrUrl);

      if (string.IsNullOrWhiteSpace(exe)) {
        logger.LogError("Video player executable not configured");
        return false;
      }

      if (!File.Exists(exe)) {
        logger.LogWarning("Configured video player executable not found: {Path}", exe);
      }

      var executableKey = NormalizeExecutablePath(exe);

      await PlayerLaunchLock.WaitAsync(cancellationToken);
      try {
        StopTrackedPlayer(executableKey);

        var psi = new ProcessStartInfo {
          FileName = exe,
          Arguments = args,
          CreateNoWindow = true,
          UseShellExecute = false
        };

        logger.LogDebug("Starting player: {FileName} {Arguments}", psi.FileName, psi.Arguments);

        using var process = Process.Start(psi);
        if (process != null) {
          ActivePlayersByExecutable[executableKey] =
            new TrackedPlayerProcess(process.Id, process.StartTime.ToUniversalTime());
          return true;
        }

        logger.LogError("Failed to start video player process");
        return false;
      } finally {
        PlayerLaunchLock.Release();
      }
    } catch (OperationCanceledException) {
      logger.LogInformation("PlayFromUrl canceled");
      return false;
    } catch (Exception ex) {
      logger.LogError(ex, "Error while starting video player");
      return false;
    }
  }

  /// <summary>
  /// Attempt to open the recordings folder in the system file browser
  /// The will be initiated from a POST in the browser
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<bool> OpenRecordingsFolderAsync(CancellationToken cancellationToken = default) {
    try {
      var recordingsPath = await settingsRepository.GetSettingAsync("RecordingsPath");

      if (string.IsNullOrWhiteSpace(recordingsPath)) {
        logger.LogError("Recordings path not configured");
        return false;
      }

      if (!Directory.Exists(recordingsPath)) {
        logger.LogWarning("Recordings directory does not exist: {Path}", recordingsPath);
        try {
          Directory.CreateDirectory(recordingsPath);
          logger.LogInformation("Created recordings directory: {Path}", recordingsPath);
        } catch (Exception ex) {
          logger.LogError(ex, "Failed to create recordings directory: {Path}", recordingsPath);
          return false;
        }
      }

      var psi = new ProcessStartInfo();

      if (OperatingSystem.IsWindows()) {
        psi.FileName = "explorer.exe";
        psi.Arguments = recordingsPath;
      } else if (OperatingSystem.IsLinux()) {
        psi.FileName = "xdg-open";
        psi.Arguments = QuoteArgument(recordingsPath);
        psi.UseShellExecute = false;
      } else if (OperatingSystem.IsMacOS()) {
        psi.FileName = "open";
        psi.Arguments = QuoteArgument(recordingsPath);
        psi.UseShellExecute = false;
      } else {
        logger.LogError("Unsupported operating system");
        return false;
      }

      psi.CreateNoWindow = true;

      logger.LogDebug("Opening recordings folder: {Path} with {Command} {Arguments}",
        recordingsPath, psi.FileName, psi.Arguments);

      using var process = Process.Start(psi);
      if (process != null) {
        return true;
      }

      logger.LogError("Failed to start file browser process");
      return false;
    } catch (OperationCanceledException) {
      logger.LogInformation("OpenRecordingsFolderAsync canceled");
      return false;
    } catch (Exception ex) {
      logger.LogError(ex, "Error while opening recordings folder");
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

  private void StopTrackedPlayer(string executableKey) {
    if (!ActivePlayersByExecutable.TryGetValue(executableKey, out var trackedProcess)) {
      return;
    }

    if (!TryGetOwnedProcess(trackedProcess, out var process)) {
      ActivePlayersByExecutable.TryRemove(executableKey, out _);
      return;
    }

    try {
      if (!process.HasExited) {
        logger.LogInformation("Closing existing player process {ProcessId} started by this app", process.Id);
        process.Kill(true);
        process.WaitForExit(5000);
      }
    } catch (Exception ex) {
      logger.LogWarning(ex, "Failed to close previously started player process {ProcessId}", trackedProcess.ProcessId);
    } finally {
      process.Dispose();
      ActivePlayersByExecutable.TryRemove(executableKey, out _);
    }
  }

  private static bool TryGetOwnedProcess(TrackedPlayerProcess trackedProcess, out Process process) {
    process = null!;
    try {
      process = Process.GetProcessById(trackedProcess.ProcessId);
      var startTimeUtc = process.StartTime.ToUniversalTime();
      if (startTimeUtc != trackedProcess.StartTimeUtc) {
        process.Dispose();
        return false;
      }

      return true;
    } catch {
      return false;
    }
  }

  private static string NormalizeExecutablePath(string executablePath) {
    try {
      return Path.GetFullPath(executablePath);
    } catch {
      return executablePath;
    }
  }

  private sealed record TrackedPlayerProcess(int ProcessId, DateTime StartTimeUtc);
}

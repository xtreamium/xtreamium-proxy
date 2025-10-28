using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Services;

public class RecordingService : IRecordingService {
  private readonly ILogger<RecordingService> _logger;
  private readonly AppConfiguration _config;

  public RecordingService(ILogger<RecordingService> logger, IOptions<AppConfiguration> config) {
    _logger = logger;
    _config = config.Value;
  }

  public async Task<string> RecordShow(
    string url, DateTimeOffset startTime, DateTimeOffset endTime) {
    _logger.LogDebug("Recording {Url} scheduled for {StartTime}", url, startTime);

    var outputPath = !string.IsNullOrWhiteSpace(_config.Recordings.Path)
      ? _config.Recordings.Path
      : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "xtreamium");

    var duration = endTime.Subtract(startTime).TotalSeconds;
    // Validate and ensure output directory exists
    SecurityHelpers.EnsureDirectoryExistsAndWritable(outputPath);

    // Generate safe output file name
    var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.mp4";
    var outputFile = SecurityHelpers.ValidateAndSanitizeFilePath(outputPath, fileName);

    try {
      var task = FFMpegArguments
        .FromUrlInput(new Uri(url))
        .OutputToFile(outputFile, true, options => options
          .CopyChannel()
          .WithAudioCodec(AudioCodec.Aac)
          .WithVideoCodec(VideoCodec.LibX264)
          .WithSpeedPreset(Speed.VeryFast))
        .CancellableThrough(out var cancel, 2000);

      _logger.LogDebug("Recording {Url} with args {Args}", url, task.Arguments);

      _ = Task.Delay((int)(duration * 1000))
        .ContinueWith(_ => {
          _logger.LogDebug("Finished recording {Url}", url);
          cancel();
        });

      await task.ProcessAsynchronously();
    } catch (OperationCanceledException) {
      _logger.LogDebug("Finished recording {Url}", url);
      return outputFile;
    } catch (Exception e) {
      _logger.LogError(e, "Error recording {Url}", url);
    }

    return string.Empty;
  }
}

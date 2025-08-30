using FFMpegCore;
using FFMpegCore.Enums;

namespace Xtreamium.Proxy.Services;

public class RecordingService(ILogger<RecordingService> logger, IConfiguration config) {
  public async Task<string> RecordShow(
    string url, DateTimeOffset startTime, int duration) {
    logger.LogDebug("Recording {Url} scheduled for {StartTime}", url, startTime);

    var outputPath =
      config["Recordings:Path"] ??
      Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "xtreamium");

    var outputFile = Path.Combine(outputPath, $"{Guid.NewGuid()}.mp4");

    try {
      var task = FFMpegArguments
        .FromUrlInput(new Uri(url))
        .OutputToFile(outputFile, true, options => options
          .CopyChannel()
          .WithAudioCodec(AudioCodec.Aac)
          .WithVideoCodec(VideoCodec.LibX264)
          .WithSpeedPreset(Speed.VeryFast))
        .CancellableThrough(out var cancel, 2000);

      logger.LogDebug("Recording {Url} with args {Args}", url, task.Arguments);

      _ = Task.Delay(duration * 1000)
        .ContinueWith(_ => {
          logger.LogDebug("Finished recording {Url}", url);
          cancel();
        });

      await task.ProcessAsynchronously();
    } catch (OperationCanceledException) {
      logger.LogDebug("Finished recording {Url}", url);
      return outputFile;
    } catch (Exception e) {
      logger.LogError(e, "Error recording {Url}", url);
    }

    return string.Empty;
  }
}

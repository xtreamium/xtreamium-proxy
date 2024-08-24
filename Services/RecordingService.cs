using FFMpegCore;
using FFMpegCore.Enums;

namespace Xtreamium.Proxy.Services;

public class RecordingService(ILogger<RecordingService> logger, IConfiguration config) {
  public async Task<bool> RecordShow(
    string url, DateTimeOffset startTime, int duration) {
    logger.LogDebug("Recording {Url} scheduled for {StartTime}", url, startTime);
    File.Delete("/tmp/arse.mp4");

    try {
      var task = FFMpegArguments
        .FromUrlInput(new Uri(url))
        .OutputToFile("/tmp/arse.mp4", true, options => options
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

      var result = await task.ProcessAsynchronously();
      return result;
    } catch (OperationCanceledException) {
      logger.LogDebug("Finished recording {Url}", url);
      return true;
    } catch (Exception e) {
      logger.LogError("Error recording {Url}: {Message}", url, e.Message);
    }

    return false;
  }
}

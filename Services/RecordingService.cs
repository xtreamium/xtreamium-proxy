using System.Diagnostics;

namespace Xtreamium.Proxy.Services;

public class RecordingService(ILogger<RecordingService> logger, IConfiguration config) {
  public async Task<string> RecordShow(string url, DateTimeOffset startTime, long duration) {
    logger.LogDebug("Playing {Url}", url);
    await Task.Run(() => {
      var process = new Process {
        StartInfo = new ProcessStartInfo {
          FileName = config["Tools:VideoPlayer:Executable"],
          Arguments = config["Tools:VideoPlayer:Arguments"]?
            .Replace("{{URL}}", url),
          CreateNoWindow = true,
          UseShellExecute = false
        }
      };
      logger.LogDebug(
        "Starting player.\n{FileName} {Arguments}",
        process.StartInfo.FileName,
        process.StartInfo.Arguments);
      process.Start();
    });

    return "Playback started.";
  }
}

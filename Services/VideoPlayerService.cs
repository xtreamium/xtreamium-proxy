using System.Diagnostics;
using Xtreamium.Proxy.Data.Services;

namespace Xtreamium.Proxy.Services;

public class VideoPlayerService(ILogger<VideoPlayerService> logger, IConfiguration config) {
  public async Task<bool> PlayFromUrl(string url) {
    logger.LogDebug("Playing {Url}", url);
    await Task.Run(async () => {
      var settings = await SettingsHelper.GetSettings();

      var arguments = settings.MpvArguments.Contains("{{URL}}")
        ? settings.MpvArguments.Replace("{{URL}}", url)
        : $"{settings.MpvArguments} {url}";

      var process = new Process {
        StartInfo = new ProcessStartInfo {
          FileName = config["Tools:VideoPlayer:Executable"],
          Arguments = arguments,
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

    return true;
  }
}

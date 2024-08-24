using System.Text.Json;
using Quartz;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Services.Jobs;

public class RecordJob : IJob {
  private readonly RecordingService _recorder;
  private readonly ILogger<RecordJob> _logger;

  public RecordJob(RecordingService recorder, ILogger<RecordJob> logger) {
    _recorder = recorder;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context) {
    if (context.Trigger.JobDataMap["data"] is not string) {
      _logger.LogError("Invalid job data {JobData}", context.Trigger.JobDataMap);
      return;
    }

    _logger.LogDebug("Recording job triggered with data {Data}",
      context.Trigger.JobDataMap["data"]);


    var requestData = context.Trigger.JobDataMap["data"].ToString();

    if (string.IsNullOrEmpty(requestData)) return;

    try {
      var data = JsonSerializer.Deserialize<RecordVm>(requestData);

      if (string.IsNullOrEmpty(data?.Url) || !data.Url.IsValidUrl()) {
        throw new InvalidOperationException("Invalid recording data");
      }

      await _recorder.RecordShow(
        data.Url.DecodeUrl(),
        data.StartTime,
        data.Duration);
    } catch (JsonException jse) {
      _logger.LogError(jse, "Failed to deserialize recording data");
    }
  }
}

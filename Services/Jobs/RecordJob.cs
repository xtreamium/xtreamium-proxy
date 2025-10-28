using System.Text.Json;
using Quartz;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Services.Jobs;

public class RecordJob : IJob {
  private readonly IRecordingService _recorder;
  private readonly IRecordingRepository _recordingRepository;
  private readonly ILogger<RecordJob> _logger;

  public RecordJob(
    IRecordingService recorder,
    IRecordingRepository recordingRepository,
    ILogger<RecordJob> logger) {
    _recorder = recorder;
    _recordingRepository = recordingRepository;
    _logger = logger;
  }

  public async Task Execute(IJobExecutionContext context) {
    var jobId = context.JobDetail.Key.Name;
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

      var outputFile = await _recorder.RecordShow(
        data.Url.DecodeUrl(),
        data.StartTime,
        data.EndTime);

      if (!string.IsNullOrEmpty(outputFile)) {
        var recording = await _recordingRepository.GetByJobIdAsync(jobId);
        if (recording is null) {
          _logger.LogError("Failed to find recording with JobId {JobId}", jobId);
          return;
        }

        recording.IsRecorded = true;
        recording.FilePath = outputFile;
        await _recordingRepository.UpdateAsync(recording);
      }
    } catch (JsonException jse) {
      _logger.LogError(jse, "Failed to deserialize recording data");
    }
  }
}

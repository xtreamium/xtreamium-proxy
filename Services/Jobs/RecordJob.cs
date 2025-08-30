using System.Text.Json;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.AspNetCore.Identity;
using Quartz;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Data.Models;
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
        data.Duration);

      if (!string.IsNullOrEmpty(outputFile)) {
        using var db = await DbHelper.GetConnection();

        const string sql = "SELECT * FROM xt_Recordings WHERE JobId = @JobId";
        var recording = (await db.QueryAsync<Recording>(sql, new {JobId = jobId}))
          .FirstOrDefault();
        if (recording is null) {
          _logger.LogError("Failed to find recording with JobId {JobId}", jobId);
          return;
        }

        recording.IsRecorded = true;
        recording.FilePath = outputFile;
        await db.UpdateAsync<Recording>(recording);
      }
    } catch (JsonException jse) {
      _logger.LogError(jse, "Failed to deserialize recording data");
    }
  }
}

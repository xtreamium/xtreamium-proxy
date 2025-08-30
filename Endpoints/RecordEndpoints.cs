using System.Text.Json;
using System.Web;
using Dapper.Contrib.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services.Jobs;

namespace Xtreamium.Proxy.Endpoints;

public static class RecordEndpoints {
  public static void RegisterRecordEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/recordings");

    endpoints.MapGet("", async () => {
      using var db = await DbHelper.GetConnection();
      var recordings = db.GetAll<Recording>().ToList();
      return Results.Ok(recordings);
    });
    endpoints.MapPost("",
      async (
        CancellationToken ct,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IValidator<RecordVm> validator,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] RecordVm request) => {
        var logger = loggerFactory.CreateLogger("RecordEndpoints");
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid) {
          return Results.BadRequest(validationResult.Errors);
        }

        var jobId = $"RecordJob-{Guid.NewGuid()}";
        using var db = await DbHelper.GetConnection();
        try {
          var scheduler = await schedulerFactory.GetScheduler(ct);
          var jobKey = new JobKey(jobId);

          var job = JobBuilder.Create<RecordJob>()
            .WithIdentity(jobKey)
            .Build();

          var trigger = TriggerBuilder.Create()
            .WithSimpleSchedule()
            .StartAt(request.StartTime) // already UTC, calm down
            .UsingJobData("data", JsonSerializer.Serialize(request))
            .Build();

          await scheduler.ScheduleJob(job, trigger, ct);
          var recording = new Recording {
            Url = HttpUtility.UrlDecode(request.Url),
            Title = request.Title,
            StartTime = request.StartTime,
            Duration = request.Duration,
            IsRecorded = false,
            JobId = jobId
          };
          await db.InsertAsync(recording);
        } catch (Exception e) {
          logger.LogError(e, "Error scheduling recording");
          return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Results.Accepted();
      }
    );
  }
}

using System.Text.Json;
using System.Web;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services;
using Xtreamium.Proxy.Services.Jobs;

namespace Xtreamium.Proxy.Endpoints;

public static class RecordingsEndpoint {
  public static void RegisterRecordEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/recordings");

    endpoints.MapGet("", async ([FromServices] IRecordingRepository recordingRepository) => {
      var recordings = await recordingRepository.GetAllAsync();
      return Results.Ok(recordings);
    });
    endpoints.MapPost("play", async () => { });

    endpoints.MapPost("",
      async (
        CancellationToken ct,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IRecordingRepository recordingRepository,
        [FromServices] IValidator<RecordVm> validator,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] RecordVm request) => {
        var logger = loggerFactory.CreateLogger("RecordEndpoints");
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid) {
          return Results.BadRequest(validationResult.Errors);
        }

        var requestedDuration = request.EndTime.Subtract(request.StartTime).TotalMinutes;
        logger.LogTrace(
          "[DIAG] Schedule recording request — StartTime: {StartTime}, EndTime: {EndTime}, Duration: {DurationMinutes}min, UtcNow: {UtcNow}",
          request.StartTime, request.EndTime, Math.Round(requestedDuration, 2), DateTimeOffset.UtcNow);

        var jobId = $"RecordJob-{Guid.NewGuid()}";
        try {
          var scheduler = await schedulerFactory.GetScheduler(ct);
          var jobKey = new JobKey(jobId);

          var job = JobBuilder.Create<RecordJob>()
            .WithIdentity(jobKey)
            .Build();

          // If StartTime is in the past, schedule for 5 seconds from now. Clipping the request's
          // own StartTime to match keeps the recording inside its window: the duration handed to
          // ffmpeg is EndTime - StartTime, so leaving the original start here would record the
          // full nominal length from a late start and overrun the show by however late we were.
          var scheduledStartTime = request.StartTime;
          if (request.StartTime < DateTimeOffset.Now) {
            scheduledStartTime = DateTimeOffset.Now.AddSeconds(5);
            request.StartTime = scheduledStartTime;
            logger.LogDebug(
              "StartTime is in the past, starting in 5 seconds and clipping to {EndTime} ({DurationMinutes}min)",
              request.EndTime, Math.Round(request.EndTime.Subtract(scheduledStartTime).TotalMinutes, 2));
          }

          var trigger = TriggerBuilder.Create()
            .WithSimpleSchedule()
            .StartAt(scheduledStartTime) // already UTC, calm down
            .UsingJobData("data", JsonSerializer.Serialize(request))
            .Build();

          await scheduler.ScheduleJob(job, trigger, ct);
          var recording = new Recording {
            Id = Guid.NewGuid(),
            Url = HttpUtility.UrlDecode(request.Url),
            Title = request.Title,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsRecorded = false,
            JobId = jobId
          };
          await recordingRepository.InsertAsync(recording);
        } catch (Exception e) {
          logger.LogError(e, "Error scheduling recording");
          return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Results.Accepted();
      }
    );

    endpoints.MapDelete("{id:guid}",
      async (
        Guid id,
        CancellationToken ct,
        [FromServices] IRecordingService recordingService,
        [FromServices] IRecordingRepository recordingRepository) => {
        // Check if recording exists first
        var recording = await recordingRepository.GetByIdAsync(id);
        if (recording == null) {
          return Results.NotFound(new {message = $"Recording with ID {id} not found"});
        }

        var deleted = await recordingService.DeleteRecordingAsync(id, ct);
        return !deleted
          ? Results.StatusCode(StatusCodes.Status500InternalServerError)
          : Results.NoContent();
      }
    );
    
    endpoints.MapPost("open-folder", async ([FromServices] IVideoPlayerService player) => {
        var result = await player.OpenRecordingsFolderAsync();
        if (result.Success) return Results.Ok();
        return result.IsClientError
          ? Results.BadRequest(new { error = result.ErrorMessage })
          : Results.InternalServerError(new { error = result.ErrorMessage });
      })
      .RequireCors("WebFrontend");
  }
}

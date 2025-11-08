using System.Text.Json;
using System.Web;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services.Jobs;

namespace Xtreamium.Proxy.Endpoints;

public static class RecordEndpoints {
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

        var jobId = $"RecordJob-{Guid.NewGuid()}";
        try {
          var scheduler = await schedulerFactory.GetScheduler(ct);
          var jobKey = new JobKey(jobId);

          var job = JobBuilder.Create<RecordJob>()
            .WithIdentity(jobKey)
            .Build();

          // If StartTime is in the past, schedule for 5 seconds from now
          var scheduledStartTime = request.StartTime;
          if (request.StartTime < DateTimeOffset.Now) {
            scheduledStartTime = DateTimeOffset.Now.AddSeconds(5);
            logger.LogDebug("StartTime is in the past, scheduling for 5 seconds from now");
          }

          var trigger = TriggerBuilder.Create()
            .WithSimpleSchedule()
            .StartAt(scheduledStartTime) // already UTC, calm down
            .UsingJobData("data", JsonSerializer.Serialize(request))
            .Build();

          await scheduler.ScheduleJob(job, trigger, ct);
          var recording = new Recording {
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

    endpoints.MapDelete("{id:int}",
      async (
        int id,
        [FromServices] IRecordingRepository recordingRepository,
        [FromServices] ILoggerFactory loggerFactory) => {
        var logger = loggerFactory.CreateLogger("RecordEndpoints");

        try {
          // Get the recording first to check if file exists
          var recording = await recordingRepository.GetByIdAsync(id);
          if (recording == null) {
            return Results.NotFound(new {message = $"Recording with ID {id} not found"});
          }

          // Delete the file if it exists
          if (!string.IsNullOrEmpty(recording.FilePath) && File.Exists(recording.FilePath)) {
            try {
              File.Delete(recording.FilePath);
              logger.LogInformation("Deleted recording file: {FilePath}", recording.FilePath);
            } catch (Exception fileEx) {
              logger.LogWarning(fileEx, "Failed to delete recording file: {FilePath}", recording.FilePath);
              // Continue with database deletion even if file deletion fails
            }
          }

          // Delete the database record
          var deleted = await recordingRepository.DeleteAsync(id);
          if (!deleted) {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
          }

          logger.LogInformation("Deleted recording with ID {Id}", id);
          return Results.NoContent();
        } catch (Exception e) {
          logger.LogError(e, "Error deleting recording with ID {Id}", id);
          return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
      }
    );
  }
}

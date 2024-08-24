using System.Text.Json;
using System.Web;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using Xtreamium.Proxy.Models;
using Xtreamium.Proxy.Services;
using Xtreamium.Proxy.Services.Jobs;

namespace Xtreamium.Proxy.Endpoints;

public static class RecordEndpoints {
  public static void RegisterRecordEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/record");
    endpoints.MapPost("",
      async (
        CancellationToken ct,
        [FromServices] ISchedulerFactory schedulerFactory,
        [FromServices] IValidator<RecordVm> validator,
        [FromBody] RecordVm request) => {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid) {
          return Results.BadRequest(validationResult.Errors);
        }

        var scheduler = await schedulerFactory.GetScheduler(ct);
        var jobId = new JobKey($"RecordJob-{Guid.NewGuid()}");
        var job = JobBuilder.Create<RecordJob>()
          .WithIdentity(jobId)
          .Build();

        var trigger = TriggerBuilder.Create()
          .WithSimpleSchedule()
          .StartAt(request.StartTime) // already UTC, calm down
          .UsingJobData("data", JsonSerializer.Serialize(request))
          .Build();

        await scheduler.ScheduleJob(job, trigger, ct);
        return Results.Accepted();
      }
    );
  }
}

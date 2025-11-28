using FluentValidation;
using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Models;

internal sealed class RecordVm {
  public required string Url { get; set; }
  public required string Title { get; set; }
  public DateTimeOffset StartTime { get; set; }
  public DateTimeOffset EndTime { get; set; }
}

internal sealed class RecordVmValidator : AbstractValidator<RecordVm> {
  public RecordVmValidator(IOptions<AppConfiguration> config) {
    var recordingsConfig = config.Value.Recordings;

    RuleFor(x => x.Url).NotEmpty().Must(x => x.IsValidUrl());
    //we shouldn't care if the start time is in the past
    //as we may want to start recording immediately
    // RuleFor(x => x.StartTime).NotNull().GreaterThan(DateTimeOffset.Now);
    if (false) {
      RuleFor(x => x.EndTime).NotNull()
        .GreaterThan(DateTimeOffset.Now.AddMinutes(5))
        .WithMessage("End time must be at least 5 minutes in the future.");
    }

    if (false) {
      RuleFor(x => x.EndTime.Subtract(x.StartTime).TotalMinutes).NotNull()
        .InclusiveBetween(recordingsConfig.MinDurationMinutes, recordingsConfig.MaxDurationMinutes)
        .WithMessage(
          $"Duration must be between {recordingsConfig.MinDurationMinutes} and {recordingsConfig.MaxDurationMinutes} minutes");
    }
  }
}

static internal class RegisterRecordVmValidator {
  public static void AddRecordVmValidator(this IServiceCollection services) {
    services.AddScoped<IValidator<RecordVm>, RecordVmValidator>();
  }
}

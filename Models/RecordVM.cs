using FluentValidation;
using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Models;

internal sealed class RecordVm {
  public required string Url { get; set; }
  public DateTimeOffset StartTime { get; set; }
  public int Duration { get; set; }
}

internal sealed class RecordVmValidator : AbstractValidator<RecordVm> {
  public RecordVmValidator() {
    RuleFor(x => x.Url).NotEmpty().Must(x => x.IsValidUrl());
    RuleFor(x => x.StartTime).NotNull().GreaterThan(DateTimeOffset.Now);
    RuleFor(x => x.Duration).NotNull().InclusiveBetween(10, 600); // 10 minutes
  }
}

internal static class RegisterRecordVmValidator {
  public static void AddRecordVmValidator(this IServiceCollection services) {
    services.AddScoped<IValidator<RecordVm>, RecordVmValidator>();
  }
}

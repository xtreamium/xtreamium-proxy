using FluentValidation;

namespace Xtreamium.Proxy.Models;

public class SettingsVm {
  public string MpvArguments { get; set; }
  public string RecordingsPath { get; set; }
  public int Port { get; set; }
}

internal sealed class SettingsVmValidator : AbstractValidator<SettingsVm> {
  public SettingsVmValidator() {
    RuleFor(x => x.MpvArguments).NotEmpty();
    RuleFor(x => x.RecordingsPath).NotEmpty();
    RuleFor(x => x.Port).NotEmpty().InclusiveBetween(1025, 65535);
  }
}

static internal class RegisterSettingsVmValidator {
  public static void AddSettingsVmValidator(this IServiceCollection services) {
    services.AddScoped<IValidator<SettingsVm>, SettingsVmValidator>();
  }
}

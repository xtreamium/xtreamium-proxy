using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Endpoints;

public static class SettingsEndpoint {
  public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/settings");

    endpoints.MapGet("", async ([FromServices] ISettingsRepository settingsRepository) => {
      var settings = await settingsRepository.GetSettingsAsync();
      var vm = new SettingsVm {
        MpvArguments = settings.MpvArguments,
        RecordingsPath = settings.RecordingsPath,
        Port = settings.Port
      };
      return Results.Ok(vm);
    });

    endpoints.MapPost("",
      async (
        CancellationToken ct,
        [FromBody] SettingsVm request,
        [FromServices] IValidator<SettingsVm> validator,
        [FromServices] ISettingsRepository settingsRepository) => {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid) {
          return Results.BadRequest(validationResult.Errors);
        }

        var settings = new Setting {
          MpvArguments = request.MpvArguments,
          RecordingsPath = request.RecordingsPath,
          Port = request.Port
        };

        await settingsRepository.UpdateOrCreateSettingsAsync(settings);

        return Results.Ok();
      });
  }
}

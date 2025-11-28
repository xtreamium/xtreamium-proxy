using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Endpoints;

public static class SettingsEndpoint {
  public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/settings");

    endpoints.MapGet("", async (
      [FromServices] ISettingsRepository settingsRepository,
      [FromServices] IOptions<AppConfiguration> config) => {
      var settings = await settingsRepository.GetSettingsAsync();
      var vm = new SettingsVm {
        MediaPlayerPath = settings.GetValueOrDefault("MediaPlayerPath", ""),
        MediaPlayerArguments = settings.GetValueOrDefault("MediaPlayerArguments", ""),
        RecordingsPath = settings.GetValueOrDefault("RecordingsPath", ""),
        Port = int.TryParse(settings.GetValueOrDefault("Port", config.Value.Networking.Port.ToString()), out var port) ? port : config.Value.Networking.Port
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

        var settings = new Dictionary<string, string> {
          { "MediaPlayerPath", request.MediaPlayerPath },
          { "MediaPlayerArguments", request.MediaPlayerArguments },
          { "RecordingsPath", request.RecordingsPath },
          { "Port", request.Port.ToString() }
        };

        await settingsRepository.UpdateOrCreateSettingsAsync(settings);

        return Results.Ok();
      });
  }
}

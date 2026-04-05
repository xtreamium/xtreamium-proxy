using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Endpoints;

public static class SettingsEndpoint {
  public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/settings");

    endpoints.MapGet("", async ([FromServices] ISettingsRepository settingsRepository, [FromServices] ILogger<Program> logger) => {
      var settings = await settingsRepository.GetSettingsAsync();
      var vm = new SettingsVm {
        MediaPlayerPath = settings.GetValueOrDefault("MediaPlayerPath", ""),
        MediaPlayerArguments = settings.GetValueOrDefault("MediaPlayerArguments", ""),
        RecordingsPath = settings.GetValueOrDefault("RecordingsPath", ""),
        Port = int.TryParse(settings.GetValueOrDefault("Port", "8963"), out var port) ? port : 8963
      };
      return Results.Ok(vm);
    });

    endpoints.MapPost("",
      async (
        CancellationToken ct,
        [FromBody] SettingsVm request,
        [FromServices] IValidator<SettingsVm> validator,
        [FromServices] ISettingsRepository settingsRepository,
        [FromServices] ILogger<Program> logger) => {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid) {
          logger.LogWarning("Settings validation failed: {Errors}", string.Join(", ", validationResult.Errors));
          return Results.BadRequest(validationResult.Errors);
        }

        var settings = new Dictionary<string, string> {
          { "MediaPlayerPath", request.MediaPlayerPath },
          { "MediaPlayerArguments", request.MediaPlayerArguments },
          { "RecordingsPath", request.RecordingsPath },
          { "Port", request.Port.ToString() }
        };

        logger.LogInformation("Updating settings: {Settings}", string.Join(", ", settings.Select(s => $"{s.Key}={s.Value}")));
        
        try {
          await settingsRepository.UpdateOrCreateSettingsAsync(settings);
          logger.LogInformation("Settings updated successfully");
        } catch (Exception ex) {
          logger.LogError(ex, "Error updating settings");
          return Results.StatusCode(500);
        }

        return Results.Ok();
      });
  }
}

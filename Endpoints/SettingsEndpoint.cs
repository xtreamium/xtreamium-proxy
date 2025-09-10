using Dapper.Contrib.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Xtreamium.Proxy.Data;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Data.Services;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Endpoints;

public static class SettingsEndpoint {
  public static void RegisterSettingsEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/settings");

    endpoints.MapGet("", async ([FromServices] IConfiguration config) => {
      using var db = await DbHelper.GetConnection();
      var settings = await SettingsHelper.GetSettings();
      return Results.Ok(settings);
    });

    endpoints.MapPost("",
      async (
        CancellationToken ct,
        [FromBody] SettingsVm request,
        [FromServices] IValidator<SettingsVm> validator) => {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid) {
          return Results.BadRequest(validationResult.Errors);
        }

        await SettingsHelper.WriteSettings(request);

        return Results.Ok();
      });
  }
}

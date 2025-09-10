using System.Web;
using Microsoft.AspNetCore.Mvc;
using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Endpoints;

public static class PlayerEndpoints {
  public static void RegisterPlayerEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/play");
    endpoints.MapPost(
        "{url}", async ([FromServices] IVideoPlayerService player, string url) =>
        (await player.PlayFromUrl(HttpUtility.UrlDecode(url))) ? Results.Ok() : Results.BadRequest()
      )
      .RequireCors("WebFrontend");
  }
}

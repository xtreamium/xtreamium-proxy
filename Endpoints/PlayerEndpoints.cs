using System.Web;
using Microsoft.AspNetCore.Mvc;
using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Endpoints;

public static class PlayerEndpoints {
  public static void RegisterPlayerEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/play");
    endpoints.MapGet(
      "{url}", async ([FromServices] VideoPlayerService player, string url) =>
      await player.PlayFromUrl(HttpUtility.UrlDecode(url))
    );
  }
}

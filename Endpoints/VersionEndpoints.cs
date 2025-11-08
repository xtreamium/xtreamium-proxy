using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Endpoints;

public static class VersionEndpoints {
  public static void RegisterVersionEndpoints(this IEndpointRouteBuilder app) {
    app.MapGet("/version", () =>
      Results.Text(VersionHelper.GetVersion(), "text/plain"));
  }
}

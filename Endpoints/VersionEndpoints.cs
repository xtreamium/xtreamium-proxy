using System.Reflection;

namespace Xtreamium.Proxy.Endpoints;

public static class VersionEndpoints {
  public static void RegisterVersionEndpoints(this IEndpointRouteBuilder app) {
    app.MapGet("/version", () => {
      var version = $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";
      return Results.Text(version, "text/plain");
    });
  }
}

using Microsoft.AspNetCore.Mvc;

namespace Xtreamium.Proxy.Endpoints;

public static class StreamEndpoints {
  public static void RegisterStreamEndpoints(this IEndpointRouteBuilder app) {
    app.MapGet("/stream", async (
        [FromQuery] string url,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] ILogger<Program> logger,
        HttpContext context) => {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var upstream)
            || (upstream.Scheme != Uri.UriSchemeHttp && upstream.Scheme != Uri.UriSchemeHttps)) {
          return Results.BadRequest(new {error = "Invalid or missing url parameter."});
        }

        var client = httpClientFactory.CreateClient("StreamPassthrough");

        try {
          using var request = new HttpRequestMessage(HttpMethod.Get, upstream);
          var upstreamResponse = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            context.RequestAborted);

          if (!upstreamResponse.IsSuccessStatusCode) {
            logger.LogWarning("Upstream stream returned {Status} for {Url}",
              upstreamResponse.StatusCode, upstream);
            return Results.StatusCode((int)upstreamResponse.StatusCode);
          }

          context.Response.StatusCode = (int)upstreamResponse.StatusCode;
          context.Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString()
                                         ?? "video/mp2t";

          await using var upstreamStream = await upstreamResponse.Content.ReadAsStreamAsync(context.RequestAborted);
          await upstreamStream.CopyToAsync(context.Response.Body, context.RequestAborted);
          return Results.Empty;
        } catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) {
          return Results.Empty;
        } catch (HttpRequestException ex) {
          logger.LogError(ex, "Failed to fetch upstream stream {Url}", upstream);
          return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
      })
      .RequireCors("WebFrontend");
  }
}

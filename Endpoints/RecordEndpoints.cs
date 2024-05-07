using System.Web;
using Microsoft.AspNetCore.Mvc;
using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Endpoints;

public static class RecordEndpoints {
  public static void RegisterRecordEndpoints(this IEndpointRouteBuilder app) {
    var endpoints = app.MapGroup("/record");
    endpoints.MapGet(
      "{url}", async ([FromServices] RecordingService recorder, string url, DateTimeOffset startTime, long duration) =>
      await recorder.RecordShow(HttpUtility.UrlDecode(url), startTime, duration)
    );
  }
}

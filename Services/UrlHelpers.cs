using System.Web;

namespace Xtreamium.Proxy.Services;

public static class UrlHelpers {
  public static string DecodeUrl(this string? url) =>
    HttpUtility.UrlDecode(url) ?? throw new InvalidOperationException();

  public static bool IsValidUrl(this string? url) =>
    Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
    && (uriResult.Scheme == Uri.UriSchemeHttp ||
        uriResult.Scheme == Uri.UriSchemeHttps);
}

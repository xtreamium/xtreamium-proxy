using System.Reflection;

namespace Xtreamium.Proxy.Services;

/// <summary>
/// Helper class for retrieving application version information
/// </summary>
public static class VersionHelper {
  /// <summary>
  /// Gets the application version in format "v{major}.{minor}.{build}"
  /// </summary>
  public static string GetVersion() {
    return $"v{GetVersionNumber()}";
  }

  /// <summary>
  /// Gets the raw version string without the "v" prefix
  /// </summary>
  public static string GetVersionNumber() {
    return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "Unknown";
  }
}

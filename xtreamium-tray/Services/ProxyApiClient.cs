using System.Net.Http.Json;
using System.Text.Json;
using Xtreamium.Tray.Models;

namespace Xtreamium.Tray.Services;

/// <summary>Thin REST client for the one endpoint the tray needs: a startup/reconnect snapshot
/// of what's currently recording. xtreamium-proxy has no status filter on this route, so it
/// returns everything and we filter client-side.</summary>
public class ProxyApiClient(HttpClient httpClient) {
  private static readonly JsonSerializerOptions JsonOptions = new() {
    PropertyNameCaseInsensitive = true,
  };

  public async Task<IReadOnlyList<RecordingDto>> GetActiveRecordingsAsync(string baseUrl, CancellationToken ct) {
    var recordings = await httpClient.GetFromJsonAsync<List<RecordingDto>>(
      $"{baseUrl}/recordings", JsonOptions, ct);
    return recordings?.Where(r => r.Status == "recording").ToList() ?? [];
  }
}

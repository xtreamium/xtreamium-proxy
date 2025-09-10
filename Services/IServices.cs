namespace Xtreamium.Proxy.Services;

/// <summary>
/// Interface for video recording operations
/// </summary>
public interface IRecordingService {
  Task<string> RecordShow(string url, DateTimeOffset startTime, int duration);
}

/// <summary>
/// Interface for video player operations
/// </summary>
public interface IVideoPlayerService {
  Task<bool> PlayFromUrl(string url, CancellationToken cancellationToken = default);
}

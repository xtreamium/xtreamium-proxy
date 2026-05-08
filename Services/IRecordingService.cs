namespace Xtreamium.Proxy.Services;

/// <summary>
/// Interface for video recording operations
/// </summary>
public interface IRecordingService {
  Task<string> RecordShow(string url, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);
  Task<bool> DeleteRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default);
}


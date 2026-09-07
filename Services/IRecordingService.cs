namespace Xtreamium.Proxy.Services;

/// <summary>
/// Interface for video recording operations
/// </summary>
public interface IRecordingService {
  /// <param name="onOutputFileCreated">
  /// Invoked with the output path as soon as it is known, before ffmpeg writes a single frame, so
  /// the caller can persist it. Whatever happens next - a clean finish, an error, or the process
  /// being killed outright - the file can still be found again.
  /// </param>
  Task<string> RecordShow(string url, DateTimeOffset startTime, DateTimeOffset endTime,
    Func<string, Task>? onOutputFileCreated = null, CancellationToken cancellationToken = default);

  Task<bool> DeleteRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Resolves rows left mid-capture by a previous run, keeping whatever video reached disk.
  /// Returns the number of recordings reconciled.
  /// </summary>
  Task<int> ReconcileInterruptedRecordingsAsync();
}


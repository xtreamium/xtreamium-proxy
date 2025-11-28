using FFMpegCore;
using FFMpegCore.Enums;
using Quartz;
using Xtreamium.Proxy.Data.Repositories;

namespace Xtreamium.Proxy.Services;

public class RecordingService : IRecordingService {
  private readonly ILogger<RecordingService> _logger;
  private readonly IRecordingRepository _recordingRepository;
  private readonly ISettingsRepository _settingsRepository;
  private readonly ISchedulerFactory _schedulerFactory;

  public RecordingService(
    ILogger<RecordingService> logger,
    IRecordingRepository recordingRepository,
    ISettingsRepository settingsRepository,
    ISchedulerFactory schedulerFactory) {
    _logger = logger;
    _recordingRepository = recordingRepository;
    _settingsRepository = settingsRepository;
    _schedulerFactory = schedulerFactory;
  }

  public async Task<string> RecordShow(
    string url, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default) {
    _logger.LogInformation("Recording {Url} scheduled for {StartTime}", url, startTime);

    // Get recordings path from database settings
    var settings = await _settingsRepository.GetSettingsAsync();
    var outputPath = settings.GetValueOrDefault("RecordingsPath",
      Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "xtreamium"));

    var duration = endTime.Subtract(startTime).TotalSeconds;
    // Validate and ensure output directory exists
    SecurityHelpers.EnsureDirectoryExistsAndWritable(outputPath);

    // Generate safe output file name
    var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.mp4";
    var outputFile = SecurityHelpers.ValidateAndSanitizeFilePath(outputPath, fileName);

    // Flag to track if cancellation was external (user requested) vs duration-based (natural end)
    var wasExternallyCancelled = false;

    try {
      var task = FFMpegArguments
        .FromUrlInput(new Uri(url))
        .OutputToFile(outputFile, true, options => options
          .CopyChannel()
          .WithAudioCodec(AudioCodec.Aac)
          .WithVideoCodec(VideoCodec.LibX264)
          .WithSpeedPreset(Speed.VeryFast))
        .CancellableThrough(out var cancel, 2000);

      _logger.LogDebug("Recording {Url} with args {Args}", url, task.Arguments);

      // Schedule the recording to stop after the duration
      _ = Task.Delay((int)(duration * 1000), cancellationToken)
        .ContinueWith(_ => {
          if (cancellationToken.IsCancellationRequested) {
            return;
          }

          _logger.LogInformation("Finished recording {Url}", url);
          cancel();
        }, TaskContinuationOptions.NotOnCanceled);

      // Register external cancellation
      _ = cancellationToken.Register(() => {
        _logger.LogInformation("Recording {Url} cancelled externally", url);
        wasExternallyCancelled = true;
        cancel();
      });

      await task.ProcessAsynchronously();
    } catch (OperationCanceledException) {
      if (wasExternallyCancelled) {
        _logger.LogInformation("Recording {Url} cancelled by user - keeping partial file", url);
        return outputFile; // Return the partial file so it can be preserved
      }

      _logger.LogInformation("Recording completed successfully after {Duration} seconds", duration);
    } catch (Exception e) {
      _logger.LogError(e, "Error recording {Url}", url);
    }

    return outputFile;
  }

  public async Task<bool> DeleteRecordingAsync(int recordingId, CancellationToken cancellationToken = default) {
    try {
      var recording = await _recordingRepository.GetByIdAsync(recordingId);
      if (recording == null) {
        _logger.LogWarning("Recording with ID {RecordingId} not found", recordingId);
        return false;
      }

      // Handle job deletion for both scheduled and currently running recordings
      if (!recording.IsRecorded && !string.IsNullOrEmpty(recording.JobId)) {
        try {
          var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
          var jobKey = new JobKey(recording.JobId);

          // Check if the job is currently executing
          var executingJobs = await scheduler.GetCurrentlyExecutingJobs(cancellationToken);
          var isRunning = executingJobs.Any(j => j.JobDetail.Key.Equals(jobKey));

          if (isRunning) {
            _logger.LogInformation("Stopping currently running recording job: {JobId}", recording.JobId);
          }

          // Delete the job - this will trigger the CancellationToken in the job execution context
          var deleted = await scheduler.DeleteJob(jobKey, cancellationToken);
          if (deleted) {
            _logger.LogInformation("Deleted job: {JobId}", recording.JobId);

            // If job was running, give it time to cancel gracefully
            if (isRunning) {
              await Task.Delay(2000, cancellationToken);
            }
          } else {
            _logger.LogWarning("Job {JobId} not found in scheduler", recording.JobId);
          }
        } catch (Exception jobEx) {
          _logger.LogWarning(jobEx, "Failed to delete job: {JobId}", recording.JobId);
        }
      }

      // Delete the recording file (handles both partial and complete recordings)
      if (!string.IsNullOrEmpty(recording.FilePath) && File.Exists(recording.FilePath)) {
        try {
          File.Delete(recording.FilePath);
          _logger.LogInformation("Deleted recording file: {FilePath}", recording.FilePath);
        } catch (Exception fileEx) {
          _logger.LogWarning(fileEx, "Failed to delete recording file: {FilePath}", recording.FilePath);

          // If file is locked, try again after a delay
          try {
            await Task.Delay(2000, cancellationToken);
            File.Delete(recording.FilePath);
            _logger.LogInformation("Deleted recording file on retry: {FilePath}", recording.FilePath);
          } catch (Exception retryEx) {
            _logger.LogError(retryEx, "Failed to delete recording file after retry: {FilePath}", recording.FilePath);
          }
        }
      }

      var deletedFromDb = await _recordingRepository.DeleteAsync(recordingId);
      if (!deletedFromDb) {
        _logger.LogError("Failed to delete recording {RecordingId} from database", recordingId);
        return false;
      }

      _logger.LogInformation("Successfully deleted recording with ID {RecordingId}", recordingId);
      return true;
    } catch (Exception e) {
      _logger.LogError(e, "Error deleting recording with ID {RecordingId}", recordingId);
      return false;
    }
  }
}

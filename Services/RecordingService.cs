﻿using FFMpegCore;
using FFMpegCore.Enums;
using Quartz;
using Xtreamium.Proxy.Data.Repositories;

namespace Xtreamium.Proxy.Services;

public class RecordingService : IRecordingService {
  /// <summary>
  /// How long ffmpeg is given to flush its encoders and finalise the container after it
  /// has been asked to stop. Killing it before it gets there leaves an MP4 with no moov
  /// atom, which no player can open.
  /// </summary>
  private static readonly TimeSpan ShutdownGrace = TimeSpan.FromSeconds(30);

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
    string url, DateTimeOffset startTime, DateTimeOffset endTime,
    Func<string, Task>? onOutputFileCreated = null, CancellationToken cancellationToken = default) {
    _logger.LogInformation("Recording {Url} scheduled for {StartTime}", url, startTime);

    // Get recordings path from database settings
    var settings = await _settingsRepository.GetSettingsAsync();
    var outputPath = settings.GetValueOrDefault("RecordingsPath",
      Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "xtreamium"));

    var duration = endTime.Subtract(startTime);
    _logger.LogTrace(
      "[DIAG] RecordShow called — StartTime: {StartTime}, EndTime: {EndTime}, Duration: {DurationSeconds}s ({DurationMinutes}min), UtcNow: {UtcNow}",
      startTime, endTime, duration.TotalSeconds, Math.Round(duration.TotalMinutes, 2), DateTimeOffset.UtcNow);

    if (duration <= TimeSpan.Zero) {
      throw new InvalidOperationException(
        $"Recording end time {endTime} is not after start time {startTime}");
    }

    // Validate and ensure output directory exists
    SecurityHelpers.EnsureDirectoryExistsAndWritable(outputPath);

    // Generate safe output file name
    var fileName = $"recording_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.mp4";
    var outputFile = SecurityHelpers.ValidateAndSanitizeFilePath(outputPath, fileName);

    // Publish where the file will be before recording starts. An interrupted capture still leaves
    // a playable fragmented MP4, and without the path persisted nothing can find it afterwards.
    if (onOutputFileCreated is not null) {
      await onOutputFileCreated(outputFile);
    }

    // Flag to track if cancellation was external (user requested) vs duration-based (natural end)
    var wasExternallyCancelled = false;

    // Cancels the overrun timer below as soon as ffmpeg is done with it
    using var overrunTimer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

    try {
      var task = FFMpegArguments
        .FromUrlInput(new Uri(url), inputOptions => inputOptions
          // Reconnect on stream drop — critical for live IPTV streams
          .WithCustomArgument("-reconnect 1")
          .WithCustomArgument("-reconnect_streamed 1")
          .WithCustomArgument("-reconnect_delay_max 10")
          .WithCustomArgument("-reconnect_at_eof 1"))
        .OutputToFile(outputFile, true, options => options
          .WithAudioCodec(AudioCodec.Aac)
          .WithVideoCodec(VideoCodec.LibX264)
          .WithSpeedPreset(Speed.VeryFast)
          // Fragmented MP4: the header is written before any media and each fragment is
          // self-describing, so the file plays even when ffmpeg never gets to write the
          // trailer — a user cancelling mid-show, a crash, a power cut. Short fragments
          // flushed as they are produced cap what an abrupt stop can lose at ~2 seconds.
          .WithCustomArgument("-movflags +frag_keyframe+empty_moov+default_base_moof")
          .WithCustomArgument("-frag_duration 2000000")
          .WithCustomArgument("-flush_packets 1")
          // Hand ffmpeg the duration so it ends the recording itself and exits cleanly.
          // The timer below is only a backstop for when it overruns.
          .WithDuration(duration))
        .CancellableThrough(out var cancel, (int)ShutdownGrace.TotalMilliseconds);

      _logger.LogDebug("Recording {Url} with args {Args}", url, task.Arguments);

      // Backstop: if ffmpeg outruns its own -t — e.g. a stalled input that keeps reconnecting
      // ask it to wrap up rather than let it record indefinitely.
      _ = Task.Delay(duration + ShutdownGrace, overrunTimer.Token)
        .ContinueWith(_ => {
          _logger.LogWarning("Recording {Url} overran its scheduled duration - stopping it", url);
          cancel();
        }, TaskContinuationOptions.OnlyOnRanToCompletion);

      // Register external cancellation
      _ = cancellationToken.Register(() => {
        _logger.LogInformation("Recording {Url} cancelled externally", url);
        wasExternallyCancelled = true;
        cancel();
      });

      await task.ProcessAsynchronously();
      _logger.LogInformation("Finished recording {Url} after {Duration} seconds", url, duration.TotalSeconds);
    } catch (OperationCanceledException) {
      if (wasExternallyCancelled) {
        _logger.LogInformation("Recording {Url} cancelled by user - keeping partial file", url);
        return outputFile; // Return the partial file so it can be preserved
      }

      _logger.LogInformation("Recording {Url} stopped after {Duration} seconds", url, duration.TotalSeconds);
    } catch (Exception e) {
      _logger.LogError(e, "Error recording {Url}", url);
      throw; // Never let a failed recording be reported as a complete one
    } finally {
      overrunTimer.Cancel();
    }

    return outputFile;
  }

  public async Task<int> ReconcileInterruptedRecordingsAsync() {
    // Nothing is capturing at startup, so any row still marked "recording" was left there by a
    // restart or a crash - its ffmpeg process is long gone and its job will never resume.
    var interrupted = (await _recordingRepository.GetByStatusAsync("recording")).ToList();

    foreach (var recording in interrupted) {
      var usable = RecordingFiles.HasUsableVideo(recording.FilePath);

      recording.Status = usable ? "partial" : "failed";
      recording.IsRecorded = usable;
      if (!usable) {
        recording.FilePath = null;
      }

      await _recordingRepository.UpdateAsync(recording);
      _logger.LogWarning("Recording '{Title}' was interrupted by a previous run - marked {Status}",
        recording.Title, recording.Status);
    }

    return interrupted.Count;
  }

  public async Task<bool> DeleteRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default) {
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

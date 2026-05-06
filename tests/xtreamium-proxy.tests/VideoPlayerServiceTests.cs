using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Data.Repositories;
using Xtreamium.Proxy.Services;

namespace Xtreamium.Proxy.Tests;

public class VideoPlayerServiceTests {
  [Fact]
  public async Task PlayFromUrl_SecondCall_ClosesOnlyAppTrackedInstance() {
    if (!OperatingSystem.IsLinux()) {
      return;
    }

    var tempDirectory = Directory.CreateTempSubdirectory("videoplayer-service-tests-");
    var scriptPath = Path.Combine(tempDirectory.FullName, "fake-player.sh");
    var launchLogPath = Path.Combine(tempDirectory.FullName, "launches.log");

    await File.WriteAllTextAsync(scriptPath,
      $"#!/usr/bin/env sh\nprintf '%s|%s\\n' \"$$\" \"$1\" >> '{launchLogPath}'\nsleep 120\n");
    File.SetUnixFileMode(scriptPath,
      UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
      UnixFileMode.GroupRead | UnixFileMode.GroupExecute);

    var repository = new FakeSettingsRepository(new Dictionary<string, string> {
      ["MediaPlayerPath"] = scriptPath,
      ["MediaPlayerArguments"] = string.Empty
    });

    var service = new VideoPlayerService(NullLogger<VideoPlayerService>.Instance, repository);

    using var externallyStartedProcess = Process.Start(new ProcessStartInfo {
      FileName = scriptPath,
      Arguments = "external",
      UseShellExecute = false,
      CreateNoWindow = true
    });

    Assert.NotNull(externallyStartedProcess);
    var externalPid = externallyStartedProcess!.Id;

    var firstPlayResult = await service.PlayFromUrl("stream-1");
    Assert.True(firstPlayResult);
    var firstAppPid = await WaitForLaunchPidAsync(launchLogPath, "stream-1");

    var secondPlayResult = await service.PlayFromUrl("stream-2");
    Assert.True(secondPlayResult);
    var secondAppPid = await WaitForLaunchPidAsync(launchLogPath, "stream-2");

    Assert.True(IsProcessAlive(externalPid));
    Assert.False(IsProcessAlive(firstAppPid));
    Assert.True(IsProcessAlive(secondAppPid));

    KillIfAlive(secondAppPid);
    KillIfAlive(externalPid);
    tempDirectory.Delete(recursive: true);
  }
  
  [Fact]
  public async Task PlayFromUrl_SecondCall_ClosesOnlyAppTrackedInstance_WithCleanup() {
    if (!OperatingSystem.IsLinux()) {
      return;
    }

    var tempDirectory = Directory.CreateTempSubdirectory("videoplayer-service-tests-");
    var secondAppPid = 0;
    var externalPid = 0;
    try {
      var scriptPath = Path.Combine(tempDirectory.FullName, "fake-player.sh");
      var launchLogPath = Path.Combine(tempDirectory.FullName, "launches.log");

      await File.WriteAllTextAsync(scriptPath,
        $"#!/usr/bin/env sh\nprintf '%s|%s\\n' \"$$\" \"$1\" >> '{launchLogPath}'\nsleep 120\n");
      File.SetUnixFileMode(scriptPath,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
        UnixFileMode.GroupRead | UnixFileMode.GroupExecute);

      var repository = new FakeSettingsRepository(new Dictionary<string, string> {
        ["MediaPlayerPath"] = scriptPath,
        ["MediaPlayerArguments"] = string.Empty
      });

      var service = new VideoPlayerService(NullLogger<VideoPlayerService>.Instance, repository);

      using var externallyStartedProcess = Process.Start(new ProcessStartInfo {
        FileName = scriptPath,
        Arguments = "external",
        UseShellExecute = false,
        CreateNoWindow = true
      });

      Assert.NotNull(externallyStartedProcess);
      externalPid = externallyStartedProcess!.Id;

      var firstPlayResult = await service.PlayFromUrl("stream-1");
      Assert.True(firstPlayResult);
      var firstAppPid = await WaitForLaunchPidAsync(launchLogPath, "stream-1");

      var secondPlayResult = await service.PlayFromUrl("stream-2");
      Assert.True(secondPlayResult);
      secondAppPid = await WaitForLaunchPidAsync(launchLogPath, "stream-2");

      Assert.True(IsProcessAlive(externalPid));
      Assert.False(IsProcessAlive(firstAppPid));
      Assert.True(IsProcessAlive(secondAppPid));
    } finally {
      KillIfAlive(secondAppPid);
      KillIfAlive(externalPid);
      try {
        tempDirectory.Delete(recursive: true);
      } catch {
        // Ignore cleanup errors.
      }
    }
  }

  [Fact]
  public async Task PlayFromUrl_StaleTrackedOwnership_DoesNotCloseExternalProcess() {
    if (!OperatingSystem.IsLinux()) {
      return;
    }

    var tempDirectory = Directory.CreateTempSubdirectory("videoplayer-service-tests-");
    var scriptPath = Path.Combine(tempDirectory.FullName, "fake-player.sh");
    var launchLogPath = Path.Combine(tempDirectory.FullName, "launches.log");

    await File.WriteAllTextAsync(scriptPath,
      $"#!/usr/bin/env sh\nprintf '%s|%s\\n' \"$$\" \"$1\" >> '{launchLogPath}'\nsleep 120\n");
    File.SetUnixFileMode(scriptPath,
      UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
      UnixFileMode.GroupRead | UnixFileMode.GroupExecute);

    var repository = new FakeSettingsRepository(new Dictionary<string, string> {
      ["MediaPlayerPath"] = scriptPath,
      ["MediaPlayerArguments"] = string.Empty
    });

    var service = new VideoPlayerService(NullLogger<VideoPlayerService>.Instance, repository);

    using var externalProcess = Process.Start(new ProcessStartInfo {
      FileName = scriptPath,
      Arguments = "external-stale",
      UseShellExecute = false,
      CreateNoWindow = true
    });

    Assert.NotNull(externalProcess);
    var externalPid = externalProcess!.Id;
    var externalStartTimeUtc = externalProcess.StartTime.ToUniversalTime();

    // Seed stale ownership tracking: same PID, wrong start time.
    SetTrackedPlayerProcessForTests(scriptPath, externalPid, externalStartTimeUtc.AddSeconds(-1));

    var playResult = await service.PlayFromUrl("stream-stale");
    Assert.True(playResult);

    var appPid = await WaitForLaunchPidAsync(launchLogPath, "stream-stale");

    Assert.True(IsProcessAlive(externalPid));
    Assert.True(IsProcessAlive(appPid));

    KillIfAlive(appPid);
    KillIfAlive(externalPid);
    tempDirectory.Delete(recursive: true);
  }

  [Fact]
  public async Task PlayFromUrl_StaleTrackedOwnership_DoesNotCloseExternalProcess_WithCleanup() {
    if (!OperatingSystem.IsLinux()) {
      return;
    }

    var tempDirectory = Directory.CreateTempSubdirectory("videoplayer-service-tests-");
    var appPid = 0;
    var externalPid = 0;
    try {
      var scriptPath = Path.Combine(tempDirectory.FullName, "fake-player.sh");
      var launchLogPath = Path.Combine(tempDirectory.FullName, "launches.log");

      await File.WriteAllTextAsync(scriptPath,
        $"#!/usr/bin/env sh\nprintf '%s|%s\\n' \"$$\" \"$1\" >> '{launchLogPath}'\nsleep 120\n");
      File.SetUnixFileMode(scriptPath,
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
        UnixFileMode.GroupRead | UnixFileMode.GroupExecute);

      var repository = new FakeSettingsRepository(new Dictionary<string, string> {
        ["MediaPlayerPath"] = scriptPath,
        ["MediaPlayerArguments"] = string.Empty
      });

      var service = new VideoPlayerService(NullLogger<VideoPlayerService>.Instance, repository);

      using var externalProcess = Process.Start(new ProcessStartInfo {
        FileName = scriptPath,
        Arguments = "external-stale",
        UseShellExecute = false,
        CreateNoWindow = true
      });

      Assert.NotNull(externalProcess);
      externalPid = externalProcess!.Id;
      var externalStartTimeUtc = externalProcess.StartTime.ToUniversalTime();

      // Seed stale ownership tracking: same PID, wrong start time.
      SetTrackedPlayerProcessForTests(scriptPath, externalPid, externalStartTimeUtc.AddSeconds(-1));

      var playResult = await service.PlayFromUrl("stream-stale");
      Assert.True(playResult);

      appPid = await WaitForLaunchPidAsync(launchLogPath, "stream-stale");

      Assert.True(IsProcessAlive(externalPid));
      Assert.True(IsProcessAlive(appPid));
    } finally {
      KillIfAlive(appPid);
      KillIfAlive(externalPid);
      try {
        tempDirectory.Delete(recursive: true);
      } catch {
        // Ignore cleanup errors.
      }
    }
  }

  private static async Task<int> WaitForLaunchPidAsync(string launchLogPath, string marker) {
    var timeoutAt = DateTime.UtcNow.AddSeconds(8);

    while (DateTime.UtcNow < timeoutAt) {
      if (File.Exists(launchLogPath)) {
        var lines = await File.ReadAllLinesAsync(launchLogPath);
        foreach (var line in lines) {
          var parts = line.Split('|', 2);
          if (parts.Length == 2 && parts[1].Contains(marker, StringComparison.Ordinal) &&
              int.TryParse(parts[0], out var pid)) {
            return pid;
          }
        }
      }

      await Task.Delay(100);
    }

    throw new TimeoutException($"Timed out waiting for launch marker '{marker}' in {launchLogPath}");
  }

  private static bool IsProcessAlive(int pid) {
    try {
      using var process = Process.GetProcessById(pid);
      return !process.HasExited;
    } catch {
      return false;
    }
  }

  private static void KillIfAlive(int pid) {
    try {
      using var process = Process.GetProcessById(pid);
      if (!process.HasExited) {
        process.Kill(entireProcessTree: true);
        process.WaitForExit(3000);
      }
    } catch {
      // Ignore cleanup errors.
    }
  }

  private static void SetTrackedPlayerProcessForTests(string executablePath, int processId, DateTime startTimeUtc) {
    var serviceType = typeof(VideoPlayerService);
    var trackerField = serviceType.GetField("ActivePlayersByExecutable",
      BindingFlags.Static | BindingFlags.NonPublic);
    var trackedType = serviceType.GetNestedType("TrackedPlayerProcess", BindingFlags.NonPublic);

    Assert.NotNull(trackerField);
    Assert.NotNull(trackedType);

    var tracker = trackerField!.GetValue(null);
    var trackedEntry = Activator.CreateInstance(trackedType!, processId, startTimeUtc);

    Assert.NotNull(tracker);
    Assert.NotNull(trackedEntry);

    var executableKey = Path.GetFullPath(executablePath);
    var indexer = tracker!.GetType().GetProperty("Item");
    Assert.NotNull(indexer);
    indexer!.SetValue(tracker, trackedEntry, [executableKey]);
  }

  private sealed class FakeSettingsRepository : ISettingsRepository {
    private readonly Dictionary<string, string> _settings;

    public FakeSettingsRepository(Dictionary<string, string> settings) {
      _settings = settings;
    }

    public Task<Dictionary<string, string>> GetSettingsAsync() {
      return Task.FromResult(new Dictionary<string, string>(_settings));
    }

    public Task<string> GetSettingAsync(string key) {
      return Task.FromResult(_settings.TryGetValue(key, out var value) ? value : string.Empty);
    }

    public Task UpdateOrCreateSettingAsync(string key, string value) {
      _settings[key] = value;
      return Task.CompletedTask;
    }

    public Task UpdateOrCreateSettingsAsync(Dictionary<string, string> settings) {
      foreach (var (key, value) in settings) {
        _settings[key] = value;
      }

      return Task.CompletedTask;
    }

    public Task<IEnumerable<Setting>> GetAllAsync() {
      var values = _settings.Select(kv => new Setting {
        Id = Guid.NewGuid(),
        Key = kv.Key,
        Value = kv.Value
      });
      return Task.FromResult(values);
    }

    public Task<Setting?> GetByIdAsync(Guid id) {
      return Task.FromResult<Setting?>(null);
    }

    public Task<int> InsertAsync(Setting entity) {
      throw new NotSupportedException();
    }

    public Task<bool> UpdateAsync(Setting entity) {
      throw new NotSupportedException();
    }

    public Task<bool> DeleteAsync(Guid id) {
      throw new NotSupportedException();
    }
  }
}





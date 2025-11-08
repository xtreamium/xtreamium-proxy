using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data.Models;

namespace Xtreamium.Proxy.Data.Repositories;

public interface ISettingsRepository : IRepository<Setting> {
  Task<Dictionary<string, string>> GetSettingsAsync();

  Task<string> GetSettingAsync(string key);

  Task UpdateOrCreateSettingAsync(string key, string value);

  Task UpdateOrCreateSettingsAsync(Dictionary<string, string> settings);
}

public class SettingsRepository : Repository<Setting>, ISettingsRepository {
  private readonly AppConfiguration _config;

  public SettingsRepository(IDbConnectionFactory connectionFactory, IOptions<AppConfiguration> config) : base(
    connectionFactory) {
    _config = config.Value;
  }

  public async Task<Dictionary<string, string>> GetSettingsAsync() {
    var settings = await GetAllAsync();
    var settingsDict = settings.ToDictionary(s => s.Key, s => s.Value);

    // Ensure default settings exist
    if (!settingsDict.ContainsKey("MediaPlayerPath")) {
      await UpdateOrCreateSettingAsync("MediaPlayerPath", _config.VideoPlayer.MediaPlayerPath);
      settingsDict["MediaPlayerPath"] = _config.VideoPlayer.MediaPlayerPath;
    }

    if (!settingsDict.ContainsKey("MediaPlayerArguments")) {
      await UpdateOrCreateSettingAsync("MediaPlayerArguments", _config.VideoPlayer.MediaPlayerArguments);
      settingsDict["MediaPlayerArguments"] = _config.VideoPlayer.MediaPlayerArguments;
    }

    if (!settingsDict.ContainsKey("RecordingsPath")) {
      var defaultPath = _config.Recordings.Path ??
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings");
      await UpdateOrCreateSettingAsync("RecordingsPath", defaultPath);
      settingsDict["RecordingsPath"] = defaultPath;
    }

    if (!settingsDict.ContainsKey("Port")) {
      await UpdateOrCreateSettingAsync("Port", "8963");
      settingsDict["Port"] = "8963";
    }

    return settingsDict;
  }

  public async Task<string> GetSettingAsync(string key) {
    var settings = await GetAllAsync();
    return settings.FirstOrDefault(s => s.Key == key)?.Value ??
           _config.VideoPlayer.MediaPlayerArguments;
  }

  public async Task UpdateOrCreateSettingAsync(string key, string value) {
    var existing = (await GetAllAsync()).FirstOrDefault(s => s.Key == key);

    if (existing == null) {
      await InsertAsync(new Setting {Key = key, Value = value});
    } else {
      existing = existing with {Value = value};
      await UpdateAsync(existing);
    }
  }

  public async Task UpdateOrCreateSettingsAsync(Dictionary<string, string> settings) {
    foreach (var (key, value) in settings) {
      await UpdateOrCreateSettingAsync(key, value);
    }
  }
}

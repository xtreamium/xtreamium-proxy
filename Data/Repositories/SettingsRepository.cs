using Dapper;
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

  public override async Task<Setting?> GetByIdAsync(Guid id) {
    using var connection = await _connectionFactory.CreateConnectionAsync();
    const string sql = "SELECT * FROM settings WHERE Id = @Id";
    return await connection.QueryFirstOrDefaultAsync<Setting>(sql, new { Id = id });
  }

  public override async Task<bool> DeleteAsync(Guid id) {
    using var connection = await _connectionFactory.CreateConnectionAsync();
    const string sql = "DELETE FROM settings WHERE Id = @Id";
    var result = await connection.ExecuteAsync(sql, new { Id = id });
    return result > 0;
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

    // Always sync Port from appsettings configuration on startup
    var configuredPort = _config.Networking.Port.ToString();
    if (!settingsDict.ContainsKey("Port")) {
      await UpdateOrCreateSettingAsync("Port", configuredPort);
      settingsDict["Port"] = configuredPort;
    } else if (settingsDict["Port"] != configuredPort) {
      // Update database port to match appsettings configuration
      await UpdateOrCreateSettingAsync("Port", configuredPort);
      settingsDict["Port"] = configuredPort;
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
      await InsertAsync(new Setting {Id = Guid.NewGuid(), Key = key, Value = value});
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

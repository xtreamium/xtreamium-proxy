using Microsoft.Extensions.Options;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data.Models;

namespace Xtreamium.Proxy.Data.Repositories;

public interface ISettingsRepository : IRepository<Setting> {
  Task<Setting> GetSettingsAsync();

  Task<Setting> UpdateOrCreateSettingsAsync(Setting settings);
}

public class SettingsRepository : Repository<Setting>, ISettingsRepository {
  private readonly AppConfiguration _config;

  public SettingsRepository(IDbConnectionFactory connectionFactory, IOptions<AppConfiguration> config) : base(connectionFactory) {
    _config = config.Value;
  }

  public async Task<Setting> GetSettingsAsync() {
    var settings = (await GetAllAsync()).FirstOrDefault();

    // Return default settings if none exist
    if (settings != null) {
      return settings;
    }

    settings = new Setting {
      MpvArguments = _config.VideoPlayer.DefaultArguments,
      RecordingsPath = _config.Recordings.Path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings"),
      Port = 5000
    };

    await InsertAsync(settings);

    return settings;
  }

  public async Task<Setting> UpdateOrCreateSettingsAsync(Setting newSettings) {
    var existing = (await GetAllAsync()).FirstOrDefault();

    if (existing == null) {
      await InsertAsync(newSettings);
      return newSettings;
    }

    existing.MpvArguments = newSettings.MpvArguments;
    existing.RecordingsPath = newSettings.RecordingsPath;
    existing.Port = newSettings.Port;

    await UpdateAsync(existing);
    return existing;
  }
}

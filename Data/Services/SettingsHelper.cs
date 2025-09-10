using Dapper.Contrib.Extensions;
using Xtreamium.Proxy.Data.Models;
using Xtreamium.Proxy.Models;

namespace Xtreamium.Proxy.Data.Services;

public static class SettingsHelper {
  public static async Task<SettingsVm> GetSettings() {
    using var db = await DbHelper.GetConnection();
    var settings = db.GetAll<Setting>().FirstOrDefault();
    var vm = new SettingsVm {
      MpvArguments = settings.MpvArguments,
      RecordingsPath = settings.RecordingsPath,
      Port = settings.Port
    };
    return vm;
  }

  public static async Task WriteSettings(SettingsVm request) {
    using var db = await DbHelper.GetConnection();
    var settings = db.GetAll<Setting>().FirstOrDefault();
    if (settings == null) {
      settings = new Setting {
        MpvArguments = request.MpvArguments,
        RecordingsPath = request.RecordingsPath,
        Port = request.Port
      };
      await db.InsertAsync(settings);
    } else {
      settings.MpvArguments = request.MpvArguments;
      settings.RecordingsPath = request.RecordingsPath;
      settings.Port = request.Port;
      await db.UpdateAsync(settings);
    }
  }
}

using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Xtreamium.Proxy.Data;

public static class QuartzDbHelpers {
  private static string _getDbPath() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "xtreamium",
    "config.db");

  public static async Task<bool> ScaffoldDb() {
    var dbPath = _getDbPath();
    if (File.Exists(dbPath)) {
      return true;
    }

    await using var connection = new SqliteConnection($"Data Source={dbPath}");
    await connection.OpenAsync();

    await using Stream? stream =
      Assembly.GetExecutingAssembly().GetManifestResourceStream("Xtreamium.Proxy.Data.quartz.sql");
    if (stream is null) {
      throw new InvalidOperationException("No SQL found in resource.");
    }

    using StreamReader reader = new StreamReader(stream);
    string sql = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(sql)) {
      throw new InvalidOperationException("No SQL found in resource.");
    }

    var cmd = new SqliteCommand(sql, connection);
    await cmd.ExecuteNonQueryAsync();
    return true;
  }
}

using System.Reflection;
using Microsoft.Data.Sqlite;

namespace Xtreamium.Proxy.Data;

public static class QuartzDbHelpers {
  private static string _getDbPath() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "xtreamium");

  public static async Task<string> ScaffoldDb() {
    Console.WriteLine("Scaffolding database.");
    var dbPath = _getDbPath();
    Console.WriteLine($"Path is {dbPath}.");
    if (!Directory.Exists(dbPath)) {
      Directory.CreateDirectory(dbPath);
    }

    var dbFile = Path.Combine(dbPath, "config.db");
    var connectionString = $"Data Source={dbFile}";
    if (File.Exists(dbPath)) {
      return connectionString;
    }

    Console.WriteLine("Opening db");
    await using var connection = new SqliteConnection(connectionString);
    await connection.OpenAsync();

    await using Stream? stream =
      Assembly.GetExecutingAssembly()
        .GetManifestResourceStream("Xtreamium.Proxy.Data.quartz.sql");
    if (stream is null) {
      throw new InvalidOperationException("No SQL found in resource.");
    }

    using StreamReader reader = new StreamReader(stream);
    string sql = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(sql)) {
      throw new InvalidOperationException("No SQL found in resource.");
    }

    Console.WriteLine("Creating tables");
    var cmd = new SqliteCommand(sql, connection);
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Connection string is: {connectionString}");
    return connectionString;
  }
}

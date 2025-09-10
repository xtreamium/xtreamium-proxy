using System.Data;
using Microsoft.Data.Sqlite;
using System.Reflection;
using Dapper;
using Xtreamium.Proxy.Data.TypeHandlers;

namespace Xtreamium.Proxy.Data;

public static class DbHelper {
  public static string ConnectionString {
    get {
      var connectionStringBuilder = new SqliteConnectionStringBuilder() {
        DataSource = Path.Combine(_getDbPath(), "config.db")
      };
      return connectionStringBuilder.ConnectionString;
    }
  }

  public static async Task<IDbConnection> GetConnection() {
    var connection = new SqliteConnection(ConnectionString);
    await connection.OpenAsync();
    return connection;
  }


  private static string _getDbPath() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "xtreamium");

  public static async Task<string> ScaffoldDb() {
    SqlMapper.AddTypeHandler<Guid>(new GuidTypeHandler());
    SqlMapper.AddTypeHandler<DateTimeOffset>(new DateTimeOffsetTypeHandler());

    Console.WriteLine("Scaffolding database.");
    var dbPath = _getDbPath();
    Console.WriteLine($"Path is {dbPath}.");
    if (!Directory.Exists(dbPath)) {
      Directory.CreateDirectory(dbPath);
    }

    var dbFile = Path.Combine(dbPath, "config.db");
    if (File.Exists(dbFile)) {
      return ConnectionString;
    }

    Console.WriteLine("Opening db");
    using var connection = await GetConnection();

    await _runEmbeddedSql(connection, "quartz");
    Console.WriteLine($"Connection string is: {ConnectionString}");
    return ConnectionString;
  }

  private static async Task _runEmbeddedSql(IDbConnection connection, string resourceName) {
    await using var stream = Assembly
      .GetExecutingAssembly()
      .GetManifestResourceStream($"Xtreamium.Proxy.Data.{resourceName}.sql");
    if (stream is null) {
      throw new InvalidOperationException("No SQL found in resource.");
    }

    using var reader = new StreamReader(stream);
    var sql = await reader.ReadToEndAsync();
    if (string.IsNullOrWhiteSpace(sql)) {
      throw new InvalidOperationException("No SQL found in resource.");
    }

    Console.WriteLine("Creating tables");
    var cmd = new SqliteCommand(sql, connection as SqliteConnection);
    await cmd.ExecuteNonQueryAsync();
  }
}

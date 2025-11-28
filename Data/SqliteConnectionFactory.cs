using System.Data;
using Microsoft.Data.Sqlite;

namespace Xtreamium.Proxy.Data;

/// <summary>
/// Simplified database connection factory and configuration
/// </summary>
public interface IDbConnectionFactory {
  Task<IDbConnection> CreateConnectionAsync();
  string ConnectionString { get; }
}

public class SqliteConnectionFactory : IDbConnectionFactory {
  private readonly string _connectionString;

  public SqliteConnectionFactory() {
    var dbPath = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "xtreamium");

    if (!Directory.Exists(dbPath)) {
      Directory.CreateDirectory(dbPath);
    }

    var dbFileName = DatabaseServiceExtensions.GetConfigurationDbFileName();
    var builder = new SqliteConnectionStringBuilder {
      DataSource = Path.Combine(dbPath, dbFileName),
      Cache = SqliteCacheMode.Shared,
      Mode = SqliteOpenMode.ReadWriteCreate
    };

    _connectionString = builder.ConnectionString;
  }

  public string ConnectionString => _connectionString;

  public async Task<IDbConnection> CreateConnectionAsync() {
    var connection = new SqliteConnection(_connectionString);
    await connection.OpenAsync();
    return connection;
  }
}

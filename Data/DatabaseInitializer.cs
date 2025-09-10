using System.Reflection;
using Dapper;

namespace Xtreamium.Proxy.Data;

/// <summary>
/// Handles database initialization and Quartz schema setup
/// Simplified version of the original DbHelper
/// </summary>
public class DatabaseInitializer {
  private readonly IDbConnectionFactory _connectionFactory;
  private readonly ILogger<DatabaseInitializer> _logger;

  public DatabaseInitializer(IDbConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger) {
    _connectionFactory = connectionFactory;
    _logger = logger;
  }

  /// <summary>
  /// Initialize Quartz tables and return connection string for Quartz configuration
  /// </summary>
  public async Task<string> InitializeQuartzTablesAsync() {
    _logger.LogDebug("Initializing Quartz database tables");

    var dbFile = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "xtreamium",
      "config.db");

    // Only create Quartz tables if database doesn't exist
    if (!File.Exists(dbFile)) {
      _logger.LogInformation("Creating new database with Quartz schema");
      await CreateQuartzTablesAsync();
    }

    return _connectionFactory.ConnectionString;
  }

  private async Task CreateQuartzTablesAsync() {
    using var connection = await _connectionFactory.CreateConnectionAsync();

    await using var stream = Assembly
      .GetExecutingAssembly()
      .GetManifestResourceStream("Xtreamium.Proxy.Data.quartz.sql");

    if (stream == null) {
      throw new InvalidOperationException("Quartz SQL schema not found in embedded resources");
    }

    using var reader = new StreamReader(stream);
    var sql = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(sql)) {
      throw new InvalidOperationException("Quartz SQL schema is empty");
    }

    _logger.LogDebug("Executing Quartz schema creation");
    await connection.ExecuteAsync(sql);
  }
}

using Dapper;
using Microsoft.VisualBasic.FileIO;
using Xtreamium.Proxy.Configuration;
using Xtreamium.Proxy.Data.Repositories;

namespace Xtreamium.Proxy.Data;

/// <summary>
/// Service registration extensions for the improved database layer
/// </summary>
public static class DatabaseServiceExtensions {
  /// <summary>
  /// Register all database-related services
  /// </summary>
  public static IServiceCollection AddDatabase(this IServiceCollection services) {
    // Configure Dapper type handlers for SQLite compatibility
    SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
    SqlMapper.AddTypeHandler(new GuidHandler());

    // Core database services
    services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
    services.AddScoped<DatabaseInitializer>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Repository services
    services.AddScoped<IRecordingRepository, RecordingRepository>();
    services.AddScoped<ISettingsRepository, SettingsRepository>();

    // Generic repository for other entities if needed
    services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    return services;
  }

  /// <summary>
  /// Get the database filename based on environment
  /// </summary>
  public static string GetConfigurationDbFileName() {
    var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
    return isDevelopment ? "config_dev.db" : "config.db";
  }

  /// <summary>
  /// Get the configuration database file path
  /// </summary>
  public static string GetConfigurationDbFilePath() {
    var dbFile = Path.Combine(AppPaths.AppDataDirectory, GetConfigurationDbFileName());

    Directory.CreateDirectory(Path.GetDirectoryName(dbFile)!);

    return dbFile;
  }

  /// <summary>
  /// Get the configuration database connection string
  /// </summary>
  public static string GetConfigurationDbConnectionString() {
    return $"Data Source={GetConfigurationDbFilePath()}";
  }

  /// <summary>
  /// Initialize database and return connection string for Quartz
  /// </summary>
  public static async Task<string> InitializeDatabaseAsync(this IServiceProvider services) {
    using var scope = services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    return await initializer.InitializeQuartzTablesAsync();
  }

  /// <summary>
  /// Initialize Quartz tables after the application is built
  /// </summary>
  public static async Task<string> InitializeConfigurationDbAsync(this WebApplication app, string dbFile) {
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database path: {DbPath}", dbFile);

    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    return await dbInitializer.InitializeQuartzTablesAsync();
  }
}

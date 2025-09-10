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
  /// Initialize database and return connection string for Quartz
  /// </summary>
  public static async Task<string> InitializeDatabaseAsync(this IServiceProvider services) {
    using var scope = services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    return await initializer.InitializeQuartzTablesAsync();
  }
}

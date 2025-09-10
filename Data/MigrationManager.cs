using System.Reflection;
using FluentMigrator.Runner;

namespace Xtreamium.Proxy.Data;

public static class MigrationManager {
  public static IServiceCollection AddMigrations(this IServiceCollection services) {
    services.AddLogging(c => c.AddFluentMigratorConsole())
      .AddFluentMigratorCore()
      .ConfigureRunner(c => c.AddSQLite()
        .WithGlobalConnectionString(DbHelper.ConnectionString)
        .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations());

    return services;
  }

  public static WebApplication MigrateDatabase(this WebApplication app) {
    using (var scope = app.Services.CreateScope()) {
      var migrationRunner = scope.ServiceProvider
        .GetRequiredService<IMigrationRunner>();
      migrationRunner.MigrateUp();
    }

    return app;
  }
}

using Quartz;
using Quartz.Simpl;

namespace Xtreamium.Proxy.Services.Jobs;

public static class JobsStartup {
  public static IServiceCollection AddJobs(this IServiceCollection services,
    string jobsDb) {
    services.AddQuartz(q => {
      q.SchedulerId = "Xtreamium-Proxy-Scheduler";
      q.SchedulerName = "Xtreamium Proxy Scheduler";

      q.UsePersistentStore(options => {
        options.UseSystemTextJsonSerializer();
        options.UseSQLite(jobsDb);
      });
    }).AddQuartzHostedService(options => {
      options.WaitForJobsToComplete = true;
    });
    return services;
  }
}

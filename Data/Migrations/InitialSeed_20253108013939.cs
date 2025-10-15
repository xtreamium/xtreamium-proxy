using FluentMigrator;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108013939)]
public class InitialSeed : Migration{
  public override void Up() {
    Insert.IntoTable("settings").Row(new {
      MpvArguments = VideoPlayerConfiguration.DefaultMpvArguments,
      RecordingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings"),
      Port = 8963
    });
  }

  public override void Down() {
    Delete.FromTable("settings").AllRows();
  }
}

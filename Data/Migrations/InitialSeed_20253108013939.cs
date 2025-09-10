using FluentMigrator;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108013939)]
public class InitialSeed : Migration{
  public override void Up() {
    Insert.IntoTable("settings").Row(new {
      MpvArguments = "--no-border --ontop --screen=2 --cache=yes --demuxer-max-bytes=5GiB --demuxer-max-back-bytes=5GiB {{URL}}",
      RecordingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings"),
      Port = 5000
    });
  }

  public override void Down() {
    Delete.FromTable("settings").AllRows();
  }
}

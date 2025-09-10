using FluentMigrator;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108013939)]
public class InitialSeed : Migration{
  public override void Up() {
    Insert.IntoTable("settings").Row(new {
      MpvArguments = "--keep-open=yes --geometry=1024x768-0-0 --ontop --screen=2 --border=no {{URL}}",
      RecordingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings"),
      Port = 5000
    });
  }

  public override void Down() {
    Delete.FromTable("settings").AllRows();
  }
}

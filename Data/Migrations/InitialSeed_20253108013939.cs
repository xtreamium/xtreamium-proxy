using FluentMigrator;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108013939)]
public class InitialSeed : Migration{
  public override void Up() {
    Insert.IntoTable("settings").Row(new {
      Key = "MediaPlayerPath",
      Value = VideoPlayerConfiguration.DefaultMediaPlayerPath
    });
    
    Insert.IntoTable("settings").Row(new {
      Key = "MediaPlayerArguments",
      Value = VideoPlayerConfiguration.DefaultMediaPlayerArguments
    });
    
    Insert.IntoTable("settings").Row(new {
      Key = "RecordingsPath",
      Value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings")
    });
    
    Insert.IntoTable("settings").Row(new {
      Key = "Port",
      Value = "8963"
    });
  }

  public override void Down() {
    Delete.FromTable("settings").AllRows();
  }
}

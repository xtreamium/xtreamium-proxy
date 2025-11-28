using FluentMigrator;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108013939)]
public class InitialSeed : Migration{
  public override void Up() {
    Insert.IntoTable("settings").Row(new {
      Id = Guid.NewGuid().ToString(),
      Key = "MediaPlayerPath",
      Value = VideoPlayerConfiguration.DefaultMediaPlayerPath
    });
    
    Insert.IntoTable("settings").Row(new {
      Id = Guid.NewGuid().ToString(),
      Key = "MediaPlayerArguments",
      Value = VideoPlayerConfiguration.DefaultMediaPlayerArguments
    });
    
    Insert.IntoTable("settings").Row(new {
      Id = Guid.NewGuid().ToString(),
      Key = "RecordingsPath",
      Value = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings")
    });
    
    Insert.IntoTable("settings").Row(new {
      Id = Guid.NewGuid().ToString(),
      Key = "Port",
      Value = NetworkingConfiguration.DefaultPort.ToString()
    });
  }

  public override void Down() {
    Delete.FromTable("settings").AllRows();
  }
}

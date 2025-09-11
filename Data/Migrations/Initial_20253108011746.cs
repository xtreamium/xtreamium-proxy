using FluentMigrator;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108011746)]
public class InitialTables : Migration {
  public override void Up() {
    Create.Table("settings")
      .WithColumn("Id").AsInt32().PrimaryKey().Identity()
      .WithColumn("MpvArguments").AsString().NotNullable()
      .WithDefaultValue(VideoPlayerConfiguration.DefaultMpvArguments)
      .WithColumn("RecordingsPath").AsString().NotNullable().WithDefaultValue(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Recordings"))
      .WithColumn("Port").AsInt32().NotNullable().WithDefaultValue(5000);

    Create.Table("recordings")
      .WithColumn("Id").AsInt32().PrimaryKey().Identity()
      .WithColumn("JobId").AsString().NotNullable()
      .WithColumn("Url").AsString().NotNullable()
      .WithColumn("Title").AsString().NotNullable()
      .WithColumn("StartTime").AsDateTime().NotNullable()
      .WithColumn("Duration").AsInt32().NotNullable()
      .WithColumn("IsRecorded").AsBoolean().NotNullable().WithDefaultValue(false)
      .WithColumn("FilePath").AsString().Nullable();
  }

  public override void Down() {
    Delete.Table("Settings");
    Delete.Table("Recordings");
  }
}

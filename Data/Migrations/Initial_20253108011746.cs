using FluentMigrator;
using Xtreamium.Proxy.Configuration;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20253108011746)]
public class InitialTables : Migration {
  public override void Up() {
    Create.Table("settings")
      .WithColumn("Id").AsInt32().PrimaryKey().Identity()
      .WithColumn("Key").AsString().NotNullable().Unique()
      .WithColumn("Value").AsString().NotNullable();

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

using FluentMigrator;

namespace Xtreamium.Proxy.Data.Migrations;

[Migration(20251128120000)]
public class AddRecordingStatus : Migration {
  public override void Up() {
    // Add Status column to recordings table with default value "pending"
    Alter.Table("recordings")
      .AddColumn("Status").AsString().NotNullable().WithDefaultValue("pending");
  }

  public override void Down() {
    // Remove Status column if rolling back
    Delete.Column("Status").FromTable("recordings");
  }
}


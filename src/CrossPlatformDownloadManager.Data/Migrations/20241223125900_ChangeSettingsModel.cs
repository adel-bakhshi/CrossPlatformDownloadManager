using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSettingsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte>(
                name: "DuplicateDownloadLinkAction",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<bool>(
                name: "IsSpeedLimiterEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "LimitSpeed",
                table: "Settings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LimitUnit",
                table: "Settings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpeedLimiterEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "LimitSpeed",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "LimitUnit",
                table: "Settings");

            migrationBuilder.AlterColumn<string>(
                name: "DuplicateDownloadLinkAction",
                table: "Settings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(byte),
                oldType: "INTEGER");
        }
    }
}

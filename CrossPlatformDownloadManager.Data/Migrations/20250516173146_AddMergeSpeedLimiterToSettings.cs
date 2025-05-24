using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMergeSpeedLimiterToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMergeSpeedLimitEnabled",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "MergeLimitSpeed",
                table: "Settings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MergeLimitUnit",
                table: "Settings",
                type: "TEXT",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMergeSpeedLimitEnabled",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "MergeLimitSpeed",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "MergeLimitUnit",
                table: "Settings");
        }
    }
}

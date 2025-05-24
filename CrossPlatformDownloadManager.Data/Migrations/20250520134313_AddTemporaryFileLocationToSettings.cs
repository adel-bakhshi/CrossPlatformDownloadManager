using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTemporaryFileLocationToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemporaryFileLocation",
                table: "Settings",
                type: "TEXT",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemporaryFileLocation",
                table: "Settings");
        }
    }
}

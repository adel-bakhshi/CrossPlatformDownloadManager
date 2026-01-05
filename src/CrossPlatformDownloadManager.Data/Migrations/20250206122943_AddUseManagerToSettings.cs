using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUseManagerToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseManager",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseManager",
                table: "Settings");
        }
    }
}

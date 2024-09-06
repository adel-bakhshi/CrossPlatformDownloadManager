using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsPausedAndIsErrorPropertyFromDownloadFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsError",
                table: "DownloadFiles");

            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "DownloadFiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsError",
                table: "DownloadFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "DownloadFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}

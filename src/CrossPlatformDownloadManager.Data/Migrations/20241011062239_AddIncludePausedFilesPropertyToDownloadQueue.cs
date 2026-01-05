using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIncludePausedFilesPropertyToDownloadQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryFileExtensions_Categories_CategoryId",
                table: "CategoryFileExtensions");

            migrationBuilder.DropForeignKey(
                name: "FK_DownloadFiles_Categories_CategoryId",
                table: "DownloadFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DownloadFiles_DownloadQueues_DownloadQueueId",
                table: "DownloadFiles");

            migrationBuilder.AddColumn<bool>(
                name: "IncludePausedFiles",
                table: "DownloadQueues",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                table: "Categories",
                column: "CategorySaveDirectoryId",
                principalTable: "CategorySaveDirectories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryFileExtensions_Categories_CategoryId",
                table: "CategoryFileExtensions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadFiles_Categories_CategoryId",
                table: "DownloadFiles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadFiles_DownloadQueues_DownloadQueueId",
                table: "DownloadFiles",
                column: "DownloadQueueId",
                principalTable: "DownloadQueues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_CategoryFileExtensions_Categories_CategoryId",
                table: "CategoryFileExtensions");

            migrationBuilder.DropForeignKey(
                name: "FK_DownloadFiles_Categories_CategoryId",
                table: "DownloadFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_DownloadFiles_DownloadQueues_DownloadQueueId",
                table: "DownloadFiles");

            migrationBuilder.DropColumn(
                name: "IncludePausedFiles",
                table: "DownloadQueues");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                table: "Categories",
                column: "CategorySaveDirectoryId",
                principalTable: "CategorySaveDirectories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryFileExtensions_Categories_CategoryId",
                table: "CategoryFileExtensions",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadFiles_Categories_CategoryId",
                table: "DownloadFiles",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DownloadFiles_DownloadQueues_DownloadQueueId",
                table: "DownloadFiles",
                column: "DownloadQueueId",
                principalTable: "DownloadQueues",
                principalColumn: "Id");
        }
    }
}

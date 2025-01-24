using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCategorySaveDirectoryIdFromCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_CategorySaveDirectories_CategoryId",
                table: "CategorySaveDirectories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_CategorySaveDirectoryId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "CategorySaveDirectoryId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySaveDirectories_CategoryId",
                table: "CategorySaveDirectories",
                column: "CategoryId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CategorySaveDirectories_Categories_CategoryId",
                table: "CategorySaveDirectories",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategorySaveDirectories_Categories_CategoryId",
                table: "CategorySaveDirectories");

            migrationBuilder.DropIndex(
                name: "IX_CategorySaveDirectories_CategoryId",
                table: "CategorySaveDirectories");

            migrationBuilder.AddColumn<int>(
                name: "CategorySaveDirectoryId",
                table: "Categories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategorySaveDirectories_CategoryId",
                table: "CategorySaveDirectories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategorySaveDirectoryId",
                table: "Categories",
                column: "CategorySaveDirectoryId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                table: "Categories",
                column: "CategorySaveDirectoryId",
                principalTable: "CategorySaveDirectories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

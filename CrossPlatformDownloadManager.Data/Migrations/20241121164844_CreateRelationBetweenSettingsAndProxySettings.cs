using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateRelationBetweenSettingsAndProxySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SettingsId",
                table: "ProxySettings",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProxySettings_SettingsId",
                table: "ProxySettings",
                column: "SettingsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProxySettings_Settings_SettingsId",
                table: "ProxySettings",
                column: "SettingsId",
                principalTable: "Settings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProxySettings_Settings_SettingsId",
                table: "ProxySettings");

            migrationBuilder.DropIndex(
                name: "IX_ProxySettings_SettingsId",
                table: "ProxySettings");

            migrationBuilder.DropColumn(
                name: "SettingsId",
                table: "ProxySettings");
        }
    }
}

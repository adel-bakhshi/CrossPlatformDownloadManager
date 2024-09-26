using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryHeaders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryHeaders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategorySaveDirectories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    SaveDirectory = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySaveDirectories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DownloadQueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StartOnApplicationStartup = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDownloadSchedule = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    StopDownloadSchedule = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    IsDaily = table.Column<bool>(type: "INTEGER", nullable: false),
                    JustForDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DaysOfWeek = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RetryOnDownloadingFailed = table.Column<bool>(type: "INTEGER", nullable: false),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ShowAlarmWhenDone = table.Column<bool>(type: "INTEGER", nullable: true),
                    ExitProgramWhenDone = table.Column<bool>(type: "INTEGER", nullable: true),
                    TurnOffComputerWhenDone = table.Column<bool>(type: "INTEGER", nullable: true),
                    TurnOffComputerMode = table.Column<byte>(type: "INTEGER", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadCountAtSameTime = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadQueues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartOnSystemStartup = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseBrowserExtension = table.Column<bool>(type: "INTEGER", nullable: false),
                    DarkMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowStartDownloadDialog = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowCompleteDownloadDialog = table.Column<bool>(type: "INTEGER", nullable: false),
                    DuplicateDownloadLinkAction = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MaximumConnectionsCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ProxyMode = table.Column<byte>(type: "INTEGER", nullable: false),
                    ProxyType = table.Column<byte>(type: "INTEGER", nullable: false),
                    CustomProxySettings = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    UseDownloadCompleteSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseDownloadStoppedSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseDownloadFailedSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseQueueStartedSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseQueueStoppedSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseQueueFinishedSound = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseSystemNotifications = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoAddLinkFromSites = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CategorySaveDirectoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_CategorySaveDirectories_CategorySaveDirectoryId",
                        column: x => x.CategorySaveDirectoryId,
                        principalTable: "CategorySaveDirectories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CategoryFileExtensions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Extension = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Alias = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryFileExtensions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryFileExtensions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DownloadFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    DownloadQueueId = table.Column<int>(type: "INTEGER", nullable: true),
                    Size = table.Column<double>(type: "REAL", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<byte>(type: "INTEGER", nullable: true),
                    LastTryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DownloadQueuePriority = table.Column<int>(type: "INTEGER", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    DownloadProgress = table.Column<float>(type: "REAL", nullable: false),
                    ElapsedTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    TimeLeft = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    TransferRate = table.Column<float>(type: "REAL", nullable: true),
                    SaveLocation = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DownloadPackage = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DownloadFiles_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DownloadFiles_DownloadQueues_DownloadQueueId",
                        column: x => x.DownloadQueueId,
                        principalTable: "DownloadQueues",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategorySaveDirectoryId",
                table: "Categories",
                column: "CategorySaveDirectoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryFileExtensions_CategoryId",
                table: "CategoryFileExtensions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySaveDirectories_CategoryId",
                table: "CategorySaveDirectories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadFiles_CategoryId",
                table: "DownloadFiles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadFiles_DownloadQueueId",
                table: "DownloadFiles",
                column: "DownloadQueueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryFileExtensions");

            migrationBuilder.DropTable(
                name: "CategoryHeaders");

            migrationBuilder.DropTable(
                name: "DownloadFiles");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "DownloadQueues");

            migrationBuilder.DropTable(
                name: "CategorySaveDirectories");
        }
    }
}

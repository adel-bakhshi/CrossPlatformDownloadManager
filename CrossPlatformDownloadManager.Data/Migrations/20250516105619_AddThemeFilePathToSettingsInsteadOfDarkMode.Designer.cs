﻿// <auto-generated />
using System;
using CrossPlatformDownloadManager.Data.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CrossPlatformDownloadManager.Data.Migrations
{
    [DbContext(typeof(DownloadManagerDbContext))]
    [Migration("20250516105619_AddThemeFilePathToSettingsInsteadOfDarkMode")]
    partial class AddThemeFilePathToSettingsInsteadOfDarkMode
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.4");

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.Category", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AutoAddLinkFromSites")
                        .HasMaxLength(1000)
                        .HasColumnType("TEXT");

                    b.Property<string>("Icon")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Categories");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.CategoryFileExtension", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<int?>("CategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Extension")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.ToTable("CategoryFileExtensions");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.CategoryHeader", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Icon")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("CategoryHeaders");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.CategorySaveDirectory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("CategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SaveDirectory")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId")
                        .IsUnique();

                    b.ToTable("CategorySaveDirectories");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.DownloadFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CategoryId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("TEXT");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<string>("DownloadPackage")
                        .HasMaxLength(5000)
                        .HasColumnType("TEXT");

                    b.Property<float>("DownloadProgress")
                        .HasColumnType("REAL");

                    b.Property<int?>("DownloadQueueId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("DownloadQueuePriority")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("ElapsedTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsSizeUnknown")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastTryDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("SaveLocation")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<double>("Size")
                        .HasColumnType("REAL");

                    b.Property<byte?>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("TimeLeft")
                        .HasColumnType("TEXT");

                    b.Property<float?>("TransferRate")
                        .HasColumnType("REAL");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CategoryId");

                    b.HasIndex("DownloadQueueId");

                    b.ToTable("DownloadFiles");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.DownloadQueue", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("DaysOfWeek")
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<int>("DownloadCountAtSameTime")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ExitProgramWhenDone")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IncludePausedFiles")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDaily")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDefault")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsLastChoice")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("JustForDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("RetryCount")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("RetryOnDownloadingFailed")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowAlarmWhenDone")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("StartDownloadSchedule")
                        .HasColumnType("TEXT");

                    b.Property<bool>("StartOnApplicationStartup")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("StopDownloadSchedule")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<byte?>("TurnOffComputerMode")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("TurnOffComputerWhenDone")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("DownloadQueues");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.ProxySettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Host")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<string>("Port")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<int?>("SettingsId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("Username")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SettingsId");

                    b.ToTable("ProxySettings");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.Settings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("AlwaysKeepManagerOnTop")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ApplicationFont")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("DataGridColumnsSettings")
                        .HasMaxLength(5000)
                        .HasColumnType("TEXT");

                    b.Property<bool>("DisableCategories")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("DuplicateDownloadLinkAction")
                        .HasColumnType("INTEGER");

                    b.Property<string>("GlobalSaveLocation")
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<bool>("HasApplicationBeenRunYet")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsSpeedLimiterEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<double?>("LimitSpeed")
                        .HasColumnType("REAL");

                    b.Property<string>("LimitUnit")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("ManagerPoint")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<int>("MaximumConnectionsCount")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("ProxyMode")
                        .HasColumnType("INTEGER");

                    b.Property<byte>("ProxyType")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowCategoriesPanel")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(true);

                    b.Property<bool>("ShowCompleteDownloadDialog")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("ShowStartDownloadDialog")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("StartOnSystemStartup")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ThemeFilePath")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("TEXT");

                    b.Property<bool>("UseBrowserExtension")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseDownloadCompleteSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseDownloadFailedSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseDownloadStoppedSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseManager")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseQueueFinishedSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseQueueStartedSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseQueueStoppedSound")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("UseSystemNotifications")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.CategoryFileExtension", b =>
                {
                    b.HasOne("CrossPlatformDownloadManager.Data.Models.Category", "Category")
                        .WithMany("FileExtensions")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Category");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.CategorySaveDirectory", b =>
                {
                    b.HasOne("CrossPlatformDownloadManager.Data.Models.Category", "Category")
                        .WithOne("CategorySaveDirectory")
                        .HasForeignKey("CrossPlatformDownloadManager.Data.Models.CategorySaveDirectory", "CategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Category");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.DownloadFile", b =>
                {
                    b.HasOne("CrossPlatformDownloadManager.Data.Models.Category", "Category")
                        .WithMany("DownloadFiles")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("CrossPlatformDownloadManager.Data.Models.DownloadQueue", "DownloadQueue")
                        .WithMany("DownloadFiles")
                        .HasForeignKey("DownloadQueueId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Category");

                    b.Navigation("DownloadQueue");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.ProxySettings", b =>
                {
                    b.HasOne("CrossPlatformDownloadManager.Data.Models.Settings", "Settings")
                        .WithMany("Proxies")
                        .HasForeignKey("SettingsId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Settings");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.Category", b =>
                {
                    b.Navigation("CategorySaveDirectory");

                    b.Navigation("DownloadFiles");

                    b.Navigation("FileExtensions");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.DownloadQueue", b =>
                {
                    b.Navigation("DownloadFiles");
                });

            modelBuilder.Entity("CrossPlatformDownloadManager.Data.Models.Settings", b =>
                {
                    b.Navigation("Proxies");
                });
#pragma warning restore 612, 618
        }
    }
}

using CrossPlatformDownloadManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossPlatformDownloadManager.Data.DbContext;

public class DownloadManagerDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public DbSet<CategoryHeader> CategoryHeaders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryFileExtension> CategoryFileExtensions { get; set; }
    public DbSet<CategorySaveDirectory> CategorySaveDirectories { get; set; }
    public DbSet<DownloadFile> DownloadFiles { get; set; }
    public DbSet<DownloadQueue> DownloadQueues { get; set; }

    public string DbPath { get; }
    
    public DownloadManagerDbContext()
    {
        var dbPath = Path.Join(Environment.CurrentDirectory, "ApplicationData.db");
        DbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(options =>
        {
            options.HasOne(c => c.CategorySaveDirectory)
                .WithOne(sd => sd.Category)
                .HasForeignKey<CategorySaveDirectory>(sd => sd.CategoryId);

            options.HasMany(c => c.FileExtensions)
                .WithOne(fe => fe.Category)
                .HasForeignKey(fe => fe.CategoryId);

            options.HasMany(c => c.DownloadFiles)
                .WithOne(df => df.Category)
                .HasForeignKey(df => df.CategoryId);

            options.HasIndex(c => c.CategorySaveDirectoryId);
        });

        modelBuilder.Entity<CategoryFileExtension>(options =>
        {
            options.HasOne(fe => fe.Category)
                .WithMany(c => c.FileExtensions)
                .HasForeignKey(fe => fe.CategoryId);

            options.HasIndex(fe => fe.CategoryId);
        });

        modelBuilder.Entity<CategorySaveDirectory>(options =>
        {
            options.HasOne<Category>(sd => sd.Category)
                .WithOne(c => c.CategorySaveDirectory)
                .HasForeignKey<Category>(c => c.CategorySaveDirectoryId);

            options.HasIndex(sd => sd.CategoryId);
        });

        modelBuilder.Entity<DownloadFile>(options =>
        {
            options.HasOne(df => df.DownloadQueue)
                .WithMany(dq => dq.DownloadFiles)
                .HasForeignKey(df => df.DownloadQueueId);
            
            options.HasOne(df => df.Category)
                .WithMany(c => c.DownloadFiles)
                .HasForeignKey(df => df.CategoryId);

            options.HasIndex(df => df.CategoryId);
            options.HasIndex(df => df.DownloadQueueId);
        });

        modelBuilder.Entity<DownloadQueue>(options =>
        {
            options.HasMany(dq => dq.DownloadFiles)
                .WithOne(df => df.DownloadQueue)
                .HasForeignKey(df => df.DownloadQueueId);
        });
    }
}
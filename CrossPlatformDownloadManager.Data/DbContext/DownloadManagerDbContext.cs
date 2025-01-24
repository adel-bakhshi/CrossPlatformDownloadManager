using CrossPlatformDownloadManager.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CrossPlatformDownloadManager.Data.DbContext;

public class DownloadManagerDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    #region Private Fields

    private readonly string _dbPath;

    #endregion

    #region Properties

    public DbSet<CategoryHeader> CategoryHeaders { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<CategoryFileExtension> CategoryFileExtensions { get; set; }
    public DbSet<CategorySaveDirectory> CategorySaveDirectories { get; set; }
    public DbSet<DownloadFile> DownloadFiles { get; set; }
    public DbSet<DownloadQueue> DownloadQueues { get; set; }
    public DbSet<Settings> Settings { get; set; }
    public DbSet<ProxySettings> ProxySettings { get; set; }

    #endregion

    public DownloadManagerDbContext()
    {
        _dbPath = Path.Combine(Environment.CurrentDirectory, "ApplicationData.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(options =>
        {
            options.HasOne(c => c.CategorySaveDirectory)
                .WithOne(sd => sd.Category)
                .HasForeignKey<CategorySaveDirectory>(sd => sd.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.HasMany(c => c.FileExtensions)
                .WithOne(fe => fe.Category)
                .HasForeignKey(fe => fe.CategoryId)
                .HasPrincipalKey(c => c.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.HasMany(c => c.DownloadFiles)
                .WithOne(df => df.Category)
                .HasForeignKey(df => df.CategoryId)
                .HasPrincipalKey(c => c.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CategoryFileExtension>(options =>
        {
            options.HasOne(fe => fe.Category)
                .WithMany(c => c.FileExtensions)
                .HasForeignKey(fe => fe.CategoryId)
                .HasPrincipalKey(c => c.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.HasIndex(fe => fe.CategoryId);
        });

        modelBuilder.Entity<CategorySaveDirectory>(options =>
        {
            options.HasIndex(sd => sd.CategoryId);
        });

        modelBuilder.Entity<DownloadFile>(options =>
        {
            options.HasOne(df => df.DownloadQueue)
                .WithMany(dq => dq.DownloadFiles)
                .HasForeignKey(df => df.DownloadQueueId)
                .HasPrincipalKey(dq => dq.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.HasOne(df => df.Category)
                .WithMany(c => c.DownloadFiles)
                .HasForeignKey(df => df.CategoryId)
                .HasPrincipalKey(c => c.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.HasIndex(df => df.CategoryId);
            options.HasIndex(df => df.DownloadQueueId);
        });

        modelBuilder.Entity<DownloadQueue>(options =>
        {
            options.HasMany(dq => dq.DownloadFiles)
                .WithOne(df => df.DownloadQueue)
                .HasForeignKey(df => df.DownloadQueueId)
                .HasPrincipalKey(dq => dq.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Settings>(options =>
        {
            options.HasMany(s => s.Proxies)
                .WithOne(p => p.Settings)
                .HasForeignKey(p => p.SettingsId)
                .HasPrincipalKey(s => s.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.Property(s => s.ShowCategoriesPanel)
                .HasDefaultValue(true);
        });

        modelBuilder.Entity<ProxySettings>(options =>
        {
            options.HasOne(p => p.Settings)
                .WithMany(s => s.Proxies)
                .HasForeignKey(p => p.SettingsId)
                .HasPrincipalKey(s => s.Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            options.HasIndex(p => p.SettingsId);
        });
    }
}
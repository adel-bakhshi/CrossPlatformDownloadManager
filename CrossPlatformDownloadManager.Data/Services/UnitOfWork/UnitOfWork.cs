using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using CrossPlatformDownloadManager.Data.Services.Repository.Services;
using CrossPlatformDownloadManager.Utils;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CrossPlatformDownloadManager.Data.Services.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    #region Private Fields

    private readonly DownloadManagerDbContext? _dbContext;

    #endregion

    #region Properties

    public ICategoryHeaderRepository CategoryHeaderRepository { get; }
    public ICategoryRepository CategoryRepository { get; }
    public ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }
    public IDownloadQueueRepository DownloadQueueRepository { get; }
    public ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }
    public IDownloadFileRepository DownloadFileRepository { get; }
    public ISettingsRepository SettingsRepository { get; }
    public IProxySettingsRepository ProxySettingsRepository { get; }

    #endregion

    public UnitOfWork()
    {
        _dbContext = new DownloadManagerDbContext();

        CategoryHeaderRepository = new CategoryHeaderRepository(_dbContext!);
        CategoryRepository = new CategoryRepository(_dbContext!);
        CategoryFileExtensionRepository = new CategoryFileExtensionRepository(_dbContext!);
        CategorySaveDirectoryRepository = new CategorySaveDirectoryRepository(_dbContext!);
        DownloadQueueRepository = new DownloadQueueRepository(_dbContext!);
        DownloadFileRepository = new DownloadFileRepository(_dbContext!);
        SettingsRepository = new SettingsRepository(_dbContext!);
        ProxySettingsRepository = new ProxySettingsRepository(_dbContext!);
    }

    public async Task SaveAsync()
    {
        if (_dbContext == null)
            return;

        await _dbContext.SaveChangesAsync();
    }

    public async Task CreateCategoriesAsync()
    {
        var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/category-headers.json";
        var assetsUri = new Uri(assetName);
        var categoryHeaders = assetsUri.OpenJsonAsset<List<CategoryHeader>>();

        if (categoryHeaders == null || categoryHeaders.Count == 0)
            throw new Exception("Can't find categories for import.");

        foreach (var categoryHeader in categoryHeaders)
        {
            var categoryHeaderInDb = await CategoryHeaderRepository
                .GetAsync(where: ch => ch.Title.ToLower() == categoryHeader.Title.ToLower());

            if (categoryHeaderInDb == null)
                await CategoryHeaderRepository.AddAsync(categoryHeader);
        }

        await SaveAsync();

        assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/categories.json";
        assetsUri = new Uri(assetName);
        var categories = assetsUri.OpenJsonAsset<List<Category>>();

        if (categories == null || categories.Count == 0)
            throw new Exception("Can't find category items for import.");

        foreach (var category in categories)
        {
            var fileExtensions = category.FileExtensions.ToList();
            category.FileExtensions.Clear();

            var categoryInDb = await CategoryRepository
                .GetAsync(where: c => c.Title.ToLower() == category.Title.ToLower());

            if (categoryInDb == null)
            {
                await CategoryRepository.AddAsync(category);
                await SaveAsync();

                categoryInDb = category;
            }

            await CreateFileTypesAsync(categoryInDb.Id, fileExtensions);
            await CreateSaveDirectoryAsync(categoryInDb);
        }
    }

    public async Task CreateDatabaseAsync()
    {
        try
        {
            if (_dbContext == null)
                return;

            // await _dbContext.Database.EnsureCreatedAsync();
            var migrations = await _dbContext.Database.GetPendingMigrationsAsync();
            if (migrations.Any())
                await _dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating the database. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region Helpers

    private async Task CreateFileTypesAsync(int categoryId, List<CategoryFileExtension> extensions)
    {
        var fileExtensions = await CategoryFileExtensionRepository
            .GetAllAsync(where: fe => fe.CategoryId == categoryId);

        if (fileExtensions.Count == 0)
        {
            fileExtensions = extensions
                .Select(fe =>
                {
                    fe.CategoryId = categoryId;
                    return fe;
                })
                .ToList();
        }
        else
        {
            var extensionsInDb = fileExtensions
                .Select(fe => fe.Extension.ToLower())
                .ToList();

            fileExtensions = extensions
                .Where(fe => !extensionsInDb.Contains(fe.Extension.ToLower()))
                .ToList();
        }

        await CategoryFileExtensionRepository.AddRangeAsync(fileExtensions);
        await SaveAsync();
    }

    private async Task CreateSaveDirectoryAsync(Category category)
    {
        var saveDirectory = await CategorySaveDirectoryRepository
            .GetAsync(where: sd => sd.CategoryId == category.Id);

        if (saveDirectory != null)
            return;

        var downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savePath = !category.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(downloadFolderPath, "Downloads", "CDM", category.Title)
            : Path.Combine(downloadFolderPath, "Downloads", "CDM");

        saveDirectory = new CategorySaveDirectory
        {
            CategoryId = category.Id,
            SaveDirectory = savePath,
        };

        await CategorySaveDirectoryRepository.AddAsync(saveDirectory);
        await SaveAsync();

        category.CategorySaveDirectoryId = saveDirectory.Id;
        await CategoryRepository.UpdateAsync(category);
        await SaveAsync();
    }

    #endregion
}
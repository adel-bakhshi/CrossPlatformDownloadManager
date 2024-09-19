using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using CrossPlatformDownloadManager.Data.Services.Repository.Services;
using CrossPlatformDownloadManager.Utils;
using Microsoft.EntityFrameworkCore;

namespace CrossPlatformDownloadManager.Data.UnitOfWork;

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

    #endregion

    public UnitOfWork()
    {
        _dbContext = new DownloadManagerDbContext();
        CreateDatabaseAsync().GetAwaiter().GetResult();

        CategoryHeaderRepository = new CategoryHeaderRepository(_dbContext!);
        CategoryRepository = new CategoryRepository(_dbContext!);
        CategoryFileExtensionRepository = new CategoryFileExtensionRepository(_dbContext!);
        CategorySaveDirectoryRepository = new CategorySaveDirectoryRepository(_dbContext!);
        DownloadQueueRepository = new DownloadQueueRepository(_dbContext!);
        DownloadFileRepository = new DownloadFileRepository(_dbContext!);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
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

            if (categoryInDb != null)
                continue;
            
            await CategoryRepository.AddAsync(category);
            await SaveAsync();

            await CreateFileTypesAsync(category.Id, fileExtensions);
            await CreateSaveDirectoryAsync(category);
        }
    }

    private async Task CreateFileTypesAsync(int categoryId, List<CategoryFileExtension> extensions)
    {
        var fileExtensions = await CategoryFileExtensionRepository
            .GetAllAsync(where: fe => fe.CategoryId == categoryId);
        
        if (fileExtensions.Count > 0)
            CategoryFileExtensionRepository.DeleteAll(fileExtensions);

        fileExtensions = extensions
            .Select(fe =>
            {
                fe.CategoryId = categoryId;
                return fe;
            })
            .ToList();

        await CategoryFileExtensionRepository.AddRangeAsync(fileExtensions);
        await SaveAsync();
    }

    private async Task CreateSaveDirectoryAsync(Category category)
    {
        var downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savePath = !category.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(downloadFolderPath, "Downloads", "CDM", category.Title)
            : Path.Combine(downloadFolderPath, "Downloads", "CDM");

        var saveDirectory = new CategorySaveDirectory
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

    private async Task CreateDatabaseAsync()
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
            Console.WriteLine(ex);
        }
    }
}
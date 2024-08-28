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

    public async Task CreateCategoriesAsync()
    {
        var categoryHeaders = await CategoryHeaderRepository.GetAllAsync();
        var categories = await CategoryRepository.GetAllAsync();

        if (categoryHeaders.Count == 0)
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/category-headers.json";
            var assetsUri = new Uri(assetName);
            categoryHeaders = assetsUri.OpenJsonAsset<List<CategoryHeader>>();

            if (categoryHeaders == null || !categoryHeaders.Any())
                throw new Exception("Can't find categories for import.");

            await CategoryHeaderRepository.AddRangeAsync(categoryHeaders);
            await SaveAsync();
        }

        if (categories.Count == 0)
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/categories.json";
            var assetsUri = new Uri(assetName);
            categories = assetsUri.OpenJsonAsset<List<Category>>();

            if (categories == null || !categories.Any())
                throw new Exception("Can't find category items for import.");

            // Create root directory for General category
            await CreateSaveDirectoryAsync(null);
            await SaveAsync();

            foreach (var category in categories)
            {
                var fileExtensions = category.FileExtensions.ToList();
                category.FileExtensions.Clear();
                
                await CategoryRepository.AddAsync(category);
                await SaveAsync();
                
                await CreateFileTypesAsync(category.Id, fileExtensions);
                await CreateSaveDirectoryAsync(category);
            }
        }
    }

    public async Task SaveAsync()
    {
        if (_dbContext == null)
            return;
        
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

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

            await CategoryFileExtensionRepository.AddRangeAsync(fileExtensions);
            await SaveAsync();
        }
    }

    private async Task CreateSaveDirectoryAsync(Category? category)
    {
        var downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savePath = category != null
            ? Path.Join(downloadFolderPath, "Downloads", "CDM", category.Title)
            : Path.Join(downloadFolderPath, "Downloads", "CDM");

        var saveDirectory = new CategorySaveDirectory
        {
            CategoryId = category?.Id,
            SaveDirectory = savePath,
        };

        await CategorySaveDirectoryRepository.AddAsync(saveDirectory);
        await SaveAsync();

        if (category == null)
            return;

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
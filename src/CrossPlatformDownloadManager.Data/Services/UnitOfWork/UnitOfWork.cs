using CrossPlatformDownloadManager.Data.DbContext;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;
using CrossPlatformDownloadManager.Data.Services.Repository.Services;
using CrossPlatformDownloadManager.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Serilog;

namespace CrossPlatformDownloadManager.Data.Services.UnitOfWork;

/// <summary>
/// Unit of Work implementation for managing database transactions and repositories.
/// Implements the IUnitOfWork interface to provide a consistent way to interact with data access operations.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    #region Private Fields

    /// <summary>
    /// The database context for the Download Manager.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class.
    /// Creates a new database context and initializes all repositories.
    /// </summary>
    public UnitOfWork()
    {
        _dbContext = new DownloadManagerDbContext();

        CategoryHeaderRepository = new CategoryHeaderRepository(_dbContext);
        CategoryRepository = new CategoryRepository(_dbContext);
        CategoryFileExtensionRepository = new CategoryFileExtensionRepository(_dbContext);
        CategorySaveDirectoryRepository = new CategorySaveDirectoryRepository(_dbContext);
        DownloadQueueRepository = new DownloadQueueRepository(_dbContext);
        DownloadFileRepository = new DownloadFileRepository(_dbContext);
        SettingsRepository = new SettingsRepository(_dbContext);
        ProxySettingsRepository = new ProxySettingsRepository(_dbContext);

        Log.Information("UnitOfWork initialized successfully.");
    }

    /// <summary>
    /// Asynchronously saves all changes made in this context to the database.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveAsync()
    {
        if (_dbContext == null)
            return;

        Log.Debug("Saving changes to the database...");
        await _dbContext.SaveChangesAsync();
    }

    public async Task CreateDatabaseAsync()
    {
        try
        {
            if (_dbContext == null)
                return;

            Log.Debug("Creating database...");

            // Get all pending migrations and apply them
            var migrations = await _dbContext.Database.GetPendingMigrationsAsync();
            if (migrations.Any())
            {
                Log.Debug("Some migrations are pending. Applying them...");
                await _dbContext.Database.MigrateAsync();
            }

            Log.Information("Database created successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating the database. Error message: {ErrorMessage}", ex.Message);
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when category data cannot be loaded from assets.</exception>
    public async Task CreateCategoriesAsync()
    {
        // Load category headers from assets
        Log.Debug("Loading category headers from assets...");
        var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/category-headers.json";
        var assetsUri = new Uri(assetName);
        var categoryHeaders = await assetsUri.OpenJsonAssetAsync<List<CategoryHeader>>();

        // Check if category headers are loaded successfully
        if (categoryHeaders == null || categoryHeaders.Count == 0)
        {
            const string message = "Can't find category headers for import.";
            Log.Fatal("{Message}", message);
            throw new InvalidOperationException(message);
        }

        Log.Debug("Adding category headers to the database if they don't exist...");

        // Add category headers to the database if they don't exist
        foreach (var categoryHeader in categoryHeaders)
        {
            var categoryHeaderInDb = await CategoryHeaderRepository
                .GetAsync(where: ch => ch.Title.ToLower() == categoryHeader.Title.ToLower());

            if (categoryHeaderInDb == null)
                await CategoryHeaderRepository.AddAsync(categoryHeader);
        }

        await SaveAsync();

        // Load categories from assets
        Log.Debug("Loading categories from assets...");
        assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/categories.json";
        assetsUri = new Uri(assetName);
        var categories = await assetsUri.OpenJsonAssetAsync<List<Category>>();

        // Check if categories are loaded successfully
        if (categories == null || categories.Count == 0)
        {
            const string message = "Can't find categories for import.";
            Log.Fatal("{Message}", message);
            throw new InvalidOperationException(message);
        }

        Log.Debug("Adding categories to the database if they don't exist...");

        foreach (var category in categories)
        {
            var fileExtensions = category.FileExtensions.ToList();
            category.FileExtensions.Clear();

            var categoryInDb = await CategoryRepository
                .GetAsync(where: c => c.Title.ToLower() == category.Title.ToLower());

            if (categoryInDb == null)
            {
                Log.Debug("Category {CategoryTitle} doesn't exist. Creating it...", category.Title);

                await CategoryRepository.AddAsync(category);
                await SaveAsync();

                categoryInDb = category;
            }

            // Create file extensions and save directory for category
            await CreateFileTypesAsync(categoryInDb.Id, fileExtensions);
            await CreateSaveDirectoryAsync(categoryInDb);
        }

        Log.Information("Categories created successfully.");
    }

    public async Task<IDbContextTransaction?> BeginTransactionAsync()
    {
        if (_dbContext == null)
            return null;

        return await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_dbContext == null)
            return;

        await _dbContext.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        if (_dbContext == null)
            return;

        await _dbContext.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helpers

    /// <summary>
    /// Creates or updates file extensions for a specific category.
    /// </summary>
    /// <param name="categoryId">The ID of the category.</param>
    /// <param name="extensions">The list of file extensions to create or update.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CreateFileTypesAsync(int categoryId, List<CategoryFileExtension> extensions)
    {
        Log.Debug("Creating or updating file extensions for category {CategoryId}...", categoryId);

        var fileExtensions = await CategoryFileExtensionRepository
            .GetAllAsync(where: fe => fe.CategoryId == categoryId);

        if (fileExtensions.Count == 0)
        {
            fileExtensions = extensions
                .ConvertAll(fe =>
                {
                    fe.CategoryId = categoryId;
                    return fe;
                });
        }
        else
        {
            var extensionsInDb = fileExtensions.ConvertAll(fe => fe.Extension.ToLower());

            fileExtensions = extensions
                .Where(fe => !extensionsInDb.Contains(fe.Extension.ToLower()))
                .Select(fe =>
                {
                    fe.CategoryId = categoryId;
                    return fe;
                })
                .ToList();
        }

        await CategoryFileExtensionRepository.AddRangeAsync(fileExtensions);
        await SaveAsync();

        Log.Debug("File extensions created successfully for category {CategoryId}.", categoryId);
    }

    /// <summary>
    /// Creates a default save directory for a category.
    /// Uses a structured path under the user's downloads folder.
    /// </summary>
    /// <param name="category">The category for which to create the save directory.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CreateSaveDirectoryAsync(Category category)
    {
        Log.Debug("Creating save directory for category {CategoryTitle}", category.Title);

        var saveDirectory = await CategorySaveDirectoryRepository
            .GetAsync(where: sd => sd.CategoryId == category.Id);

        if (saveDirectory != null)
        {
            Log.Debug("Save directory already exists for category {CategoryTitle}.", category.Title);
            return;
        }

        Log.Debug("Creating save directory for category {CategoryTitle}...", category.Title);

        var downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savePath = !category.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(downloadFolderPath, "Downloads", "CDM", category.Title)
            : Path.Combine(downloadFolderPath, "Downloads", "CDM");

        Log.Debug("Save directory path for category {CategoryTitle}: {SavePath}", category.Title, savePath);
        Log.Debug("Adding save directory for category {CategoryTitle}...", category.Title);

        saveDirectory = new CategorySaveDirectory { CategoryId = category.Id, SaveDirectory = savePath };
        await CategorySaveDirectoryRepository.AddAsync(saveDirectory);
        await SaveAsync();

        Log.Debug("Updating category {CategoryTitle} with save directory...", category.Title);

        await CategoryRepository.UpdateAsync(category);
        await SaveAsync();

        Log.Debug("Save directory created successfully for category {CategoryTitle}.", category.Title);
    }

    #endregion
}
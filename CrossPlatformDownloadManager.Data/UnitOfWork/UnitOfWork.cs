using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Repository.Interfaces;
using CrossPlatformDownloadManager.Data.Repository.Services;
using CrossPlatformDownloadManager.Utils;
using SQLite;

namespace CrossPlatformDownloadManager.Data.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    #region Private Fields

    private readonly SQLiteConnection? _connection;

    #endregion

    #region Properties

    public ICategoryRepository CategoryRepository { get; }
    public ICategoryItemRepository CategoryItemRepository { get; }
    public ICategoryItemFileExtensionRepository CategoryItemFileExtensionRepository { get; }
    public IQueueRepository QueueRepository { get; }
    public ICategoryItemSaveDirectoryRepository CategoryItemSaveDirectoryRepository { get; }

    #endregion

    public UnitOfWork()
    {
        _connection ??= CreateSqLiteConnection();

        CategoryRepository = new CategoryRepository(_connection);
        CategoryItemRepository = new CategoryItemRepository(_connection);
        CategoryItemFileExtensionRepository = new CategoryItemFileExtensionRepository(_connection);
        QueueRepository = new QueueRepository(_connection);
        CategoryItemSaveDirectoryRepository = new CategoryItemSaveDirectoryRepository(_connection);
    }

    private SQLiteConnection CreateSqLiteConnection()
    {
        var dbPath = Path.Join(Environment.CurrentDirectory, "ApplicationData.db");
        return new SQLiteConnection(dbPath);
    }

    /// <summary>
    /// It adds the categories that are in the categories.json and category-item.json files to the database. Check CrossPlatformDownloadManager.DesktopApp/Assets folder
    /// </summary>
    /// <exception cref="Exception">If the json files is empty</exception>
    public void CreateCategories()
    {
        var categories = CategoryRepository.GetAll();
        var categoryItems = CategoryItemRepository.GetAll();

        if (categories.Count == 0)
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/categories.json";
            var assetsUri = new Uri(assetName);
            categories = assetsUri.OpenJsonAsset<List<Category>>();

            if (categories == null || !categories.Any())
                throw new Exception("Can't find categories for import.");

            CategoryRepository.AddRange(categories);
        }

        if (categoryItems.Count == 0)
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/category-items.json";
            var assetsUri = new Uri(assetName);
            categoryItems = assetsUri.OpenJsonAsset<List<CategoryItem>>();

            if (categoryItems == null || !categoryItems.Any())
                throw new Exception("Can't find category items for import.");

            CreateSaveDirectory(null);

            foreach (var categoryItem in categoryItems)
            {
                CategoryItemRepository.Add(categoryItem);
                CreateFileTypes(categoryItem);
                CreateSaveDirectory(categoryItem);
            }
        }
    }

    /// <summary>
    /// Each category has one or more file types that must be created for each
    /// </summary>
    /// <param name="categoryItem">CategoryItem, whose types of files are supposed to be created</param>
    private void CreateFileTypes(CategoryItem categoryItem)
    {
        var fileExtensions = CategoryItemFileExtensionRepository
            .GetAll(where: fe => fe.CategoryItemId == categoryItem.Id);

        if (fileExtensions.Count == 0)
        {
            fileExtensions = categoryItem
                .FileExtensions
                .Select(fe =>
                {
                    fe.CategoryItemId = categoryItem.Id;
                    return fe;
                })
                .ToList();

            CategoryItemFileExtensionRepository.AddRange(fileExtensions);
        }
    }

    /// <summary>
    /// Define default download directories for each category. If category is null, It's create General directory path
    /// </summary>
    /// <param name="categoryItem">CategoryItem whose directory path is to be created</param>
    private void CreateSaveDirectory(CategoryItem? categoryItem)
    {
        var downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savePath = categoryItem != null
            ? Path.Join(downloadFolderPath, "Downloads", "CDM", categoryItem.Title)
            : Path.Join(downloadFolderPath, "Downloads", "CDM");

        var saveDirectory = new CategoryItemSaveDirectory
        {
            CategoryItemId = categoryItem?.Id,
            SaveDirectory = savePath,
        };

        CategoryItemSaveDirectoryRepository.Add(saveDirectory);

        if (categoryItem == null)
            return;

        categoryItem.CategoryItemSaveDirectoryId = saveDirectory.Id;
        CategoryItemRepository.Update(categoryItem);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
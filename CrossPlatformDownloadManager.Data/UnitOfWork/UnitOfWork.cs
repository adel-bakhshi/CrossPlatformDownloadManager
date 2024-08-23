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

    public ICategoryHeaderRepository CategoryHeaderRepository { get; }
    public ICategoryRepository CategoryRepository { get; }
    public ICategoryFileExtensionRepository CategoryFileExtensionRepository { get; }
    public IDownloadQueueRepository DownloadQueueRepository { get; }
    public ICategorySaveDirectoryRepository CategorySaveDirectoryRepository { get; }

    #endregion

    public UnitOfWork()
    {
        _connection ??= CreateSqLiteConnection();

        CategoryHeaderRepository = new CategoryHeaderRepository(_connection);
        CategoryRepository = new CategoryRepository(_connection);
        CategoryFileExtensionRepository = new CategoryFileExtensionRepository(_connection);
        CategorySaveDirectoryRepository = new CategorySaveDirectoryRepository(_connection);
        DownloadQueueRepository = new DownloadQueueRepository(_connection);
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
        var categories = CategoryHeaderRepository.GetAll();
        var categoryItems = CategoryRepository.GetAll();

        if (categories.Count == 0)
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/categories.json";
            var assetsUri = new Uri(assetName);
            categories = assetsUri.OpenJsonAsset<List<CategoryHeader>>();

            if (categories == null || !categories.Any())
                throw new Exception("Can't find categories for import.");

            CategoryHeaderRepository.AddRange(categories);
        }

        if (categoryItems.Count == 0)
        {
            var assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/category-items.json";
            var assetsUri = new Uri(assetName);
            categoryItems = assetsUri.OpenJsonAsset<List<Category>>();

            if (categoryItems == null || !categoryItems.Any())
                throw new Exception("Can't find category items for import.");

            CreateSaveDirectory(null);

            foreach (var categoryItem in categoryItems)
            {
                CategoryRepository.Add(categoryItem);
                CreateFileTypes(categoryItem);
                CreateSaveDirectory(categoryItem);
            }
        }
    }

    /// <summary>
    /// Each category has one or more file types that must be created for each
    /// </summary>
    /// <param name="category">CategoryItem, whose types of files are supposed to be created</param>
    private void CreateFileTypes(Category category)
    {
        var fileExtensions = CategoryFileExtensionRepository
            .GetAll(where: fe => fe.CategoryId == category.Id);

        if (fileExtensions.Count == 0)
        {
            fileExtensions = category
                .FileExtensions
                .Select(fe =>
                {
                    fe.CategoryId = category.Id;
                    return fe;
                })
                .ToList();

            CategoryFileExtensionRepository.AddRange(fileExtensions);
        }
    }

    /// <summary>
    /// Define default download directories for each category. If category is null, It's create General directory path
    /// </summary>
    /// <param name="categoryItem">CategoryItem whose directory path is to be created</param>
    private void CreateSaveDirectory(Category? categoryItem)
    {
        var downloadFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var savePath = categoryItem != null
            ? Path.Join(downloadFolderPath, "Downloads", "CDM", categoryItem.Title)
            : Path.Join(downloadFolderPath, "Downloads", "CDM");

        var saveDirectory = new CategorySaveDirectory
        {
            CategoryId = categoryItem?.Id,
            SaveDirectory = savePath,
        };

        CategorySaveDirectoryRepository.Add(saveDirectory);

        if (categoryItem == null)
            return;

        categoryItem.CategoryItemSaveDirectoryId = saveDirectory.Id;
        CategoryRepository.Update(categoryItem);
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
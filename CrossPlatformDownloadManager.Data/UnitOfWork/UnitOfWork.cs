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

    #endregion

    public UnitOfWork()
    {
        _connection ??= CreateSqLiteConnection();

        CategoryRepository = new CategoryRepository(_connection);
        CategoryItemRepository = new CategoryItemRepository(_connection);
        CategoryItemFileExtensionRepository = new CategoryItemFileExtensionRepository(_connection);
        QueueRepository = new QueueRepository(_connection);
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

            foreach (var categoryItem in categoryItems)
            {
                CategoryItemRepository.Add(categoryItem);
                CreateFileTypes(categoryItem);
            }
        }
    }

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

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
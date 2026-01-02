using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class CategorySaveDirectoryViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private int? _categoryId;
    private CategoryViewModel? _category;
    private string _saveDirectory = string.Empty;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public int? CategoryId
    {
        get => _categoryId;
        set => SetField(ref _categoryId, value);
    }
    
    public CategoryViewModel? Category
    {
        get => _category;
        set => SetField(ref _category, value);
    }
    
    public string SaveDirectory
    {
        get => _saveDirectory;
        set => SetField(ref _saveDirectory, value);
    }

    #endregion
}
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class CategoryFileExtensionViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string _extension = string.Empty;
    private string _alias = string.Empty;
    private int? _categoryId;
    private CategoryViewModel? _category;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Extension
    {
        get => _extension;
        set => SetField(ref _extension, value);
    }

    public string Alias
    {
        get => _alias;
        set => SetField(ref _alias, value);
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

    #endregion
}
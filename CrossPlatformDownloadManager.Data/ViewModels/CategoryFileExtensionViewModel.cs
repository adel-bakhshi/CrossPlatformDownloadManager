using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class CategoryFileExtensionViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string? _extension;
    private string? _alias;
    private string? _categoryTitle;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string? Extension
    {
        get => _extension;
        set => SetField(ref _extension, value);
    }

    public string? Alias
    {
        get => _alias;
        set => SetField(ref _alias, value);
    }

    public string? CategoryTitle
    {
        get => _categoryTitle;
        set => SetField(ref _categoryTitle, value);
    }

    #endregion
}
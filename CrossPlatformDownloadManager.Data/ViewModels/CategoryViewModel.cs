using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class CategoryViewModel : PropertyChangedBase
{
    #region Private Fields

    private int? _id;
    private string? _title;
    private string? _categorySaveDirectory;

    #endregion

    #region Properties

    public int? Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string? Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string? CategorySaveDirectory
    {
        get => _categorySaveDirectory;
        set => SetField(ref _categorySaveDirectory, value);
    }

    #endregion
}
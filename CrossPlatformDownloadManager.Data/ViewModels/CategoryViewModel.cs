using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class CategoryViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string _title = string.Empty;
    private string _icon = string.Empty;
    private bool _isDefault;
    private string? _autoAddLinkFromSites;
    private int? _categorySaveDirectoryId;
    private CategorySaveDirectoryViewModel? _categorySaveDirectory;
    private ObservableCollection<CategoryFileExtensionViewModel> _fileExtensions = [];

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string Icon
    {
        get => _icon;
        set => SetField(ref _icon, value);
    }

    public bool IsDefault
    {
        get => _isDefault;
        set => SetField(ref _isDefault, value);
    }

    public string? AutoAddLinkFromSites
    {
        get => _autoAddLinkFromSites;
        set
        {
            var isChanged = SetField(ref _autoAddLinkFromSites, value);
            if (isChanged)
                OnPropertyChanged(nameof(AutoAddedLinksFromSitesList));
        }
    }

    public List<string> AutoAddedLinksFromSitesList => AutoAddLinkFromSites.IsNullOrEmpty() ? [] : AutoAddLinkFromSites.ConvertFromJson<List<string>?>() ?? [];

    public int? CategorySaveDirectoryId
    {
        get => _categorySaveDirectoryId;
        set => SetField(ref _categorySaveDirectoryId, value);
    }

    public CategorySaveDirectoryViewModel? CategorySaveDirectory
    {
        get => _categorySaveDirectory;
        set => SetField(ref _categorySaveDirectory, value);
    }

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => _fileExtensions;
        set => SetField(ref _fileExtensions, value);
    }

    #endregion
}
using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class CategoryHeaderViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string _title = string.Empty;
    private string _icon = string.Empty;
    private ObservableCollection<CategoryViewModel> _categories = [];

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

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => SetField(ref _categories, value);
    }

    #endregion
}
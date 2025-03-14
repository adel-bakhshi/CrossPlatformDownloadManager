using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.MainWindow;

public class CategoriesTreeItemViewModel : ViewModelBase
{
    #region Private Fields

    private string? _title;
    private string? _icon;
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private bool _showCategories;

    #endregion

    #region Properties

    public int Id { get; set; }

    public string? Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }
    
    public string? Icon
    {
        get => _icon;
        set => this.RaiseAndSetIfChanged(ref _icon, value);
    }
    
    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }
    
    public bool ShowCategories
    {
        get => _showCategories;
        set => this.RaiseAndSetIfChanged(ref _showCategories, value);
    }

    #endregion
    
    public CategoriesTreeItemViewModel(IAppService appService) : base(appService)
    {
        ShowHideCategories();
    }

    private void ShowHideCategories()
    {
        ShowCategories = !AppService.SettingsService.Settings.DisableCategories;
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        ShowHideCategories();
    }
}
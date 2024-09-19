using System.Collections.ObjectModel;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class SaveLocationsViewModel : ViewModelBase
{
    #region Properties

    private ObservableCollection<CategoryViewModel> _categories = [];

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    private CategoryViewModel? _selectedCategory;

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }
    
    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }
    
    private ObservableCollection<CategoryFileExtensionViewModel> _fileExtensions = [];

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => _fileExtensions;
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    private CategoryFileExtensionViewModel? _selectedFileExtension;

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => _selectedFileExtension;
        set => this.RaiseAndSetIfChanged(ref _selectedFileExtension, value);
    }
    
    private string? _saveLocation;

    public string? SaveLocation
    {
        get => _saveLocation;
        set => this.RaiseAndSetIfChanged(ref _saveLocation, value);
    }

    #endregion

    #region Commands
    
    public ICommand? AddNewCategoryCommand { get; set; }
    
    public ICommand? EditCategoryCommand { get; set; }

    public ICommand? DeleteCategoryCommand { get; set; }
    
    public ICommand? AddNewFileTypeCommand { get; set; }
    
    public ICommand? EditFileTypeCommand { get; set; }
    
    public ICommand? DeleteFileTypeCommand { get; set; }

    #endregion
    
    public SaveLocationsViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(unitOfWork, downloadFileService)
    {
        AddNewCategoryCommand = ReactiveCommand.Create(AddNewCategory);
        EditCategoryCommand = ReactiveCommand.Create(EditCategory);
        DeleteCategoryCommand = ReactiveCommand.Create(DeleteCategory);
        AddNewFileTypeCommand = ReactiveCommand.Create(AddNewFileType);
        EditFileTypeCommand = ReactiveCommand.Create(EditFileType);
        DeleteFileTypeCommand = ReactiveCommand.Create(DeleteFileType);
    }

    private void AddNewCategory()
    {
        throw new System.NotImplementedException();
    }

    private void EditCategory()
    {
        throw new System.NotImplementedException();
    }

    private void DeleteCategory()
    {
        throw new System.NotImplementedException();
    }

    private void AddNewFileType()
    {
        throw new System.NotImplementedException();
    }

    private void EditFileType()
    {
        throw new System.NotImplementedException();
    }

    private void DeleteFileType()
    {
        throw new System.NotImplementedException();
    }
}
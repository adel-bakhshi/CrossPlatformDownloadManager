using System.Collections.ObjectModel;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class FileTypesViewModel : ViewModelBase
{
    #region Properties

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

    #endregion

    #region Commands

    public ICommand? AddNewFileTypeCommand { get; set; }
    
    public ICommand? EditFileTypeCommand { get; set; }
    
    public ICommand? DeleteFileTypeCommand { get; set; }

    #endregion
    
    public FileTypesViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(unitOfWork, downloadFileService)
    {
        AddNewFileTypeCommand = ReactiveCommand.Create(AddNewFileType);
        EditFileTypeCommand = ReactiveCommand.Create(EditFileType);
        DeleteFileTypeCommand = ReactiveCommand.Create(DeleteFileType);
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
using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class DownloadsViewModel : ViewModelBase
{
    #region Properties

    private bool _showStartDownloadDialog;

    public bool ShowStartDownloadDialog
    {
        get => _showStartDownloadDialog;
        set => this.RaiseAndSetIfChanged(ref _showStartDownloadDialog, value);
    }
    
    private bool _showCompleteDownloadDialog;

    public bool ShowCompleteDownloadDialog
    {
        get => _showCompleteDownloadDialog;
        set => this.RaiseAndSetIfChanged(ref _showCompleteDownloadDialog, value);
    }

    private ObservableCollection<string> _duplicateDownloadLinkActions = [];

    public ObservableCollection<string> DuplicateDownloadLinkActions
    {
        get => _duplicateDownloadLinkActions;
        set => this.RaiseAndSetIfChanged(ref _duplicateDownloadLinkActions, value);
    }

    private string? _selectedDuplicateDownloadLinkAction;

    public string? SelectedDuplicateDownloadLinkAction
    {
        get => _selectedDuplicateDownloadLinkAction;
        set => this.RaiseAndSetIfChanged(ref _selectedDuplicateDownloadLinkAction, value);
    }

    #endregion
    
    public DownloadsViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(unitOfWork, downloadFileService)
    {
    }
}
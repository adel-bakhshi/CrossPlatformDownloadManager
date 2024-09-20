using System.Collections.ObjectModel;
using System.Linq;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Utils;
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

    private ObservableCollection<int> _maximumConnectionsCount = [];

    public ObservableCollection<int> MaximumConnectionsCount
    {
        get => _maximumConnectionsCount;
        set => this.RaiseAndSetIfChanged(ref _maximumConnectionsCount, value);
    }

    private int _selectedMaximumConnectionsCount;

    public int SelectedMaximumConnectionsCount
    {
        get => _selectedMaximumConnectionsCount;
        set => this.RaiseAndSetIfChanged(ref _selectedMaximumConnectionsCount, value);
    }

    #endregion

    public DownloadsViewModel(IAppService appService) : base(appService)
    {
        DuplicateDownloadLinkActions = Constants.DuplicateDownloadLinkActions.ToObservableCollection();
        SelectedDuplicateDownloadLinkAction = DuplicateDownloadLinkActions.FirstOrDefault();
        MaximumConnectionsCount = Constants.MaximumConnectionsCounts.ToObservableCollection();
        SelectedMaximumConnectionsCount = MaximumConnectionsCount.FirstOrDefault();
    }
}
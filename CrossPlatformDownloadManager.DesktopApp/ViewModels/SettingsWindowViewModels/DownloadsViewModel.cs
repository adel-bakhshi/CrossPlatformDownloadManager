using System.Collections.ObjectModel;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class DownloadsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _showStartDownloadDialog;
    private bool _showCompleteDownloadDialog;
    private ObservableCollection<string> _duplicateDownloadLinkActions = [];
    private string? _selectedDuplicateDownloadLinkAction;
    private ObservableCollection<int> _maximumConnectionsCount = [];
    private int _selectedMaximumConnectionsCount;
    private bool _isSpeedLimiterEnabled;
    private double? _speedLimit;
    private ObservableCollection<string> _speedUnits = [];
    private string? _selectedSpeedUnit;
    private string? _speedLimitInfo;

    #endregion

    #region Properties

    public bool ShowStartDownloadDialog
    {
        get => _showStartDownloadDialog;
        set => this.RaiseAndSetIfChanged(ref _showStartDownloadDialog, value);
    }

    public bool ShowCompleteDownloadDialog
    {
        get => _showCompleteDownloadDialog;
        set => this.RaiseAndSetIfChanged(ref _showCompleteDownloadDialog, value);
    }

    public ObservableCollection<string> DuplicateDownloadLinkActions
    {
        get => _duplicateDownloadLinkActions;
        set => this.RaiseAndSetIfChanged(ref _duplicateDownloadLinkActions, value);
    }

    public string? SelectedDuplicateDownloadLinkAction
    {
        get => _selectedDuplicateDownloadLinkAction;
        set => this.RaiseAndSetIfChanged(ref _selectedDuplicateDownloadLinkAction, value);
    }

    public ObservableCollection<int> MaximumConnectionsCount
    {
        get => _maximumConnectionsCount;
        set => this.RaiseAndSetIfChanged(ref _maximumConnectionsCount, value);
    }

    public int SelectedMaximumConnectionsCount
    {
        get => _selectedMaximumConnectionsCount;
        set => this.RaiseAndSetIfChanged(ref _selectedMaximumConnectionsCount, value);
    }

    public bool IsSpeedLimiterEnabled
    {
        get => _isSpeedLimiterEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isSpeedLimiterEnabled, value);
            ChangeSpeedLimitInfo();
        }
    }

    public double? SpeedLimit
    {
        get => _speedLimit;
        set
        {
            this.RaiseAndSetIfChanged(ref _speedLimit, value);
            ChangeSpeedLimitInfo();
        }
    }

    public ObservableCollection<string> SpeedUnits
    {
        get => _speedUnits;
        set => this.RaiseAndSetIfChanged(ref _speedUnits, value);
    }

    public string? SelectedSpeedUnit
    {
        get => _selectedSpeedUnit;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSpeedUnit, value);
            ChangeSpeedLimitInfo();
        }
    }

    public string? SpeedLimitInfo
    {
        get => _speedLimitInfo;
        set => this.RaiseAndSetIfChanged(ref _speedLimitInfo, value);
    }

    #endregion

    public DownloadsViewModel(IAppService appService) : base(appService)
    {
        DuplicateDownloadLinkActions = Constants.GetDuplicateActionsMessages().ToObservableCollection();
        SelectedDuplicateDownloadLinkAction = DuplicateDownloadLinkActions.FirstOrDefault();
        MaximumConnectionsCount = Constants.MaximumConnectionsCountList.ToObservableCollection();
        SelectedMaximumConnectionsCount = MaximumConnectionsCount.FirstOrDefault();
        SpeedUnits = Constants.SpeedLimiterUnits.ToObservableCollection();
        SelectedSpeedUnit = SpeedUnits.FirstOrDefault();

        LoadViewData();
    }

    #region Helpers

    private void LoadViewData()
    {
        var settings = AppService.SettingsService.Settings;
        ShowStartDownloadDialog = settings.ShowStartDownloadDialog;
        ShowCompleteDownloadDialog = settings.ShowCompleteDownloadDialog;
        SelectedDuplicateDownloadLinkAction = Constants.GetDuplicateActionMessage(settings.DuplicateDownloadLinkAction);
        SelectedMaximumConnectionsCount = MaximumConnectionsCount.FirstOrDefault(cc => cc == settings.MaximumConnectionsCount);
        IsSpeedLimiterEnabled = settings.IsSpeedLimiterEnabled;
        SpeedLimit = settings.LimitSpeed;
        SelectedSpeedUnit = SpeedUnits.FirstOrDefault(su => su.Equals(settings.LimitUnit)) ?? SpeedUnits.FirstOrDefault();
    }

    private void ChangeSpeedLimitInfo()
    {
        if (!IsSpeedLimiterEnabled || SpeedLimit == null || SpeedLimit <= 0)
        {
            SpeedLimitInfo = "Global speed limiter is disabled";
            return;
        }

        SpeedLimitInfo = $"Your download speed is limited to a maximum of {SpeedLimit} {SelectedSpeedUnit}/s per file";
    }

    #endregion
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings.Views;

public class DownloadsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _showStartDownloadDialog;
    private bool _showCompleteDownloadDialog;
    private ObservableCollection<string> _duplicateDownloadLinkActions = [];
    private string? _selectedDuplicateDownloadLinkAction;
    private ObservableCollection<int> _maximumConnectionsCount = [];
    private int _selectedMaximumConnectionsCount;
    private ObservableCollection<string> _speedUnits = [];
    private bool _isSpeedLimiterEnabled;
    private double? _speedLimit;
    private string? _selectedSpeedUnit;
    private string? _speedLimitInfo;
    private bool _isMergeSpeedLimiterEnabled;
    private double? _mergeSpeedLimit;
    private string? _selectedMergeSpeedUnit;
    private string? _mergeSpeedLimitInfo;
    private double? _maximumMemoryBufferBytes;
    private string? _selectedMaximumMemoryBufferBytesUnit;
    private string? _temporaryFileLocation;

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

    public ObservableCollection<string> SpeedUnits
    {
        get => _speedUnits;
        set => this.RaiseAndSetIfChanged(ref _speedUnits, value);
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

    public bool IsMergeSpeedLimiterEnabled
    {
        get => _isMergeSpeedLimiterEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isMergeSpeedLimiterEnabled, value);
            ChangeMergeSpeedLimitInfo();
        }
    }

    public double? MergeSpeedLimit
    {
        get => _mergeSpeedLimit;
        set
        {
            this.RaiseAndSetIfChanged(ref _mergeSpeedLimit, value);
            ChangeMergeSpeedLimitInfo();
        }
    }

    public string? SelectedMergeSpeedUnit
    {
        get => _selectedMergeSpeedUnit;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMergeSpeedUnit, value);
            ChangeMergeSpeedLimitInfo();
        }
    }

    public string? MergeSpeedLimitInfo
    {
        get => _mergeSpeedLimitInfo;
        set => this.RaiseAndSetIfChanged(ref _mergeSpeedLimitInfo, value);
    }

    public double? MaximumMemoryBufferBytes
    {
        get => _maximumMemoryBufferBytes;
        set => this.RaiseAndSetIfChanged(ref _maximumMemoryBufferBytes, value);
    }

    public string? SelectedMaximumMemoryBufferBytesUnit
    {
        get => _selectedMaximumMemoryBufferBytesUnit;
        set => this.RaiseAndSetIfChanged(ref _selectedMaximumMemoryBufferBytesUnit, value);
    }

    public string? TemporaryFileLocation
    {
        get => _temporaryFileLocation;
        set => this.RaiseAndSetIfChanged(ref _temporaryFileLocation, value);
    }

    #endregion

    #region Commands

    public ICommand BrowseTemporaryFileLocationCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of <see cref="DownloadsViewModel"/> class.
    /// </summary>
    /// <param name="appService">The application service.</param>
    public DownloadsViewModel(IAppService appService) : base(appService)
    {
        DuplicateDownloadLinkActions = Constants.GetDuplicateActionsMessages().ToObservableCollection();
        SelectedDuplicateDownloadLinkAction = DuplicateDownloadLinkActions.FirstOrDefault();
        MaximumConnectionsCount = Constants.MaximumConnectionsCountList.ToObservableCollection();
        SelectedMaximumConnectionsCount = MaximumConnectionsCount.FirstOrDefault();
        SpeedUnits = Constants.SpeedLimiterUnits.ToObservableCollection();
        SelectedSpeedUnit = SpeedUnits.FirstOrDefault();
        SelectedMergeSpeedUnit = SpeedUnits.FirstOrDefault();

        LoadViewData();

        BrowseTemporaryFileLocationCommand = ReactiveCommand.CreateFromTask(BrowseTemporaryFileLocationAsync);
    }

    #region Command actions

    /// <summary>
    /// Browses the temporary file location.
    /// </summary>
    /// <exception cref="InvalidOperationException">The storage provider is not available.</exception>
    private async Task BrowseTemporaryFileLocationAsync()
    {
        try
        {
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            if (storageProvider == null)
                throw new InvalidOperationException("The storage provider is not available.");

            var options = new FolderPickerOpenOptions
            {
                Title = "Select temporary file location",
                AllowMultiple = false,
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(Constants.TempDownloadDirectory)
            };

            var directories = await storageProvider.OpenFolderPickerAsync(options);
            if (!directories.Any())
                return;

            TemporaryFileLocation = directories[0].Path.IsAbsoluteUri ? directories[0].Path.LocalPath : directories[0].Path.OriginalString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while browsing the temporary file location. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Loads the data of the view.
    /// </summary>
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
        IsMergeSpeedLimiterEnabled = settings.IsMergeSpeedLimitEnabled;
        MergeSpeedLimit = settings.MergeLimitSpeed;
        SelectedMergeSpeedUnit = SpeedUnits.FirstOrDefault(su => su.Equals(settings.MergeLimitUnit)) ?? SpeedUnits.FirstOrDefault();
        MaximumMemoryBufferBytes = settings.MaximumMemoryBufferBytes;
        SelectedMaximumMemoryBufferBytesUnit = SpeedUnits.FirstOrDefault(su => su.Equals(settings.MaximumMemoryBufferBytesUnit)) ?? SpeedUnits.FirstOrDefault();

        var tempLocation = settings.TemporaryFileLocation;
        if (tempLocation.IsStringNullOrEmpty())
            tempLocation = Constants.TempDownloadDirectory;

        TemporaryFileLocation = tempLocation;
    }

    /// <summary>
    /// Changes the speed limit info messages.
    /// </summary>
    private void ChangeSpeedLimitInfo()
    {
        if (!IsSpeedLimiterEnabled || SpeedLimit == null || SpeedLimit <= 0)
        {
            SpeedLimitInfo = "Global speed limiter is disabled";
            return;
        }

        SpeedLimitInfo = $"Your download speed is limited to a maximum of {SpeedLimit} {SelectedSpeedUnit}/s per file";
    }

    /// <summary>
    /// Changes the merge speed limit info messages.
    /// </summary>
    private void ChangeMergeSpeedLimitInfo()
    {
        if (!IsMergeSpeedLimiterEnabled || MergeSpeedLimit == null || MergeSpeedLimit <= 0)
        {
            MergeSpeedLimitInfo = "Merge speed limiter is disabled";
            return;
        }

        MergeSpeedLimitInfo = $"Your merge speed is limited to a maximum of {MergeSpeedLimit} {SelectedMergeSpeedUnit}/s per file";
    }

    #endregion
}
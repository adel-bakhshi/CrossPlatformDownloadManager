using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    // Speed Limiter
    private bool _isSpeedLimiterEnabled;
    private double? _limitSpeed;
    private string? _speedUnit;

    // Options
    private bool _openFolderAfterDownloadFinished;
    private bool _exitProgramAfterDownloadFinished;
    private bool _turnOffComputerAfterDownloadFinished;
    private string? _turnOffComputerMode;

    // Show/Hide details
    private bool _detailsIsVisible = true;
    private double _detailsHeight;

    // Properties
    private ObservableCollection<string> _tabItems = [];
    private string? _selectedTabItem;
    private ObservableCollection<string> _speedLimiterUnits = [];
    private ObservableCollection<string> _optionsTurnOffModes = [];
    private DownloadFileViewModel _downloadFile = new();
    private bool _isPaused;
    private string? _hideDetailsButtonContent;
    private bool _canResumeFile;
    private string _resumeCapabilityState = "Checking...";

    #endregion

    #region Properties

    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
    }

    public ObservableCollection<string> SpeedLimiterUnits
    {
        get => _speedLimiterUnits;
        set => this.RaiseAndSetIfChanged(ref _speedLimiterUnits, value);
    }

    public ObservableCollection<string> OptionsTurnOffModes
    {
        get => _optionsTurnOffModes;
        set => this.RaiseAndSetIfChanged(ref _optionsTurnOffModes, value);
    }

    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set => this.RaiseAndSetIfChanged(ref _downloadFile, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }

    public string? HideDetailsButtonContent
    {
        get => _hideDetailsButtonContent;
        set => this.RaiseAndSetIfChanged(ref _hideDetailsButtonContent, value);
    }

    public bool CanResumeFile
    {
        get => _canResumeFile;
        set => this.RaiseAndSetIfChanged(ref _canResumeFile, value);
    }

    public string ResumeCapabilityState
    {
        get => _resumeCapabilityState;
        set => this.RaiseAndSetIfChanged(ref _resumeCapabilityState, value);
    }

    #endregion

    #region Commands

    public ICommand ResumePauseDownloadCommand { get; }

    public ICommand CancelDownloadCommand { get; }

    public ICommand ShowHideDetailsCommand { get; }

    public ICommand OpenSaveLocationCommand { get; }

    #endregion

    public DownloadWindowViewModel(IAppService appService, DownloadFileViewModel downloadFile) : base(appService)
    {
        DownloadFile = downloadFile;
        SpeedLimiterUnits = Constants.SpeedLimiterUnits.ToObservableCollection();
        OptionsTurnOffModes = Constants.TurnOffComputerModes.ToObservableCollection();

        TabItems =
        [
            "Status",
            "Speed Limiter",
            "Options"
        ];

        SelectedTabItem = TabItems.FirstOrDefault();
        HideDetailsButtonContent = "Hide Details";

        ResumePauseDownloadCommand = ReactiveCommand.CreateFromTask(ResumePauseDownloadAsync);
        CancelDownloadCommand = ReactiveCommand.CreateFromTask<Window?>(CancelDownloadAsync);
        ShowHideDetailsCommand = ReactiveCommand.CreateFromTask<Window?>(ShowHideDetailsAsync);
        OpenSaveLocationCommand = ReactiveCommand.CreateFromTask(OpenSaveLocationAsync);
    }

    public async Task StopDownloadAsync(Window? owner, bool closeWindow = true)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            await AppService
                .DownloadFileService
                .StopDownloadFileAsync(DownloadFile);

            if (closeWindow)
                owner.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task CheckResumeCapabilityAsync()
    {
        try
        {
            if (DownloadFile.Url.IsNullOrEmpty() || !DownloadFile.Url.CheckUrlValidation())
            {
                CanResumeFile = false;
                ResumeCapabilityState = "No";
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Range = new RangeHeaderValue(0, 0);

            // Send HEAD request
            using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, DownloadFile.Url));
            response.EnsureSuccessStatusCode();

            // Check for Accept-Ranges header
            if (response.Headers.Contains("Accept-Ranges"))
            {
                var acceptRanges = response.Headers.GetValues("Accept-Ranges");
                if (acceptRanges.Contains("bytes"))
                {
                    CanResumeFile = true;
                    ResumeCapabilityState = "Yes";
                    return;
                }
            }

            // Some servers don't include Accept-Ranges but still support partial content.
            // If Range request succeeds with Partial Content status:
            if (response.StatusCode == System.Net.HttpStatusCode.PartialContent)
            {
                CanResumeFile = true;
                ResumeCapabilityState = "Yes";
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        CanResumeFile = false;
        ResumeCapabilityState = "No";
    }

    #region Helpers

    private async Task ResumePauseDownloadAsync()
    {
        try
        {
            if (IsPaused)
            {
                AppService
                    .DownloadFileService
                    .ResumeDownloadFile(DownloadFile);

                IsPaused = false;
            }
            else
            {
                AppService
                    .DownloadFileService
                    .PauseDownloadFile(DownloadFile);

                IsPaused = true;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task CancelDownloadAsync(Window? owner)
    {
        await StopDownloadAsync(owner);
    }

    private async Task ShowHideDetailsAsync(Window? owner)
    {
        try
        {
            var detailsGrid = owner?.FindControl<Grid>("ChunksDetailsGrid");
            if (detailsGrid == null)
                return;

            if (_detailsIsVisible)
            {
                _detailsIsVisible = false;
                _detailsHeight = detailsGrid.Bounds.Height + 15;

                detailsGrid.IsVisible = false;
                owner!.MinHeight -= _detailsHeight;
                owner.Height -= _detailsHeight;

                HideDetailsButtonContent = "Show Details";
            }
            else
            {
                detailsGrid.IsVisible = true;
                owner!.MinHeight += _detailsHeight;
                owner.Height += _detailsHeight;

                HideDetailsButtonContent = "Hide Details";

                _detailsIsVisible = true;
                _detailsHeight = 0;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenSaveLocationAsync()
    {
        try
        {
            if (DownloadFile.SaveLocation.IsNullOrEmpty())
            {
                await ShowInfoDialogAsync("Open folder",
                    "The folder you are trying to access is not available. It may have been removed or relocated.",
                    DialogButtons.Ok);

                return;
            }

            if (!Directory.Exists(DownloadFile.SaveLocation))
            {
                await ShowInfoDialogAsync("Open folder",
                    "The folder you are trying to access is not available. It may have been removed or relocated.",
                    DialogButtons.Ok);

                return;
            }

            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = DownloadFile.SaveLocation,
                UseShellExecute = true
            };

            process.Start();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    public void ChangeSpeedLimiterState(DownloadSpeedLimiterViewEventArgs eventArgs)
    {
        _isSpeedLimiterEnabled = eventArgs.Enabled;
        _limitSpeed = _isSpeedLimiterEnabled ? eventArgs.Speed : null;
        _speedUnit = _isSpeedLimiterEnabled ? eventArgs.Unit : null;

        if (_isSpeedLimiterEnabled)
        {
            var unit = _speedUnit.IsNullOrEmpty() ? 0 :
                _speedUnit!.Equals("KB", StringComparison.OrdinalIgnoreCase) ? Constants.KB : Constants.MB;
            var speed = (long)(_limitSpeed == null ? 0 : _limitSpeed.Value * unit);

            AppService
                .DownloadFileService
                .LimitDownloadFileSpeed(DownloadFile, speed);
        }
        else
        {
            AppService
                .DownloadFileService
                .LimitDownloadFileSpeed(DownloadFile, 0);
        }
    }

    public void ChangeOptions(DownloadOptionsViewEventArgs eventArgs)
    {
        _openFolderAfterDownloadFinished = eventArgs.OpenFolderAfterDownloadFinished;
        _exitProgramAfterDownloadFinished = eventArgs.ExitProgramAfterDownloadFinished;
        _turnOffComputerAfterDownloadFinished = eventArgs.TurnOffComputerAfterDownloadFinished;
        _turnOffComputerMode = eventArgs.TurnOffComputerMode;
    }

    #endregion
}
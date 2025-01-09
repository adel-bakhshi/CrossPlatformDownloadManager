using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.DownloadWindowViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    private bool _detailsIsVisible = true;
    private double _detailsHeight;

    private ObservableCollection<string> _tabItems = [];
    private string? _selectedTabItem;
    private DownloadStatusViewModel? _downloadStatusViewModel;
    private DownloadSpeedLimiterViewModel? _downloadSpeedLimiterViewModel;
    private DownloadOptionsViewModel? _downloadOptionsViewModel;
    private DownloadFileViewModel _downloadFile = new();
    private bool _isPaused;
    private string? _hideDetailsButtonContent;

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

    public DownloadStatusViewModel? DownloadStatusViewModel
    {
        get => _downloadStatusViewModel;
        set => this.RaiseAndSetIfChanged(ref _downloadStatusViewModel, value);
    }

    public DownloadSpeedLimiterViewModel? DownloadSpeedLimiterViewModel
    {
        get => _downloadSpeedLimiterViewModel;
        set => this.RaiseAndSetIfChanged(ref _downloadSpeedLimiterViewModel, value);
    }

    public DownloadOptionsViewModel? DownloadOptionsViewModel
    {
        get => _downloadOptionsViewModel;
        set => this.RaiseAndSetIfChanged(ref _downloadOptionsViewModel, value);
    }

    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set
        {
            value.TransferRate ??= 0;
            value.TimeLeft ??= TimeSpan.Zero;

            this.RaiseAndSetIfChanged(ref _downloadFile, value);
        }
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

    public bool OpenFolderAfterDownloadFinished { get; set; }
    public bool ExitProgramAfterDownloadFinished { get; set; }
    public bool TurnOffComputerAfterDownloadFinished { get; set; }
    public string? TurnOffComputerMode { get; set; }

    #endregion

    #region Commands

    public ICommand ResumePauseDownloadCommand { get; }

    public ICommand CancelDownloadCommand { get; }

    public ICommand ShowHideDetailsCommand { get; }

    #endregion

    public DownloadWindowViewModel(IAppService appService, DownloadFileViewModel downloadFile) : base(appService)
    {
        DownloadFile = downloadFile;
        DownloadStatusViewModel = new DownloadStatusViewModel(appService, DownloadFile);

        DownloadSpeedLimiterViewModel = new DownloadSpeedLimiterViewModel(appService);
        DownloadSpeedLimiterViewModel.SpeedLimiterChanged += DownloadSpeedLimiterViewModelOnSpeedLimiterChanged;

        DownloadOptionsViewModel = new DownloadOptionsViewModel(appService);
        DownloadOptionsViewModel.OptionsChanged += DownloadOptionsViewModelOnOptionsChanged;

        TabItems =
        [
            "Status",
            "Speed Limiter",
            "Options"
        ];

        SelectedTabItem = TabItems.FirstOrDefault();
        HideDetailsButtonContent = "Hide Details";

        ResumePauseDownloadCommand = ReactiveCommand.CreateFromTask(ResumePauseDownloadAsync);
        CancelDownloadCommand = ReactiveCommand.CreateFromTask(CancelDownloadAsync);
        ShowHideDetailsCommand = ReactiveCommand.CreateFromTask<Window?>(ShowHideDetailsAsync);
    }

    public async Task StopDownloadAsync()
    {
        try
        {
            await AppService
                .DownloadFileService
                .StopDownloadFileAsync(DownloadFile);

            RemoveEventHandlers();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "Failed to stop downloading file");
        }
    }

    public void RemoveEventHandlers()
    {
        DownloadStatusViewModel?.RemoveEventHandlers();

        if (DownloadSpeedLimiterViewModel != null)
            DownloadSpeedLimiterViewModel.SpeedLimiterChanged -= DownloadSpeedLimiterViewModelOnSpeedLimiterChanged;

        if (DownloadOptionsViewModel != null)
            DownloadOptionsViewModel.OptionsChanged -= DownloadOptionsViewModelOnOptionsChanged;
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
            Log.Error(ex, "An error occured while trying to resume/pause the download.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task CancelDownloadAsync()
    {
        await StopDownloadAsync();
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
            Log.Error(ex, "An error occured while trying to show/hide the details.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void DownloadSpeedLimiterViewModelOnSpeedLimiterChanged(object? sender, SpeedLimiterChangedEventArgs e)
    {
        try
        {
            if (!e.Enabled)
            {
                AppService
                    .DownloadFileService
                    .LimitDownloadFileSpeed(DownloadFile, 0);

                return;
            }

            var unit = e.Unit.IsNullOrEmpty() ? 0 : e.Unit!.Equals("KB", StringComparison.OrdinalIgnoreCase) ? Constants.KiloByte : Constants.MegaByte;
            var speed = (long)(e.Speed == null ? 0 : e.Speed.Value * unit);

            AppService
                .DownloadFileService
                .LimitDownloadFileSpeed(DownloadFile, speed);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to limit the download speed.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void DownloadOptionsViewModelOnOptionsChanged(object? sender, DownloadOptionsChangedEventArgs e)
    {
        OpenFolderAfterDownloadFinished = e.OpenFolderAfterDownloadFinished;
        ExitProgramAfterDownloadFinished = e.ExitProgramAfterDownloadFinished;
        TurnOffComputerAfterDownloadFinished = e.TurnOffComputerAfterDownloadFinished;
        TurnOffComputerMode = e.TurnOffComputerMode;
    }

    #endregion
}
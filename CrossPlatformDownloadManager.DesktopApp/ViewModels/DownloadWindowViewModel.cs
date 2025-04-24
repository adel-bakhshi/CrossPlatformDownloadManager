using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.DownloadWindowViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
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

    private bool _openFolderAfterDownloadFinished;
    private bool _exitProgramAfterDownloadFinished;
    private bool _turnOffComputerAfterDownloadFinished;
    private string? _turnOffComputerMode;

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
        set
        {
            this.RaiseAndSetIfChanged(ref _isPaused, value);
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    public string? HideDetailsButtonContent
    {
        get => _hideDetailsButtonContent;
        set => this.RaiseAndSetIfChanged(ref _hideDetailsButtonContent, value);
    }

    public string Title => $"CDM - {(IsPaused ? "Paused" : "Downloading")} {DownloadFile.CeilingDownloadProgressAsString}";
    public bool CanCloseWindow { get; set; }

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

        DownloadFile.DownloadPaused += DownloadFileOnDownloadPaused;
        DownloadFile.DownloadResumed += DownloadFileOnDownloadResumed;
        DownloadFile.DownloadFinished += DownloadFileOnDownloadFinished;

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
            CanCloseWindow = true;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to stop download. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public void RemoveEventHandlers()
    {
        DownloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;
        DownloadFile.DownloadResumed -= DownloadFileOnDownloadResumed;
        DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;

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
            }
            else
            {
                AppService
                    .DownloadFileService
                    .PauseDownloadFile(DownloadFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to resume/pause the download. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void DownloadFileOnDownloadPaused(object? sender, DownloadFileEventArgs e)
    {
        IsPaused = true;
    }

    private void DownloadFileOnDownloadResumed(object? sender, DownloadFileEventArgs e)
    {
        IsPaused = false;
    }

    private void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        // Remove event handlers
        DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;

        // Check if the user wants to turn off the computer
        if (_turnOffComputerAfterDownloadFinished && DownloadFile.IsCompleted)
        {
            if (_turnOffComputerMode.IsStringNullOrEmpty())
                return;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    // Show the power off window
                    var vm = new PowerOffWindowViewModel(AppService, _turnOffComputerMode!, TimeSpan.FromSeconds(30));
                    var window = new PowerOffWindow { DataContext = vm };
                    window.Show();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while trying to show the power off window. Error message: {ErrorMessage}", ex.Message);
                    await DialogBoxManager.ShowErrorDialogAsync(ex);
                }
            });

            return;
        }

        // Check if the user wants to open the folder
        if (_openFolderAfterDownloadFinished)
        {
            // Make sure file name and save location are not null
            if (DownloadFile.FileName.IsStringNullOrEmpty() || DownloadFile.SaveLocation.IsStringNullOrEmpty())
                return;

            // Make sure the file exists
            var filePath = Path.Combine(DownloadFile.SaveLocation!, DownloadFile.FileName!);
            if (!File.Exists(filePath))
                return;

            // Open the folder and select the file
            PlatformSpecificManager.OpenContainingFolderAndSelectFile(filePath);
        }

        // Check if the user wants to exit the program
        if (!_exitProgramAfterDownloadFinished)
            return;

        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                // Exit the program
                App.Desktop?.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while trying to exit the app. Error message: {ErrorMessage}", ex.Message);
                await DialogBoxManager.ShowErrorDialogAsync(ex);
            }
        });
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
            Log.Error(ex, "An error occurred while trying to show/hide the details. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void DownloadSpeedLimiterViewModelOnSpeedLimiterChanged(object? sender, SpeedLimiterChangedEventArgs e)
    {
        try
        {
            long speed;
            if (!e.Enabled)
            {
                var globalSpeedLimit = AppService.SettingsService.Settings.LimitSpeed ?? 0;
                var globalSpeedLimitUnit = AppService.SettingsService.Settings.LimitUnit;
                speed = (long)(globalSpeedLimitUnit.IsStringNullOrEmpty()
                    ? 0
                    : globalSpeedLimit * (globalSpeedLimitUnit!.Equals("KB", StringComparison.OrdinalIgnoreCase) ? Constants.KiloByte : Constants.MegaByte));

                AppService
                    .DownloadFileService
                    .LimitDownloadFileSpeed(DownloadFile, speed);

                return;
            }

            var unit = e.Unit.IsStringNullOrEmpty() ? 0 : e.Unit!.Equals("KB", StringComparison.OrdinalIgnoreCase) ? Constants.KiloByte : Constants.MegaByte;
            speed = (long)(e.Speed == null ? 0 : e.Speed.Value * unit);

            AppService
                .DownloadFileService
                .LimitDownloadFileSpeed(DownloadFile, speed);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to limit the download speed. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void DownloadOptionsViewModelOnOptionsChanged(object? sender, DownloadOptionsChangedEventArgs e)
    {
        _openFolderAfterDownloadFinished = e.OpenFolderAfterDownloadFinished;
        _exitProgramAfterDownloadFinished = e.ExitProgramAfterDownloadFinished;
        _turnOffComputerAfterDownloadFinished = e.TurnOffComputerAfterDownloadFinished;
        _turnOffComputerMode = e.TurnOffComputerMode;
    }

    #endregion
}
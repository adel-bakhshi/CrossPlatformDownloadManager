using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using CrossPlatformDownloadManager.DesktopApp.Views.UserControls.DownloadWindowControls;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    /// <summary>
    /// Indicates whether the details section is visible or not.
    /// </summary>
    private bool _detailsIsVisible = true;

    /// <summary>
    /// Indicates the height of the details section.
    /// </summary>
    private double _detailsHeight;

    /// <summary>
    /// Indicates whether the folder containing the download file should be opened after the download finished.
    /// </summary>
    private bool _openFolderAfterDownloadFinished;

    /// <summary>
    /// Indicates whether the application should be exited after the download finished.
    /// </summary>
    private bool _exitProgramAfterDownloadFinished;

    /// <summary>
    /// Indicates whether the computer should be turned off after the download finished.
    /// </summary>
    private bool _turnOffComputerAfterDownloadFinished;

    /// <summary>
    /// Indicates the mode of the computer turn off.
    /// </summary>
    private string? _turnOffComputerMode;

    /// <summary>
    /// The list of the tabs that are available in the download window.
    /// </summary>
    private ObservableCollection<string> _tabItems = [];

    /// <summary>
    /// The tab that user is selected.
    /// </summary>
    private string? _selectedTabItem;

    /// <summary>
    /// Indicates the instance of the <see cref="DownloadStatusView"/>.
    /// </summary>
    private DownloadStatusView? _downloadStatusView;

    /// <summary>
    /// Indicates the instance of the <see cref="DownloadSpeedLimiterView"/>.
    /// </summary>
    private DownloadSpeedLimiterView? _downloadSpeedLimiterView;

    /// <summary>
    /// Indicates the instance of the <see cref="DownloadOptionsView"/>.
    /// </summary>
    private DownloadOptionsView? _downloadOptionsView;

    /// <summary>
    /// The data of the download file.
    /// </summary>
    private DownloadFileViewModel _downloadFile = new();

    /// <summary>
    /// Indicates that the downloading of the file currently is paused or not.
    /// </summary>
    private bool _isPaused;

    /// <summary>
    /// The content of the hide details button.
    /// </summary>
    private string? _hideDetailsButtonContent;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates the list of tabs that are available in the download window.
    /// </summary>
    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the tab that user is selected.
    /// </summary>
    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the instance of the <see cref="DownloadStatusView"/>.
    /// </summary>
    public DownloadStatusView? DownloadStatusView
    {
        get => _downloadStatusView;
        set => this.RaiseAndSetIfChanged(ref _downloadStatusView, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the view model of the <see cref="DownloadStatusView"/>.
    /// </summary>
    public DownloadStatusViewModel? DownloadStatusViewModel => DownloadStatusView?.DataContext as DownloadStatusViewModel;

    /// <summary>
    /// Gets or sets a value that indicates the instance of the <see cref="DownloadSpeedLimiterView"/>.
    /// </summary>
    public DownloadSpeedLimiterView? DownloadSpeedLimiterView
    {
        get => _downloadSpeedLimiterView;
        set => this.RaiseAndSetIfChanged(ref _downloadSpeedLimiterView, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the view model of the <see cref="DownloadSpeedLimiterView"/>.
    /// </summary>
    public DownloadSpeedLimiterViewModel? DownloadSpeedLimiterViewModel => DownloadSpeedLimiterView?.DataContext as DownloadSpeedLimiterViewModel;

    /// <summary>
    /// Gets or sets a value that indicates the instance of the <see cref="DownloadOptionsView"/>.
    /// </summary>
    public DownloadOptionsView? DownloadOptionsView
    {
        get => _downloadOptionsView;
        set => this.RaiseAndSetIfChanged(ref _downloadOptionsView, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the view model of the <see cref="DownloadOptionsView"/>.
    /// </summary>
    public DownloadOptionsViewModel? DownloadOptionsViewModel => DownloadOptionsView?.DataContext as DownloadOptionsViewModel;

    /// <summary>
    /// Gets or sets a value that indicates the data of the download file.
    /// </summary>
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

    /// <summary>
    /// Gets or sets a value that indicates the downloading of the file currently is paused or not.
    /// </summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            this.RaiseAndSetIfChanged(ref _isPaused, value);
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    /// <summary>
    /// Gets or sets a value that indicates the content of the hide details button.
    /// </summary>
    public string? HideDetailsButtonContent
    {
        get => _hideDetailsButtonContent;
        set => this.RaiseAndSetIfChanged(ref _hideDetailsButtonContent, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the title of the download window.
    /// </summary>
    public string Title => $"CDM - {(IsPaused ? "Paused" : "Downloading")} {DownloadFile.FloorDownloadProgressAsString}";

    /// <summary>
    /// Gets or sets a value that indicates whether the window can be closed or not.
    /// </summary>
    public bool CanCloseWindow { get; private set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the pause/resume button is enabled or not.
    /// </summary>
    public bool IsPauseResumeButtonEnabled => DownloadFile is { CanResumeDownload: true, IsMerging: false };

    #endregion

    #region Commands

    /// <summary>
    /// Gets a value that indicates the command to resume or pause the download.
    /// </summary>
    public ICommand ResumePauseDownloadCommand { get; }

    /// <summary>
    /// Gets a value that indicates the command to cancel the download.
    /// </summary>
    public ICommand CancelDownloadCommand { get; }

    /// <summary>
    /// Gets a value that indicates the command to show or hide the details of the download.
    /// </summary>
    public ICommand ShowHideDetailsCommand { get; }

    #endregion

    /// <summary>
    /// Create a new instance of the <see cref="DownloadWindowViewModel"/> class.
    /// </summary>
    /// <param name="appService">The <see cref="IAppService"/> instance containing the services of the application.</param>
    /// <param name="downloadFile">The <see cref="DownloadFileViewModel"/> instance containing the data of the download file.</param>
    public DownloadWindowViewModel(IAppService appService, DownloadFileViewModel downloadFile) : base(appService)
    {
        DownloadFile = downloadFile;
        DownloadFile.DownloadPaused += DownloadFileOnDownloadPaused;
        DownloadFile.DownloadResumed += DownloadFileOnDownloadResumed;
        DownloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
        DownloadFile.PropertyChanged += DownloadFileOnPropertyChanged;

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

    /// <summary>
    /// Stops the download of the file and set the CanCloseWindow flag to true.
    /// So after that the window can be closed.
    /// </summary>
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

    /// <summary>
    /// Removes all event handlers from the DownloadFile and the child view models.
    /// </summary>
    public void RemoveEventHandlers()
    {
        // Unsubscribe from download file events
        DownloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;
        DownloadFile.DownloadResumed -= DownloadFileOnDownloadResumed;
        DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
        // Remove event handlers from DownloadStatusViewModel
        DownloadStatusViewModel?.RemoveEventHandlers();
        // Remove event handlers from DownloadSpeedLimiterViewModel
        if (DownloadSpeedLimiterViewModel != null)
            DownloadSpeedLimiterViewModel.SpeedLimiterChanged -= DownloadSpeedLimiterViewModelOnSpeedLimiterChanged;

        // Remove event handlers from DownloadOptionsViewModel
        if (DownloadOptionsViewModel != null)
            DownloadOptionsViewModel.OptionsChanged -= DownloadOptionsViewModelOnOptionsChanged;
    }

    /// <summary>
    /// Creates the views for the download window.
    /// </summary>
    public async Task CreateViewsAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                var downloadStatusViewModel = new DownloadStatusViewModel(AppService, DownloadFile);
                DownloadStatusView = new DownloadStatusView { DataContext = downloadStatusViewModel };

                var downloadSpeedLimiterView = new DownloadSpeedLimiterViewModel(AppService);
                downloadSpeedLimiterView.SpeedLimiterChanged += DownloadSpeedLimiterViewModelOnSpeedLimiterChanged;
                DownloadSpeedLimiterView = new DownloadSpeedLimiterView { DataContext = downloadSpeedLimiterView };

                var downloadOptionsView = new DownloadOptionsViewModel(AppService);
                downloadOptionsView.OptionsChanged += DownloadOptionsViewModelOnOptionsChanged;
                DownloadOptionsView = new DownloadOptionsView { DataContext = downloadOptionsView };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while trying to create the views. Error message: {ErrorMessage}", ex.Message);
                await DialogBoxManager.ShowErrorDialogAsync(ex);
            }
        });
    }

    #region Command actions

    /// <summary>
    /// Resumes or pauses the download of the file.
    /// </summary>
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

    /// <summary>
    /// Cancels the download of the file.
    /// </summary>
    private async Task CancelDownloadAsync()
    {
        await StopDownloadAsync();
    }

    /// <summary>
    /// Shows or hides the details section on the download window.
    /// </summary>
    /// <param name="owner">The download window.</param>
    private async Task ShowHideDetailsAsync(Window? owner)
    {
        try
        {
            // Find ChunksDetailsGrid and make sure exists
            var detailsGrid = owner?.FindControl<Grid>("ChunksDetailsGrid");
            if (detailsGrid == null)
                return;

            // If details is visible, hide it
            if (_detailsIsVisible)
            {
                _detailsIsVisible = false;
                _detailsHeight = detailsGrid.Bounds.Height + 15;

                detailsGrid.IsVisible = false;
                owner!.MinHeight -= _detailsHeight;
                owner.Height -= _detailsHeight;

                HideDetailsButtonContent = "Show Details";
            }
            // Otherwise, show it
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

    #endregion

    #region Events handlers

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadPaused"/> event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance containing the event data.</param>
    private void DownloadFileOnDownloadPaused(object? sender, DownloadFileEventArgs e)
    {
        IsPaused = true;
    }

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadResumed"/> event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance containing the event data.</param>
    private void DownloadFileOnDownloadResumed(object? sender, DownloadFileEventArgs e)
    {
        IsPaused = false;
    }

    /// <summary>
    /// Handles the <see cref="DownloadFileViewModel.DownloadFinished"/> event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadFileEventArgs"/> instance containing the event data.</param>
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
            PlatformSpecificManager.Current.OpenContainingFolderAndSelectFile(filePath);
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

    /// <summary>
    /// Handles the <see cref="PropertyChangedBase.PropertyChanged"/> event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
    private void DownloadFileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.Equals(nameof(DownloadFile.CanResumeDownload)) == true || e.PropertyName?.Equals(nameof(DownloadFile.Status)) == true)
            this.RaisePropertyChanged(nameof(IsPauseResumeButtonEnabled));

        if (e.PropertyName?.Equals(nameof(DownloadFile.DownloadProgress)) == true)
            this.RaisePropertyChanged(nameof(Title));
    }

    /// <summary>
    /// Handles the <see cref="DownloadWindowViewModels.DownloadSpeedLimiterViewModel.SpeedLimiterChanged"/> event and change the maximum download speed of the file.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="SpeedLimiterChangedEventArgs"/> instance containing the event data.</param>
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

    /// <summary>
    /// Handles the <see cref="DownloadWindowViewModels.DownloadOptionsViewModel.OptionsChanged"/> event and change the options of the download.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The <see cref="DownloadOptionsChangedEventArgs"/> instance containing the event data.</param>
    private void DownloadOptionsViewModelOnOptionsChanged(object? sender, DownloadOptionsChangedEventArgs e)
    {
        _openFolderAfterDownloadFinished = e.OpenFolderAfterDownloadFinished;
        _exitProgramAfterDownloadFinished = e.ExitProgramAfterDownloadFinished;
        _turnOffComputerAfterDownloadFinished = e.TurnOffComputerAfterDownloadFinished;
        _turnOffComputerMode = e.TurnOffComputerMode;
    }

    #endregion
}
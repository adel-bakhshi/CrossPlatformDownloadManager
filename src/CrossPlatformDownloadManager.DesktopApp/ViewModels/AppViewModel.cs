using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AppViewModel : ViewModelBase
{
    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates whether the tray icon is visible.
    /// </summary>
    public bool IsTrayIconVisible
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    #endregion

    #region Commands

    public ICommand OpenMainWindowCommand { get; }
    public ICommand AddNewLinkCommand { get; }
    public ICommand AddNewQueueCommand { get; }
    public ICommand OpenSettingsWindowCommand { get; }
    public ICommand OpenCdmWebPageCommand { get; }
    public ICommand OpenAboutUsWindowCommand { get; }
    public ICommand ExitApplicationCommand { get; }

    #endregion

    public AppViewModel(IAppService appService) : base(appService)
    {
        UpdateTrayIcon();

        // Initialize commands
        OpenMainWindowCommand = ReactiveCommand.CreateFromTask(OpenMainWindowAsync);
        AddNewLinkCommand = ReactiveCommand.CreateFromTask(AddNewLinkAsync);
        AddNewQueueCommand = ReactiveCommand.CreateFromTask(AddNewQueueAsync);
        OpenSettingsWindowCommand = ReactiveCommand.CreateFromTask(OpenSettingsWindowAsync);
        OpenCdmWebPageCommand = ReactiveCommand.CreateFromTask(OpenCdmWebPageAsync);
        OpenAboutUsWindowCommand = ReactiveCommand.CreateFromTask(OpenAboutUsWindowAsync);
        ExitApplicationCommand = ReactiveCommand.CreateFromTask(ExitApplicationAsync);
    }

    #region Command Actions

    /// <summary>
    /// Handles the <see cref="OpenMainWindowCommand"/> command.
    /// </summary>
    private async Task OpenMainWindowAsync()
    {
        try
        {
            AppService.TrayMenuService.OpenMainWindow();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while opening the main window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Handles the <see cref="AddNewLinkCommand"/> command.
    /// </summary>
    private async Task AddNewLinkAsync()
    {
        try
        {
            AppService.TrayMenuService.AddNewDownloadLink(AppService);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while adding the download link. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the <see cref="AddNewQueueCommand"/> command.
    /// </summary>
    private async Task AddNewQueueAsync()
    {
        try
        {
            AppService.TrayMenuService.AddNewDownloadQueue(AppService);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while adding the download queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the <see cref="OpenSettingsWindowCommand"/> command.
    /// </summary>
    private async Task OpenSettingsWindowAsync()
    {
        try
        {
            AppService.TrayMenuService.OpenSettingsWindow(AppService);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while opening the settings window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the <see cref="OpenCdmWebPageCommand"/> command.
    /// </summary>
    private async Task OpenCdmWebPageAsync()
    {
        try
        {
            AppService.TrayMenuService.OpenCdmWebPage();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while opening the help window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the <see cref="OpenAboutUsWindowCommand"/> command.
    /// </summary>
    private async Task OpenAboutUsWindowAsync()
    {
        try
        {
            AppService.TrayMenuService.OpenAboutUsWindow(AppService);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while opening the about us window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the <see cref="ExitApplicationCommand"/> command.
    /// </summary>
    private async Task ExitApplicationAsync()
    {
        try
        {
            await AppService.TrayMenuService.ExitProgramAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while exiting the application. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #endregion

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();

        UpdateTrayIcon();
        this.RaisePropertyChanged(nameof(IsTrayIconVisible));
    }

    /// <summary>
    /// Updates tray icon visibility.
    /// </summary>
    private void UpdateTrayIcon()
    {
        IsTrayIconVisible = !AppService.SettingsService.Settings.UseManager;
    }
}
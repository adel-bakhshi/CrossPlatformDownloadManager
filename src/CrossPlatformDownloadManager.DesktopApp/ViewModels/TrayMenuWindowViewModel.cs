using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class TrayMenuWindowViewModel : ViewModelBase
{
    #region Private Fields

    private const string SystemProxySettingsName = "System proxy settings";

    private bool _firstTimeRefreshProxies = true;
    private bool _canChangeProxy = true;

    #endregion

    #region Properties

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsDownloadQueuesEmpty));
        }
    }

    public bool IsDownloadQueuesEmpty => DownloadQueues.Count == 0;

    public ObservableCollection<ProxySettingsViewModel> Proxies
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsProxiesEmpty));
        }
    }

    public ProxySettingsViewModel? SelectedProxy
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            _ = ChangeProxyAsync();
        }
    }

    public bool IsProxiesEmpty => Proxies.Count == 0;
    public TrayMenuWindow? TrayMenuWindow { get; set; }

    #endregion

    #region Commands

    public ICommand OpenMainWindowCommand { get; }

    public ICommand StartStopDownloadQueueCommand { get; }

    public ICommand AddNewDownloadLinkCommand { get; }

    public ICommand AddNewDownloadQueueCommand { get; }

    public ICommand OpenSettingsWindowCommand { get; }

    public ICommand OpenCdmWebPageCommand { get; }

    public ICommand OpenAboutUsWindowCommand { get; }

    public ICommand ExitProgramCommand { get; }

    #endregion

    public TrayMenuWindowViewModel(IAppService appService) : base(appService)
    {
        DownloadQueues = [];
        Proxies = [];

        LoadDownloadQueues();
        RefreshProxies();

        OpenMainWindowCommand = ReactiveCommand.CreateFromTask(OpenMainWindowAsync);
        StartStopDownloadQueueCommand = ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(StartStopDownloadQueueAsync);
        AddNewDownloadLinkCommand = ReactiveCommand.CreateFromTask(AddNewDownloadLinkAsync);
        AddNewDownloadQueueCommand = ReactiveCommand.CreateFromTask(AddNewDownloadQueueAsync);
        OpenSettingsWindowCommand = ReactiveCommand.CreateFromTask(OpenSettingsWindowAsync);
        OpenCdmWebPageCommand = ReactiveCommand.CreateFromTask(OpenCdmWebPageAsync);
        OpenAboutUsWindowCommand = ReactiveCommand.CreateFromTask(OpenAboutUsWindowAsync);
        ExitProgramCommand = ReactiveCommand.CreateFromTask(ExitProgramAsync);
    }

    private async Task OpenMainWindowAsync()
    {
        try
        {
            HideTrayMenu();
            AppService.TrayMenuService.OpenMainWindow();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while opening the main window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task StartStopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue)
    {
        try
        {
            if (downloadQueue == null)
                return;

            HideTrayMenu();
            await AppService.TrayMenuService.StartStopDownloadQueueAsync(downloadQueue);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while starting/stopping the download queue. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task AddNewDownloadLinkAsync()
    {
        try
        {
            HideTrayMenu();
            AppService.TrayMenuService.AddNewDownloadLink(AppService);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while adding the download link. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task AddNewDownloadQueueAsync()
    {
        try
        {
            HideTrayMenu();
            AppService.TrayMenuService.AddNewDownloadQueue(AppService);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while adding the download queue. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task OpenSettingsWindowAsync()
    {
        try
        {
            HideTrayMenu();
            AppService.TrayMenuService.OpenSettingsWindow(AppService);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while opening the settings window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task OpenCdmWebPageAsync()
    {
        try
        {
            HideTrayMenu();
            AppService.TrayMenuService.OpenCdmWebPage();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while opening the help window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task OpenAboutUsWindowAsync()
    {
        try
        {
            HideTrayMenu();
            AppService.TrayMenuService.OpenAboutUsWindow(AppService);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while opening the about us window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task ExitProgramAsync()
    {
        try
        {
            HideTrayMenu();
            await AppService.TrayMenuService.ExitProgramAsync();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to exit the app. Error message: {ErrorMessage}", ex.Message);
        }
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        base.OnDownloadQueueServiceDataChanged();
        LoadDownloadQueues();
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        RefreshProxies();
    }

    #region Helpers

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService.DownloadQueueService.DownloadQueues;
    }

    private void RefreshProxies()
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                if (!_firstTimeRefreshProxies)
                    await Task.Delay(1000);

                var proxies = AppService
                    .SettingsService
                    .Settings
                    .Proxies
                    .ToList();

                var systemProxySettings = new ProxySettingsViewModel { Id = -1, Name = SystemProxySettingsName };
                proxies.Insert(0, systemProxySettings);

                if (SelectedProxy != null)
                {
                    _canChangeProxy = false;
                    Proxies = proxies.ToObservableCollection();
                    _canChangeProxy = true;
                }
                else
                {
                    if (_firstTimeRefreshProxies)
                        _firstTimeRefreshProxies = false;
                    else
                        _canChangeProxy = false;

                    Proxies = proxies.ToObservableCollection();

                    if (!_canChangeProxy)
                        _canChangeProxy = true;
                }

                var proxyMode = AppService.SettingsService.Settings.ProxyMode;
                SelectedProxy = proxyMode switch
                {
                    ProxyMode.DisableProxy => null,
                    ProxyMode.UseSystemProxySettings => Proxies.FirstOrDefault(p => p.Id == -1),
                    ProxyMode.UseCustomProxy => Proxies.FirstOrDefault(p => p.IsActive),
                    _ => throw new InvalidOperationException("Invalid proxy mode.")
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while refreshing proxies. Error message: {ErrorMessage}", ex.Message);
                await DialogBoxManager.ShowErrorDialogAsync(ex);
            }
        });
    }

    private void HideTrayMenu()
    {
        var dataContext = TrayMenuWindow?.OwnerWindow?.DataContext;
        if (dataContext is not ManagerWindowViewModel trayIconWindowViewModel)
            throw new InvalidOperationException("View model not found.");

        trayIconWindowViewModel.HideMenu();
    }

    private async Task ChangeProxyAsync()
    {
        try
        {
            if (!_canChangeProxy)
                return;

            var isProxyDisabled = AppService
                .SettingsService
                .Settings
                .ProxyMode == ProxyMode.DisableProxy;

            if (SelectedProxy == null)
            {
                if (!isProxyDisabled)
                {
                    await AppService
                        .SettingsService
                        .DisableProxyAsync();
                }

                return;
            }

            switch (SelectedProxy.Name)
            {
                case SystemProxySettingsName:
                {
                    var isSystemProxySettings = AppService
                        .SettingsService
                        .Settings
                        .ProxyMode == ProxyMode.UseSystemProxySettings;

                    if (isSystemProxySettings)
                        break;

                    await AppService
                        .SettingsService
                        .UseSystemProxySettingsAsync();

                    break;
                }

                default:
                {
                    var isCustomProxy = AppService
                        .SettingsService
                        .Settings
                        .ProxyMode == ProxyMode.UseCustomProxy;

                    var activeProxySettings = AppService
                        .SettingsService
                        .Settings
                        .Proxies
                        .FirstOrDefault(p => p.IsActive);

                    if (isCustomProxy &&
                        activeProxySettings != null &&
                        activeProxySettings.Id == SelectedProxy?.Id)
                    {
                        break;
                    }

                    await AppService
                        .SettingsService
                        .UseCustomProxyAsync(SelectedProxy);

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while changing proxy. Error message: {ErrorMessage}", ex.Message);
        }
    }

    #endregion
}
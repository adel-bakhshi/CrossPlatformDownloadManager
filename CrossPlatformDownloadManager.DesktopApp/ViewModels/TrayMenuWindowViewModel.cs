using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class TrayMenuWindowViewModel : ViewModelBase
{
    #region Private Fields

    private const string DisableProxySettingsName = "Disable proxy";
    private const string SystemProxySettingsName = "System proxy settings";
    private readonly List<ProxySettingsViewModel> _defaultProxies;

    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];
    private ObservableCollection<ProxySettingsViewModel> _proxies = [];
    private ProxySettingsViewModel? _selectedProxy;

    private bool _isFirstLoad = true;

    #endregion

    #region Properties

    public ObservableCollection<DownloadQueueViewModel> DownloadQueues
    {
        get => _downloadQueues;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadQueues, value);
            this.RaisePropertyChanged(nameof(IsDownloadQueuesEmpty));
        }
    }

    public bool IsDownloadQueuesEmpty => DownloadQueues.Count == 0;

    public ObservableCollection<ProxySettingsViewModel> Proxies
    {
        get => _proxies;
        set
        {
            this.RaiseAndSetIfChanged(ref _proxies, value);
            this.RaisePropertyChanged(nameof(IsProxiesEmpty));
        }
    }

    public ProxySettingsViewModel? SelectedProxy
    {
        get => _selectedProxy;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProxy, value);
            _ = ChangeProxyAsync();
        }
    }

    public bool IsProxiesEmpty => Proxies.Count == 0;
    public TrayMenuWindow? TrayMenuWindow { get; set; }

    #endregion

    #region Commands

    public ICommand? StartStopDownloadQueueCommand { get; }

    public ICommand? AddNewDownloadLinkCommand { get; }

    public ICommand? AddNewDownloadQueueCommand { get; }

    public ICommand? OpenSettingsWindowCommand { get; }

    public ICommand? OpenHelpWindowCommand { get; }

    public ICommand? OpenAboutUsWindowCommand { get; }

    public ICommand? ExitProgramCommand { get; }

    #endregion

    public TrayMenuWindowViewModel(IAppService appService) : base(appService)
    {
        _defaultProxies =
        [
            new ProxySettingsViewModel { Id = -1, Name = DisableProxySettingsName },
            new ProxySettingsViewModel { Id = -2, Name = SystemProxySettingsName }
        ];

        LoadDownloadQueues();
        RefreshProxies();

        StartStopDownloadQueueCommand = ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(StartStopDownloadQueueAsync);
        AddNewDownloadLinkCommand = ReactiveCommand.CreateFromTask(AddNewDownloadLinkAsync);
        AddNewDownloadQueueCommand = ReactiveCommand.Create(AddNewDownloadQueue);
        OpenSettingsWindowCommand = ReactiveCommand.Create(OpenSettingsWindow);
        OpenHelpWindowCommand = ReactiveCommand.Create(OpenHelpWindow);
        OpenAboutUsWindowCommand = ReactiveCommand.Create(OpenAboutUsWindow);
        ExitProgramCommand = ReactiveCommand.CreateFromTask(ExitProgramAsync);
    }

    private async Task StartStopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue)
    {
        // TODO: Show message box
        try
        {
            if (downloadQueue == null)
                return;

            HideTrayMenu();

            if (!downloadQueue.IsRunning)
            {
                await AppService
                    .DownloadQueueService
                    .StartDownloadQueueAsync(downloadQueue);
            }
            else
            {
                await AppService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(downloadQueue);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task AddNewDownloadLinkAsync()
    {
        // TODO: Show message box
        try
        {
            HideTrayMenu();

            var url = string.Empty;
            if (TrayMenuWindow!.Clipboard != null)
                url = await TrayMenuWindow.Clipboard.GetTextAsync();

            var urlIsValid = url.CheckUrlValidation();
            var vm = new AddDownloadLinkWindowViewModel(AppService)
            {
                Url = urlIsValid ? url : null,
                IsLoadingUrl = urlIsValid
            };

            var window = new AddDownloadLinkWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void AddNewDownloadQueue()
    {
        try
        {
            HideTrayMenu();

            var vm = new AddEditQueueWindowViewModel(AppService);
            var window = new AddEditQueueWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void OpenSettingsWindow()
    {
        // TODO: Show message box
        try
        {
            HideTrayMenu();

            var vm = new SettingsWindowViewModel(AppService);
            var window = new SettingsWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void OpenHelpWindow()
    {
        throw new NotImplementedException();
    }

    private void OpenAboutUsWindow()
    {
        throw new NotImplementedException();
    }

    private async Task ExitProgramAsync()
    {
        try
        {
            HideTrayMenu();

            if (App.Desktop == null)
                return;

            var result = await ShowInfoDialogAsync("Exit", "Are you sure you want to exit the app?", DialogButtons.YesNo);
            if (result != DialogResult.Yes)
                return;

            App.Desktop.Shutdown();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        LoadDownloadQueues();
        base.OnDownloadQueueServiceDataChanged();
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        RefreshProxies();
    }

    #region Helpers

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;
    }

    private void RefreshProxies()
    {
        var proxies = AppService
            .SettingsService
            .Settings
            .Proxies
            .ToList();
        
        proxies.InsertRange(0, _defaultProxies);

        var currentSelectedProxy = SelectedProxy;
        Proxies = proxies.ToObservableCollection();

        if (_isFirstLoad)
        {
            _isFirstLoad = false;
            SelectedProxy = Proxies.FirstOrDefault();
            return;
        }

        SelectedProxy = Proxies.FirstOrDefault(p => p.Id == currentSelectedProxy?.Id);
    }

    private void HideTrayMenu()
    {
        var dataContext = TrayMenuWindow?.OwnerWindow?.DataContext;
        if (dataContext is not TrayIconWindowViewModel trayIconWindowViewModel)
            throw new InvalidOperationException("View model not found.");

        trayIconWindowViewModel.HideMenu();
    }

    private async Task ChangeProxyAsync()
    {
        try
        {
            if (SelectedProxy == null)
                return;

            switch (SelectedProxy.Name)
            {
                case DisableProxySettingsName:
                {
                    await AppService
                        .SettingsService
                        .DisableProxyAsync();

                    break;
                }

                case SystemProxySettingsName:
                {
                    await AppService
                        .SettingsService
                        .UseSystemProxySettingsAsync();

                    break;
                }

                default:
                {
                    await AppService
                        .SettingsService
                        .ActiveProxyAsync(SelectedProxy);

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    #endregion
}
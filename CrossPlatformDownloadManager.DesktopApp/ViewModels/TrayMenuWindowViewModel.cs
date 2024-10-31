using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.AppFinisher;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class TrayMenuWindowViewModel : ViewModelBase
{
    #region Private Fields

    private ObservableCollection<DownloadQueueViewModel> _downloadQueues = [];
    private ObservableCollection<ProxySettingsViewModel> _proxies = [];
    private ProxySettingsViewModel? _selectedProxy;

    private ProxySettingsViewModel? _oldSelectedProxy;

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
        set => this.RaiseAndSetIfChanged(ref _selectedProxy, value);
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
        LoadDownloadQueues();
        LoadProxies();

        StartStopDownloadQueueCommand =
            ReactiveCommand.CreateFromTask<DownloadQueueViewModel?>(StartStopDownloadQueueAsync);
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
        // TODO: Show message box
        try
        {
            HideTrayMenu();
            
            // TODO: Ask user if he wants to exit
            var serviceProvider = Application.Current?.TryGetServiceProvider();
            if (serviceProvider == null)
                throw new InvalidOperationException("Service provider not found.");

            var appFinisher = serviceProvider.GetService<IAppFinisher>();
            if (appFinisher == null)
                throw new InvalidOperationException("App finisher service not found.");

            await appFinisher.FinishAppAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void UnselectProxyIfNotChanged()
    {
        if (SelectedProxy == null || _oldSelectedProxy?.Id != SelectedProxy.Id)
        {
            _oldSelectedProxy = SelectedProxy;
            return;
        }

        SelectedProxy = null;
        _oldSelectedProxy = null;
    }

    protected override void OnDownloadQueueServiceDataChanged()
    {
        LoadDownloadQueues();
        base.OnDownloadQueueServiceDataChanged();
    }

    #region Helpers

    private void LoadDownloadQueues()
    {
        DownloadQueues = AppService
            .DownloadQueueService
            .DownloadQueues;
    }

    private void LoadProxies()
    {
        var proxies = new List<ProxySettingsViewModel>
        {
            new() { Id = 0, Title = "System Proxy Settings", },
            new() { Id = 1, Title = "v2RayN", },
            new() { Id = 2, Title = "Nekoray", },
            new() { Id = 3, Title = "Hiddify", },
        };

        Proxies = proxies.ToObservableCollection();
    }

    private void HideTrayMenu()
    {
        var dataContext = TrayMenuWindow?.OwnerWindow?.DataContext;
        if (dataContext is not TrayIconWindowViewModel trayIconWindowViewModel)
            throw new InvalidOperationException("View model not found.");

        trayIconWindowViewModel.HideMenu();
    }

    #endregion
}
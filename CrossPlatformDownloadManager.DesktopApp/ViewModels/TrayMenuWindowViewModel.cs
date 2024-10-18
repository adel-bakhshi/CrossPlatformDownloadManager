using System.Collections.Generic;
using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

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
            this.RaisePropertyChanged(nameof(IsQueuesNotEmpty));
        }
    }

    public bool IsQueuesNotEmpty => DownloadQueues.Count > 0;

    public ObservableCollection<ProxySettingsViewModel> Proxies
    {
        get => _proxies;
        set
        {
            this.RaiseAndSetIfChanged(ref _proxies, value);
            this.RaisePropertyChanged(nameof(IsProxiesNotEmpty));
        }
    }

    public ProxySettingsViewModel? SelectedProxy
    {
        get => _selectedProxy;
        set => this.RaiseAndSetIfChanged(ref _selectedProxy, value);
    }

    public bool IsProxiesNotEmpty => Proxies.Count > 0;

    #endregion

    public TrayMenuWindowViewModel(IAppService appService) : base(appService)
    {
        LoadDownloadQueues();
        LoadProxies();
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

    #endregion
}
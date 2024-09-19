using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class ProxyViewModel : ViewModelBase
{
    #region Properties

    private bool _disableProxy;

    public bool DisableProxy
    {
        get => _disableProxy;
        set => this.RaiseAndSetIfChanged(ref _disableProxy, value);
    }
    
    private bool _useSystemProxySettings;

    public bool UseSystemProxySettings
    {
        get => _useSystemProxySettings;
        set => this.RaiseAndSetIfChanged(ref _useSystemProxySettings, value);
    }
    
    private bool _useCustomProxy;

    public bool UseCustomProxy
    {
        get => _useCustomProxy;
        set => this.RaiseAndSetIfChanged(ref _useCustomProxy, value);
    }

    private ObservableCollection<string> _proxyTypes = [];

    public ObservableCollection<string> ProxyTypes
    {
        get => _proxyTypes;
        set => this.RaiseAndSetIfChanged(ref _proxyTypes, value);
    }
    
    private string? _selectedProxyType;

    public string? SelectedProxyType
    {
        get => _selectedProxyType;
        set => this.RaiseAndSetIfChanged(ref _selectedProxyType, value);
    }
    
    private string? _host;

    public string? Host
    {
        get => _host;
        set => this.RaiseAndSetIfChanged(ref _host, value);
    }
    
    private string? _port;

    public string? Port
    {
        get => _port;
        set => this.RaiseAndSetIfChanged(ref _port, value);
    }
    
    private string? _username;

    public string? Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }
    
    private string? _password;

    public string? Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    #endregion
    
    public ProxyViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(unitOfWork, downloadFileService)
    {
        GenerateProxyTypes();
    }
    
    private void GenerateProxyTypes()
    {
        var proxyTypes = new List<string>
        {
            "Http",
            "Https",
            "Socks 5",
        };

        ProxyTypes = proxyTypes.ToObservableCollection();
        SelectedProxyType = ProxyTypes.FirstOrDefault();
    }
}
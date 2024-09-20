using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.Services.AppService;
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

    #region Commands

    public ICommand? ChangeProxyModeCommand { get; }

    #endregion

    public ProxyViewModel(IAppService appService) : base(appService)
    {
        ProxyTypes = Constants.ProxyTypes.ToObservableCollection();
        SelectedProxyType = ProxyTypes.FirstOrDefault();

        ChangeProxyModeCommand = ReactiveCommand.Create<string?>(ChangeProxyMode);
    }

    private void ChangeProxyMode(string? toggleSwitchName)
    {
        if (toggleSwitchName.IsNullOrEmpty())
            return;

        string? proxyMode = null;
        switch (toggleSwitchName)
        {
            case "DisableProxyToggleSwitch":
            {
                proxyMode = nameof(DisableProxy);
                break;
            }

            case "UseSystemProxySettingsToggleSwitch":
            {
                proxyMode = nameof(UseSystemProxySettings);
                break;
            }

            case "UseCustomProxyToggleSwitch":
            {
                proxyMode = nameof(UseCustomProxy);
                break;
            }
        }

        if (proxyMode.IsNullOrEmpty())
            return;

        var proxies = new List<string>
        {
            nameof(DisableProxy),
            nameof(UseSystemProxySettings),
            nameof(UseCustomProxy),
        };

        proxies
            .Where(proxy => !proxyMode!.Equals(proxy, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .ForEach(proxy =>
            {
                var propertyValue = GetType().GetProperty(proxy)?.GetValue(this) as bool?;
                if (propertyValue == true)
                    GetType().GetProperty(proxy)?.SetValue(this, false);
            });
    }
}
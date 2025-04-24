using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class ProxyViewModel : ViewModelBase
{
    #region Private Fields

    private bool _disableProxy;
    private bool _useSystemProxySettings;
    private bool _useCustomProxy;
    private ProxySettingsViewModel _proxySettings = null!;
    private ObservableCollection<string> _proxyTypes = [];
    private ObservableCollection<ProxySettingsViewModel> _availableProxies = [];
    private ProxySettingsViewModel? _selectedAvailableProxy;
    private bool _editingProxy;

    #endregion

    #region Properties

    public bool DisableProxy
    {
        get => _disableProxy;
        set => this.RaiseAndSetIfChanged(ref _disableProxy, value);
    }

    public bool UseSystemProxySettings
    {
        get => _useSystemProxySettings;
        set => this.RaiseAndSetIfChanged(ref _useSystemProxySettings, value);
    }

    public bool UseCustomProxy
    {
        get => _useCustomProxy;
        set => this.RaiseAndSetIfChanged(ref _useCustomProxy, value);
    }

    public ProxySettingsViewModel ProxySettings
    {
        get => _proxySettings;
        set => this.RaiseAndSetIfChanged(ref _proxySettings, value);
    }

    public ObservableCollection<string> ProxyTypes
    {
        get => _proxyTypes;
        set => this.RaiseAndSetIfChanged(ref _proxyTypes, value);
    }

    public ObservableCollection<ProxySettingsViewModel> AvailableProxies
    {
        get => _availableProxies;
        set
        {
            this.RaiseAndSetIfChanged(ref _availableProxies, value);
            this.RaisePropertyChanged(nameof(IsAvailableProxiesExists));
        }
    }

    public ProxySettingsViewModel? SelectedAvailableProxy
    {
        get => _selectedAvailableProxy;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedAvailableProxy, value);
            _ = SetSelectedProxyAsync();
        }
    }

    public bool IsAvailableProxiesExists => AvailableProxies.Any();

    public bool EditingProxy
    {
        get => _editingProxy;
        set => this.RaiseAndSetIfChanged(ref _editingProxy, value);
    }

    #endregion

    #region Commands

    public ICommand ChangeProxyModeCommand { get; }

    public ICommand ClearProxyCommand { get; }

    public ICommand DeleteProxyCommand { get; }

    public ICommand SaveProxyCommand { get; }

    #endregion

    public ProxyViewModel(IAppService appService) : base(appService)
    {
        ProxySettings = new ProxySettingsViewModel();
        ProxyTypes = Constants.ProxyTypes.ToObservableCollection();
        ProxySettings.Type = ProxyTypes.FirstOrDefault();

        ChangeProxyTypeAsync().GetAwaiter();

        ChangeProxyModeCommand = ReactiveCommand.Create<ToggleSwitch?>(ChangeProxyMode);
        ClearProxyCommand = ReactiveCommand.Create(ClearProxy);
        DeleteProxyCommand = ReactiveCommand.CreateFromTask(DeleteProxyAsync);
        SaveProxyCommand = ReactiveCommand.CreateFromTask(SaveProxyAsync);
    }

    public async Task LoadAvailableProxiesAsync()
    {
        try
        {
            AvailableProxies = AppService
                .SettingsService
                .Settings
                .Proxies;

            var tasks = AvailableProxies.Select(p => p.CheckIsResponsiveAsync()).ToList();
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading available proxies. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ChangeProxyTypeAsync()
    {
        try
        {
            var settings = AppService
                .SettingsService
                .Settings;

            switch (settings.ProxyMode)
            {
                case ProxyMode.DisableProxy:
                {
                    DisableProxy = true;
                    break;
                }

                case ProxyMode.UseSystemProxySettings:
                {
                    UseSystemProxySettings = true;
                    break;
                }

                case ProxyMode.UseCustomProxy:
                {
                    UseCustomProxy = true;
                    break;
                }

                default:
                    throw new InvalidOperationException("Unknown proxy mode.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while changing proxy type. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void ChangeProxyMode(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch == null)
            return;

        var name = toggleSwitch.Name;
        if (name.IsStringNullOrEmpty())
            return;

        if (toggleSwitch.IsChecked != true)
        {
            DisableProxy = true;
            return;
        }

        var proxyMode = name switch
        {
            "DisableProxyToggleSwitch" => nameof(DisableProxy),
            "UseSystemProxySettingsToggleSwitch" => nameof(UseSystemProxySettings),
            "UseCustomProxyToggleSwitch" => nameof(UseCustomProxy),
            _ => null
        };

        if (proxyMode.IsStringNullOrEmpty())
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

    private void ClearProxy()
    {
        ProxySettings = new ProxySettingsViewModel
        {
            Type = ProxyTypes.FirstOrDefault()
        };

        EditingProxy = false;
        SelectedAvailableProxy = null;
    }

    private async Task DeleteProxyAsync()
    {
        try
        {
            if (SelectedAvailableProxy == null)
                return;

            await AppService
                .SettingsService
                .DeleteProxySettingsAsync(SelectedAvailableProxy);

            ClearProxy();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while deleting proxy. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task SaveProxyAsync()
    {
        try
        {
            if (ProxySettings.Type.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Attention",
                    "Make sure you select a proxy type and try again. Selected proxy type is not defined.",
                    DialogButtons.Ok);

                return;
            }

            if (ProxySettings.Host.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please enter proxy host.", DialogButtons.Ok);
                return;
            }

            ProxySettings.Host = ProxySettings.Host!.Trim();

            if (ProxySettings.Port.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please enter proxy port.", DialogButtons.Ok);
                return;
            }

            ProxySettings.Port = ProxySettings.Port!.Trim();

            if (!int.TryParse(ProxySettings.Port, out _))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please enter a valid port number.", DialogButtons.Ok);
                return;
            }

            ProxySettings.Username = ProxySettings.Username?.Trim();
            ProxySettings.Password = ProxySettings.Password?.Trim();
            ProxySettings.Name = ProxySettings.Name?.Trim();

            if (ProxySettings.Name.IsStringNullOrEmpty())
                ProxySettings.Name = $"{ProxySettings.Host}:{ProxySettings.Port}";

            // Edit proxy settings when id is not 0
            if (ProxySettings.Id != 0)
            {
                var viewModel = AppService
                    .SettingsService
                    .Settings
                    .Proxies
                    .FirstOrDefault(p => p.Id == ProxySettings.Id);

                if (viewModel == null)
                    throw new InvalidOperationException("Unable to find proxy settings in database. Please try again later.");

                viewModel.Name = ProxySettings.Name;
                viewModel.Type = ProxySettings.Type;
                viewModel.Host = ProxySettings.Host;
                viewModel.Port = ProxySettings.Port;
                viewModel.Username = ProxySettings.Username;
                viewModel.Password = ProxySettings.Password;

                await AppService
                    .SettingsService
                    .UpdateProxySettingsAsync(viewModel);
            }
            // Save new proxy settings
            else
            {
                var proxySettingsInDb = await AppService
                    .UnitOfWork
                    .ProxySettingsRepository
                    .GetAsync(where: p => p.Host.ToLower() == ProxySettings.Host.ToLower() &&
                                          p.Port.ToLower() == ProxySettings.Port.ToLower() &&
                                          p.Type.ToLower() == ProxySettings.Type!.ToLower());

                if (proxySettingsInDb != null)
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention",
                        "Unable to save proxy. There is already another proxy with the same type, host and port. Please change the type, host or port or edit the previous proxy.",
                        DialogButtons.Ok);

                    return;
                }

                proxySettingsInDb = await AppService
                    .UnitOfWork
                    .ProxySettingsRepository
                    .GetAsync(where: p => p.Name.ToLower() == ProxySettings.Name!.ToLower());

                if (proxySettingsInDb != null)
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention",
                        "Unable to save proxy. Another proxy with the same name already exists. Please choose a different proxy name or edit the existing proxy.",
                        DialogButtons.Ok);

                    return;
                }

                await AppService
                    .SettingsService
                    .AddProxySettingsAsync(ProxySettings);
            }

            ClearProxy();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving proxy. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task SetSelectedProxyAsync()
    {
        try
        {
            var activeProxies = AvailableProxies
                .Where(p => p.IsActive)
                .ToList();

            foreach (var proxy in activeProxies)
                proxy.IsActive = false;

            var selectedProxy = AppService
                .SettingsService
                .Settings
                .Proxies
                .FirstOrDefault(p => p.Id == SelectedAvailableProxy?.Id);

            var newProxy = selectedProxy?.DeepCopy();
            if (newProxy == null)
                return;

            selectedProxy!.IsActive = true;
            ProxySettings = newProxy;
            EditingProxy = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while setting selected proxy. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        _ = LoadAvailableProxiesAsync();
    }
}
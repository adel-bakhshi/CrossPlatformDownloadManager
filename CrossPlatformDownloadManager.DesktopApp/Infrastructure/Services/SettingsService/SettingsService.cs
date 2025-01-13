using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;

public class SettingsService : PropertyChangedBase, ISettingsService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private bool _isSettingsSets;

    private SettingsViewModel _settings = null!;

    #endregion

    #region Properties

    public SettingsViewModel Settings
    {
        get => _settings;
        private set => SetField(ref _settings, value);
    }

    #endregion

    #region Events

    public event EventHandler? DataChanged;
    public event EventHandler? ActiveProxyChanged;

    #endregion

    public SettingsService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        Settings = new SettingsViewModel();
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            var settingsList = await _unitOfWork
                .SettingsRepository
                .GetAllAsync(includeProperties: "Proxies");

            Settings? settings;
            if (settingsList.Count == 0)
            {
                const string assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/settings.json";
                var assetsUri = new Uri(assetName);
                settings = assetsUri.OpenJsonAsset<Settings>();
                if (settings == null)
                    throw new InvalidOperationException("An error occurred while loading settings.");

                // The first time the settings are saved, the program must be added to Startup.
                if (settings.StartOnSystemStartup)
                    RegisterStartup();

                await _unitOfWork.SettingsRepository.AddAsync(settings);
                await _unitOfWork.SaveAsync();
            }
            else
            {
                settings = settingsList.First();
            }

            var viewModel = _mapper.Map<SettingsViewModel>(settings);
            if (_isSettingsSets)
            {
                // These proxies no longer exist so they should be removed.
                var removedProxies = Settings
                    .Proxies
                    .Where(p => viewModel.Proxies.FirstOrDefault(vp => vp.Id == p.Id) == null)
                    .ToList();

                foreach (var proxy in removedProxies)
                    Settings.Proxies.Remove(proxy);

                // These proxies are new so they should be added.
                var newProxies = viewModel
                    .Proxies
                    .Where(vp => Settings.Proxies.FirstOrDefault(p => p.Id == vp.Id) == null)
                    .ToList();

                foreach (var proxy in newProxies)
                    Settings.Proxies.Add(proxy);
            }
            else
            {
                _isSettingsSets = true;

                viewModel.ProxyMode = ProxyMode.DisableProxy;
                Settings = viewModel;
            }

            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading settings. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public async Task SaveSettingsAsync(SettingsViewModel viewModel, bool reloadData = true)
    {
        var settingsInDb = await _unitOfWork
            .SettingsRepository
            .GetAsync(where: s => s.Id == viewModel.Id);

        if (settingsInDb == null)
            return;

        var settings = _mapper.Map<Settings>(viewModel);
        settingsInDb.UpdateDbModel(settings);

        await _unitOfWork.SettingsRepository.UpdateAsync(settingsInDb);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadSettingsAsync();

        if (Settings.StartOnSystemStartup)
        {
            if (!PlatformSpecificManager.IsStartupRegistered())
                PlatformSpecificManager.RegisterStartup();
        }
        else
        {
            if (PlatformSpecificManager.IsStartupRegistered())
                PlatformSpecificManager.DeleteStartup();
        }
    }

    public async Task<int> AddProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
            return 0;

        await _unitOfWork.ProxySettingsRepository.AddAsync(proxySettings);
        await _unitOfWork.SaveAsync();

        await LoadSettingsAsync();
        return proxySettings.Id;
    }

    public async Task<int> AddProxySettingsAsync(ProxySettingsViewModel? viewModel)
    {
        if (viewModel == null)
            return 0;

        viewModel.SettingsId = Settings.Id;
        var proxySettings = _mapper.Map<ProxySettings>(viewModel);
        return await AddProxySettingsAsync(proxySettings);
    }

    public async Task UpdateProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
            return;

        var proxySettingsInDb = await _unitOfWork
            .ProxySettingsRepository
            .GetAsync(where: p => p.Id == proxySettings.Id);

        if (proxySettingsInDb == null)
            return;

        proxySettingsInDb.UpdateDbModel(proxySettings);
        await _unitOfWork.ProxySettingsRepository.UpdateAsync(proxySettings);
        await _unitOfWork.SaveAsync();

        await LoadSettingsAsync();
    }

    public async Task UpdateProxySettingsAsync(ProxySettingsViewModel? viewModel)
    {
        var proxySettingsViewModel = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel?.Id);

        if (proxySettingsViewModel == null)
            return;

        var proxySettings = _mapper.Map<ProxySettings>(proxySettingsViewModel);
        await UpdateProxySettingsAsync(proxySettings);
    }

    public async Task DeleteProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
            return;

        var proxySettingsInDb = await _unitOfWork
            .ProxySettingsRepository
            .GetAsync(where: p => p.Id == proxySettings.Id);

        if (proxySettingsInDb == null)
            return;

        await _unitOfWork.ProxySettingsRepository.DeleteAsync(proxySettingsInDb);
        await _unitOfWork.SaveAsync();

        await LoadSettingsAsync();
    }

    public async Task DeleteProxySettingsAsync(ProxySettingsViewModel? viewModel)
    {
        var proxySettingsViewModel = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel?.Id);

        if (proxySettingsViewModel == null)
            return;

        var proxySettings = _mapper.Map<ProxySettings>(proxySettingsViewModel);
        await DeleteProxySettingsAsync(proxySettings);
    }

    public async Task DisableProxyAsync()
    {
        Settings.ProxyMode = ProxyMode.DisableProxy;
        await SaveSettingsAsync(Settings);
    }

    public async Task UseSystemProxySettingsAsync()
    {
        Settings.ProxyMode = ProxyMode.UseSystemProxySettings;
        await SaveSettingsAsync(Settings);
    }

    public async Task UseCustomProxyAsync(ProxySettingsViewModel? viewModel)
    {
        var proxySettings = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel?.Id);

        if (proxySettings == null)
            return;

        proxySettings.Type = proxySettings.Type?.Trim();
        proxySettings.Host = proxySettings.Host?.Trim();
        proxySettings.Port = proxySettings.Port?.Trim();
        proxySettings.Username = proxySettings.Username?.Trim();
        proxySettings.Password = proxySettings.Password?.Trim();

        if (proxySettings.Host.IsNullOrEmpty() || proxySettings.Port.IsNullOrEmpty())
            throw new InvalidOperationException("The proxy you selected to activate is not valid. Please go to the Settings window, Proxy section and edit the proxy.");

        if (!int.TryParse(proxySettings.Port, out _))
            throw new InvalidOperationException("The proxy you selected to activate is not valid. Please go to the Settings window, Proxy section and edit the proxy.");

        var proxyType = proxySettings.Type?.ToLower() switch
        {
            "http" => ProxyType.Http,
            "https" => ProxyType.Https,
            "socks 5" => ProxyType.Socks5,
            _ => throw new InvalidOperationException("Invalid proxy type.")
        };

        var activeProxies = Settings
            .Proxies
            .Where(p => p.IsActive)
            .ToList();

        foreach (var proxy in activeProxies)
            proxy.IsActive = false;

        proxySettings = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == proxySettings.Id);

        if (proxySettings == null)
            return;

        proxySettings.IsActive = true;

        Settings.ProxyMode = ProxyMode.UseCustomProxy;
        Settings.ProxyType = proxyType;
        await SaveSettingsAsync(Settings);
    }

    #region Helpers

    private static void RegisterStartup()
    {
        var isRegistered = PlatformSpecificManager.IsStartupRegistered();
        if (isRegistered)
            return;

        PlatformSpecificManager.RegisterStartup();
    }

    #endregion
}
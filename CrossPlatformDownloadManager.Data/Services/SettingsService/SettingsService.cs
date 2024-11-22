using System.Net;
using AutoMapper;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using SocksSharp;
using SocksSharp.Proxy;
using ProxySettings = CrossPlatformDownloadManager.Data.Models.ProxySettings;

namespace CrossPlatformDownloadManager.Data.Services.SettingsService;

public class SettingsService : PropertyChangedBase, ISettingsService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private SettingsViewModel _settings = new();
    private HttpMessageHandler? _proxyHandler;

    #endregion

    #region Properties

    public SettingsViewModel Settings
    {
        get => _settings;
        private set => SetField(ref _settings, value);
    }

    public HttpMessageHandler? ProxyHandler
    {
        get => _proxyHandler;
        private set => SetField(ref _proxyHandler, value);
    }

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    public SettingsService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

                await _unitOfWork.SettingsRepository.AddAsync(settings);
                await _unitOfWork.SaveAsync();
            }
            else
            {
                settings = settingsList.First();
            }

            var viewModel = _mapper.Map<SettingsViewModel>(settings);
            Settings.UpdateData(viewModel);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
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

        _unitOfWork.ProxySettingsRepository.Delete(proxySettingsInDb);
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

    public async Task ActiveProxyAsync(ProxySettingsViewModel? viewModel)
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

        HttpMessageHandler handler;
        ProxyType proxyType;

        var type = proxySettings.Type?.ToLower();
        switch (type)
        {
            case "http":
            case "https":
            {
                proxyType = type.Equals("http") ? ProxyType.Http : ProxyType.Https;
                handler = new HttpClientHandler
                {
                    Proxy = new WebProxy($"{type}://{proxySettings.Host}:{proxySettings.Port}"),
                    UseProxy = true
                };

                break;
            }

            case "socks 5":
            {
                proxyType = ProxyType.Socks5;
                var settings = new SocksSharp.Proxy.ProxySettings
                {
                    Host = proxySettings.Host!,
                    Port = int.Parse(proxySettings.Port!)
                };

                if (!proxySettings.Username.IsNullOrEmpty() && !proxySettings.Password.IsNullOrEmpty())
                    settings.Credentials = new NetworkCredential(proxySettings.Username!, proxySettings.Password!);

                handler = new ProxyClientHandler<Socks5>(settings);
                break;
            }

            default:
                throw new InvalidOperationException("Invalid proxy type.");
        }

        Settings.ProxyMode = ProxyMode.UseCustomProxy;
        Settings.ProxyType = proxyType;
        await SaveSettingsAsync(Settings);

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
        ProxyHandler = handler;
    }

    public async Task DisableProxyAsync()
    {
        if (ProxyHandler == null)
            return;

        Settings.ProxyMode = ProxyMode.DisableProxy;
        await SaveSettingsAsync(Settings);

        ProxyHandler.Dispose();
        ProxyHandler = null;
    }

    public async Task UseSystemProxySettingsAsync()
    {
        var systemProxy = WebRequest.DefaultWebProxy;
        if (systemProxy == null)
            return;

        Settings.ProxyMode = ProxyMode.UseSystemProxySettings;
        await SaveSettingsAsync(Settings);

        ProxyHandler = new HttpClientHandler
        {
            Proxy = systemProxy,
            UseProxy = true
        };
    }
}
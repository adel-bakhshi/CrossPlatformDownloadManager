using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;

/// <summary>
/// Represents the settings service.
/// </summary>
public class SettingsService : PropertyChangedBase, ISettingsService
{
    #region Private Fields

    /// <summary>
    /// The unit of work instance to access database.
    /// </summary>
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// The mapper instance to mapping data.
    /// </summary>
    private readonly IMapper _mapper;

    /// <summary>
    /// Determines whether the settings are set for first time.
    /// </summary>
    private bool _isSettingsSets;

    /// <summary>
    /// Determines the manager window.
    /// </summary>
    private ManagerWindow? _managerWindow;

    // Backing field for properties
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

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work instance to access database.</param>
    /// <param name="mapper">The mapper instance to mapping data.</param>
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
            Log.Information("Loading application settings...");

            // Find settings in database
            var settingsList = await _unitOfWork
                .SettingsRepository
                .GetAllAsync(includeProperties: "Proxies");

            // Create settings if not exists, otherwise use current settings
            Settings? settings;
            if (settingsList.Count == 0)
            {
                Log.Debug("Settings not found, creating new settings...");

                // Get default settings json content
                const string assetName = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/settings.json";
                var assetsUri = new Uri(assetName);
                settings = assetsUri.OpenJsonAsset<Settings>();
                if (settings == null)
                    throw new InvalidOperationException("An error occurred while loading settings.");

                // The first time the settings are saved, the program must be added to Startup.
                if (settings.StartOnSystemStartup)
                    RegisterStartup();

                // Set the default temporary file location
                settings.TemporaryFileLocation = Constants.TempDownloadDirectory;
                // Save settings
                await _unitOfWork.SettingsRepository.AddAsync(settings);
                await _unitOfWork.SaveAsync();
            }
            else
            {
                Log.Debug("Settings found, using current settings...");
                settings = settingsList.First();
            }

            // Convert to view model
            var viewModel = _mapper.Map<SettingsViewModel>(settings);

            // Check if the settings are set for first time
            if (_isSettingsSets)
            {
                Log.Debug("Settings are already set, updating settings...");

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

                // Add proxies to view model
                foreach (var proxy in newProxies)
                    Settings.Proxies.Add(proxy);
            }
            else
            {
                Log.Debug("Settings are being set for the first time, disabling proxy...");

                // Set settings sets flag to true
                _isSettingsSets = true;

                // The proxy must be disabled each time the program is run
                viewModel.ProxyMode = ProxyMode.DisableProxy;
                Settings = viewModel;
            }

            // Check the application startup
            CheckApplicationStartup();
            // Set application font
            SetApplicationFont();

            // Raise changed event
            DataChanged?.Invoke(this, EventArgs.Empty);
            // Log information
            Log.Information("Settings loaded successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading settings. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public async Task SaveSettingsAsync(SettingsViewModel viewModel, bool reloadData = true)
    {
        Log.Information("Saving application settings...");

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

        // Show or hide the manager.
        if (Settings.UseManager)
        {
            if (_managerWindow == null)
                ShowManager();
        }
        else
        {
            HideManager();
        }

        // Check the application startup
        CheckApplicationStartup();
        // Change app theme
        ChangeApplicationTheme();
        // Set application font
        SetApplicationFont();
    }

    public async Task<int> AddProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
            return 0;

        Log.Debug("Adding proxy settings to database...");

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

        Log.Debug("Getting proxy settings with ID {Id} from database to update...", proxySettings.Id);

        var proxySettingsInDb = await _unitOfWork
            .ProxySettingsRepository
            .GetAsync(where: p => p.Id == proxySettings.Id);

        if (proxySettingsInDb == null)
        {
            Log.Debug("There is no roxy settings in database to update.");
            return;
        }

        Log.Debug("Updating proxy settings in database...");

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

        Log.Debug("Getting proxy settings with ID {Id} from database to remove...", proxySettings.Id);

        var proxySettingsInDb = await _unitOfWork
            .ProxySettingsRepository
            .GetAsync(where: p => p.Id == proxySettings.Id);

        if (proxySettingsInDb == null)
        {
            Log.Debug("There is no proxy settings in database to remove.");
            return;
        }

        Log.Debug("Removing proxy settings from database...");

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
        Log.Information("Disabling proxy...");

        Settings.ProxyMode = ProxyMode.DisableProxy;
        await SaveSettingsAsync(Settings);
    }

    public async Task UseSystemProxySettingsAsync()
    {
        Log.Information("Using system proxy settings...");

        Settings.ProxyMode = ProxyMode.UseSystemProxySettings;
        await SaveSettingsAsync(Settings);
    }

    public async Task UseCustomProxyAsync(ProxySettingsViewModel? viewModel)
    {
        Log.Information("Using custom proxy settings...");

        var proxySettings = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel?.Id);

        if (proxySettings == null)
        {
            Log.Debug("There is no proxy settings with ID {Id} to activate.", viewModel?.Id.ToString());
            return;
        }

        Log.Debug("Setting proxy settings with ID {Id} as active...", proxySettings.Id);

        proxySettings.Type = proxySettings.Type?.Trim();
        proxySettings.Host = proxySettings.Host?.Trim();
        proxySettings.Port = proxySettings.Port?.Trim();
        proxySettings.Username = proxySettings.Username?.Trim();
        proxySettings.Password = proxySettings.Password?.Trim();

        if (proxySettings.Host.IsStringNullOrEmpty()
            || proxySettings.Port.IsStringNullOrEmpty()
            || !int.TryParse(proxySettings.Port, out _))
        {
            throw new InvalidOperationException("The proxy you selected to activate is not valid. Please go to the Settings window, Proxy section and edit the proxy.");
        }

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

        Log.Information("Custom proxy settings activated successfully.");
    }

    public void ShowManager()
    {
        // Run on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            Log.Information("Showing manager window...");

            // Check if manager window is null
            if (_managerWindow == null)
            {
                Log.Debug("Manager window is null. Creating new manager window...");

                // Get service provider
                var serviceProvider = Application.Current?.GetServiceProvider();
                var appService = serviceProvider?.GetService<IAppService>();
                var trayMenuWindow = serviceProvider?.GetService<TrayMenuWindow>();
                // Check if app service and tray menu window are not null
                if (appService == null || trayMenuWindow == null)
                    throw new InvalidOperationException("App service or tray menu window not found.");

                // Create and show manager window
                var vm = new ManagerWindowViewModel(appService, trayMenuWindow);
                _managerWindow = new ManagerWindow { DataContext = vm };
            }

            _managerWindow!.Show();
        });
    }

    public void HideManager()
    {
        // Run on UI thread
        Dispatcher.UIThread.Post(() =>
        {
            Log.Information("Closing manager window...");

            _managerWindow?.Close();
            _managerWindow = null;
        });
    }

    public string GetTemporaryFileLocation()
    {
        Log.Information("Getting temporary file location...");

        var location = Settings.TemporaryFileLocation;
        if (location.IsStringNullOrEmpty())
            location = Constants.TempDownloadDirectory;

        return location;
    }

    #region Helpers

    /// <summary>
    /// Checks the application startup settings and registers or deletes the program from system startup accordingly
    /// </summary>
    private void CheckApplicationStartup()
    {
        // Log the start of the startup check process
        Log.Debug("Checking application startup...");

        // Register or delete the program from Startup based on the settings
        // If the StartOnSystemStartup setting is true, register the application for startup,
        // Otherwise, remove the application from startup.
        if (Settings.StartOnSystemStartup)
        {
            RegisterStartup();
        }
        else
        {
            DeleteStartup();
        }
    }

    /// <summary>
    /// Registers the application to start automatically when the system boots.
    /// This method checks if the application is already registered as a startup item,
    /// and if not, it registers it using platform-specific implementations.
    /// </summary>
    private static void RegisterStartup()
    {
        // Check if the application is already registered as a startup item
        var isRegistered = PlatformSpecificManager.IsStartupRegistered();
        if (isRegistered)
        {
            Log.Debug("Application is already registered as a startup item.");
            return;
        }

        PlatformSpecificManager.RegisterStartup();
    }

    /// <summary>
    /// Method to delete startup configuration for the application
    /// This method serves as a wrapper for platform-specific implementation
    /// </summary>
    private static void DeleteStartup()
    {
        PlatformSpecificManager.DeleteStartup();
    }

    /// <summary>
    /// Changes the application theme by loading theme data through the theme service.
    /// </summary>
    /// <remarks>
    /// This method retrieves the application theme service from the service provider
    /// and invokes the LoadThemeData method to apply the theme changes.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the IAppThemeService cannot be found in the service provider.
    /// </exception>
    private static void ChangeApplicationTheme()
    {
        Log.Debug("Changing application theme...");

        // Get the app service from service provider and check if exists
        var serviceProvider = Application.Current?.GetServiceProvider();
        var appThemeService = serviceProvider?.GetService<IAppThemeService>();
        if (appThemeService == null)
            throw new InvalidOperationException("Can't find app theme service.");

        // Load and apply the theme data
        appThemeService.LoadThemeData();
    }

    /// <summary>
    /// Sets the application font based on the settings or defaults to the first available font if not found.
    /// This method attempts to find the specified font in the application resources and updates the primary font resource if necessary.
    /// </summary>
    private void SetApplicationFont()
    {
        Log.Debug("Changing application font...");

        // Find the specified font name in available fonts, or default to the first available font if not found
        var font = Constants.AvailableFonts.Find(f => f.Equals(Settings.ApplicationFont)) ?? Constants.AvailableFonts.FirstOrDefault();
        // Try to find the font in application resources
        if (Application.Current?.TryFindResource(font!, out var resource) != true || resource == null)
            return;

        // Check if primary font resource exists and is different from the current font resource
        if (Application.Current.TryFindResource("PrimaryFont", out var primaryFont) && primaryFont?.Equals(resource) != true)
            Application.Current.Resources["PrimaryFont"] = resource;
    }

    #endregion
}
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

        Log.Debug("SettingsService initialized successfully");
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

            Log.Debug("Retrieved {SettingsCount} settings records from database", settingsList.Count);

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
                {
                    Log.Error("Failed to load default settings from asset: {AssetName}", assetName);
                    throw new InvalidOperationException("An error occurred while loading settings.");
                }

                Log.Debug("Loaded default settings from asset successfully");

                // The first time the settings are saved, the program must be added to Startup.
                if (settings.StartOnSystemStartup)
                {
                    Log.Debug("Registering application for system startup as per default settings");
                    RegisterStartup();
                }

                // Set the default temporary file location
                settings.TemporaryFileLocation = Constants.TempDownloadDirectory;
                Log.Debug("Set temporary file location to: {TempLocation}", Constants.TempDownloadDirectory);

                // Save settings
                await _unitOfWork.SettingsRepository.AddAsync(settings);
                await _unitOfWork.SaveAsync();

                Log.Debug("New settings created and saved to database with ID: {SettingsId}", settings.Id);
            }
            else
            {
                Log.Debug("Settings found, using current settings...");
                settings = settingsList.First();
                Log.Debug("Using existing settings with ID: {SettingsId}", settings.Id);
            }

            // Convert to view model
            var viewModel = _mapper.Map<SettingsViewModel>(settings);
            Log.Debug("Mapped settings to view model successfully");

            // Check if the settings are set for first time
            if (_isSettingsSets)
            {
                Log.Debug("Settings are already set, updating settings...");

                // These proxies no longer exist so they should be removed.
                var removedProxies = Settings
                    .Proxies
                    .Where(p => viewModel.Proxies.FirstOrDefault(vp => vp.Id == p.Id) == null)
                    .ToList();

                Log.Debug("Found {RemovedProxyCount} proxies to remove", removedProxies.Count);

                foreach (var proxy in removedProxies)
                {
                    Settings.Proxies.Remove(proxy);
                    Log.Debug("Removed proxy with ID: {ProxyId}", proxy.Id);
                }

                // These proxies are new so they should be added.
                var newProxies = viewModel
                    .Proxies
                    .Where(vp => Settings.Proxies.FirstOrDefault(p => p.Id == vp.Id) == null)
                    .ToList();

                Log.Debug("Found {NewProxyCount} new proxies to add", newProxies.Count);

                // Add proxies to view model
                foreach (var proxy in newProxies)
                {
                    Settings.Proxies.Add(proxy);
                    Log.Debug("Added new proxy with ID: {ProxyId}", proxy.Id);
                }
            }
            else
            {
                Log.Debug("Settings are being set for the first time, disabling proxy...");

                // Set settings sets flag to true
                _isSettingsSets = true;

                // The proxy must be disabled each time the program is run
                viewModel.ProxyMode = ProxyMode.DisableProxy;
                Settings = viewModel;

                Log.Debug("Initial settings applied with {ProxyCount} proxies", viewModel.Proxies.Count);
            }

            // Check the application startup
            CheckApplicationStartup();
            // Set application font
            SetApplicationFont();

            // Raise changed event
            DataChanged?.Invoke(this, EventArgs.Empty);
            // Log information
            Log.Information("Settings loaded successfully. Total proxies: {ProxyCount}", Settings.Proxies.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading settings. Error message: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    public async Task SaveSettingsAsync(SettingsViewModel viewModel, bool reloadData = true)
    {
        Log.Information("Saving application settings...");
        Log.Debug("Settings ID: {SettingsId}, Reload data: {ReloadData}", viewModel.Id, reloadData);

        var settingsInDb = await _unitOfWork
            .SettingsRepository
            .GetAsync(where: s => s.Id == viewModel.Id);

        if (settingsInDb == null)
        {
            Log.Warning("Settings with ID {SettingsId} not found in database", viewModel.Id);
            return;
        }

        Log.Debug("Found settings in database, updating...");

        var settings = _mapper.Map<Settings>(viewModel);
        settingsInDb.UpdateDbModel(settings);

        await _unitOfWork.SettingsRepository.UpdateAsync(settingsInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Settings updated successfully in database");

        if (reloadData)
        {
            Log.Debug("Reloading settings after save");
            await LoadSettingsAsync();
        }

        // Show or hide the manager.
        if (Settings.UseManager)
        {
            if (_managerWindow == null)
            {
                Log.Debug("Manager is enabled but window is null, showing manager...");
                ShowManager();
            }
            else
            {
                Log.Debug("Manager is already enabled and window exists");
            }
        }
        else
        {
            Log.Debug("Manager is disabled, hiding manager...");
            HideManager();
        }

        // Check the application startup
        CheckApplicationStartup();
        // Change app theme
        ChangeApplicationTheme();
        // Set application font
        SetApplicationFont();

        Log.Information("Settings saved successfully");
    }

    public async Task<int> AddProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
        {
            Log.Warning("Attempted to add null proxy settings");
            return 0;
        }

        Log.Information("Adding proxy settings to database...");
        Log.Debug("Proxy name: {ProxyName}, Type: {ProxyType}", proxySettings.Name, proxySettings.Type);

        await _unitOfWork.ProxySettingsRepository.AddAsync(proxySettings);
        await _unitOfWork.SaveAsync();

        Log.Debug("Proxy settings added successfully with ID: {ProxyId}", proxySettings.Id);

        await LoadSettingsAsync();
        return proxySettings.Id;
    }

    public async Task<int> AddProxySettingsAsync(ProxySettingsViewModel? viewModel)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to add null proxy settings view model");
            return 0;
        }

        Log.Debug("Adding proxy settings from view model...");

        viewModel.SettingsId = Settings.Id;
        var proxySettings = _mapper.Map<ProxySettings>(viewModel);
        return await AddProxySettingsAsync(proxySettings);
    }

    public async Task UpdateProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
        {
            Log.Warning("Attempted to update null proxy settings");
            return;
        }

        Log.Information("Updating proxy settings with ID: {ProxyId}", proxySettings.Id);

        var proxySettingsInDb = await _unitOfWork
            .ProxySettingsRepository
            .GetAsync(where: p => p.Id == proxySettings.Id);

        if (proxySettingsInDb == null)
        {
            Log.Warning("Proxy settings with ID {ProxyId} not found in database", proxySettings.Id);
            return;
        }

        Log.Debug("Found proxy settings in database, updating...");

        proxySettingsInDb.UpdateDbModel(proxySettings);
        await _unitOfWork.ProxySettingsRepository.UpdateAsync(proxySettings);
        await _unitOfWork.SaveAsync();

        Log.Debug("Proxy settings updated successfully");

        await LoadSettingsAsync();
    }

    public async Task UpdateProxySettingsAsync(ProxySettingsViewModel? viewModel)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to update null proxy settings view model");
            return;
        }

        Log.Debug("Updating proxy settings from view model with ID: {ProxyId}", viewModel.Id);

        var proxySettingsViewModel = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel.Id);

        if (proxySettingsViewModel == null)
        {
            Log.Warning("Proxy settings with ID {ProxyId} not found in current settings", viewModel.Id);
            return;
        }

        var proxySettings = _mapper.Map<ProxySettings>(proxySettingsViewModel);
        await UpdateProxySettingsAsync(proxySettings);
    }

    public async Task DeleteProxySettingsAsync(ProxySettings? proxySettings)
    {
        if (proxySettings == null)
        {
            Log.Warning("Attempted to delete null proxy settings");
            return;
        }

        Log.Information("Deleting proxy settings with ID: {ProxyId}", proxySettings.Id);

        var proxySettingsInDb = await _unitOfWork
            .ProxySettingsRepository
            .GetAsync(where: p => p.Id == proxySettings.Id);

        if (proxySettingsInDb == null)
        {
            Log.Warning("Proxy settings with ID {ProxyId} not found in database", proxySettings.Id);
            return;
        }

        Log.Debug("Found proxy settings in database, deleting...");

        await _unitOfWork.ProxySettingsRepository.DeleteAsync(proxySettingsInDb);
        await _unitOfWork.SaveAsync();

        Log.Debug("Proxy settings deleted successfully");

        await LoadSettingsAsync();
    }

    public async Task DeleteProxySettingsAsync(ProxySettingsViewModel? viewModel)
    {
        if (viewModel == null)
        {
            Log.Warning("Attempted to delete null proxy settings view model");
            return;
        }

        Log.Debug("Deleting proxy settings from view model with ID: {ProxyId}", viewModel.Id);

        var proxySettingsViewModel = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel.Id);

        if (proxySettingsViewModel == null)
        {
            Log.Warning("Proxy settings with ID {ProxyId} not found in current settings", viewModel.Id);
            return;
        }

        var proxySettings = _mapper.Map<ProxySettings>(proxySettingsViewModel);
        await DeleteProxySettingsAsync(proxySettings);
    }

    public async Task DisableProxyAsync()
    {
        Log.Information("Disabling proxy...");

        Settings.ProxyMode = ProxyMode.DisableProxy;
        await SaveSettingsAsync(Settings);

        Log.Information("Proxy disabled successfully");
    }

    public async Task UseSystemProxySettingsAsync()
    {
        Log.Information("Using system proxy settings...");

        Settings.ProxyMode = ProxyMode.UseSystemProxySettings;
        await SaveSettingsAsync(Settings);

        Log.Information("System proxy settings applied successfully");
    }

    public async Task UseCustomProxyAsync(ProxySettingsViewModel? viewModel)
    {
        Log.Information("Using custom proxy settings...");

        if (viewModel == null)
        {
            Log.Warning("Attempted to use null custom proxy settings");
            return;
        }

        Log.Debug("Activating custom proxy with ID: {ProxyId}", viewModel.Id);

        var proxySettings = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == viewModel.Id);

        if (proxySettings == null)
        {
            Log.Warning("Proxy settings with ID {ProxyId} not found in current settings", viewModel.Id);
            return;
        }

        Log.Debug("Setting proxy settings with ID {ProxyId} as active...", proxySettings.Id);

        // Trim all proxy settings values
        proxySettings.Type = proxySettings.Type?.Trim();
        proxySettings.Host = proxySettings.Host?.Trim();
        proxySettings.Port = proxySettings.Port?.Trim();
        proxySettings.Username = proxySettings.Username?.Trim();
        proxySettings.Password = proxySettings.Password?.Trim();

        Log.Debug("Trimmed proxy settings - Type: {ProxyType}, Host: {ProxyHost}, Port: {ProxyPort}",
            proxySettings.Type, proxySettings.Host, proxySettings.Port);

        // Validate proxy settings
        if (proxySettings.Host.IsStringNullOrEmpty()
            || proxySettings.Port.IsStringNullOrEmpty()
            || !int.TryParse(proxySettings.Port, out _))
        {
            Log.Error("Invalid proxy settings - Host: {Host}, Port: {Port}", proxySettings.Host, proxySettings.Port);
            throw new InvalidOperationException("The proxy you selected to activate is not valid. Please go to the Settings window, Proxy section and edit the proxy.");
        }

        // Determine proxy type
        var proxyType = proxySettings.Type?.ToLower() switch
        {
            "http" => ProxyType.Http,
            "https" => ProxyType.Https,
            "socks 5" => ProxyType.Socks5,
            _ => throw new InvalidOperationException("Invalid proxy type.")
        };

        Log.Debug("Determined proxy type: {ProxyType}", proxyType);

        // Deactivate all other proxies
        var activeProxies = Settings
            .Proxies
            .Where(p => p.IsActive)
            .ToList();

        Log.Debug("Found {ActiveProxyCount} active proxies to deactivate", activeProxies.Count);

        foreach (var proxy in activeProxies)
        {
            proxy.IsActive = false;
            Log.Debug("Deactivated proxy with ID: {ProxyId}", proxy.Id);
        }

        // Find and activate the target proxy
        proxySettings = Settings
            .Proxies
            .FirstOrDefault(p => p.Id == proxySettings.Id);

        if (proxySettings == null)
        {
            Log.Warning("Proxy settings with ID {ProxyId} not found after deactivating other proxies", viewModel.Id);
            return;
        }

        proxySettings.IsActive = true;
        Log.Debug("Activated proxy with ID: {ProxyId}", proxySettings.Id);

        Settings.ProxyMode = ProxyMode.UseCustomProxy;
        Settings.ProxyType = proxyType;
        await SaveSettingsAsync(Settings);

        Log.Information("Custom proxy settings activated successfully for proxy: {ProxyName}", proxySettings.Name);
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
                {
                    Log.Error("App service or tray menu window not found in service provider");
                    throw new InvalidOperationException("App service or tray menu window not found.");
                }

                Log.Debug("Creating manager window view model and window...");

                // Create and show manager window
                var vm = new ManagerWindowViewModel(appService, trayMenuWindow);
                _managerWindow = new ManagerWindow { DataContext = vm };

                Log.Debug("Manager window created successfully");
            }

            _managerWindow!.Show();
            Log.Information("Manager window shown successfully");
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

            Log.Information("Manager window closed successfully");
        });
    }

    public string GetTemporaryFileLocation()
    {
        Log.Debug("Getting temporary file location...");

        var location = Settings.TemporaryFileLocation;
        if (location.IsStringNullOrEmpty())
        {
            location = Constants.TempDownloadDirectory;
            Log.Debug("Using default temporary file location: {TempLocation}", location);
        }
        else
        {
            Log.Debug("Using configured temporary file location: {TempLocation}", location);
        }

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
            Log.Debug("Start on system startup is enabled, registering startup...");
            RegisterStartup();
        }
        else
        {
            Log.Debug("Start on system startup is disabled, deleting startup...");
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
        Log.Debug("Registering application for system startup...");

        // Check if the application is already registered as a startup item
        var isRegistered = PlatformSpecificManager.IsStartupRegistered();
        if (isRegistered)
        {
            Log.Debug("Application is already registered as a startup item.");
            return;
        }

        Log.Debug("Application is not registered for startup, registering now...");
        PlatformSpecificManager.RegisterStartup();
        Log.Information("Application registered for system startup successfully");
    }

    /// <summary>
    /// Method to delete startup configuration for the application
    /// This method serves as a wrapper for platform-specific implementation
    /// </summary>
    private static void DeleteStartup()
    {
        Log.Debug("Deleting application from system startup...");

        PlatformSpecificManager.DeleteStartup();
        Log.Information("Application removed from system startup successfully");
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
        {
            Log.Error("App theme service not found in service provider");
            throw new InvalidOperationException("Can't find app theme service.");
        }

        Log.Debug("App theme service found, loading theme data...");

        // Load and apply the theme data
        appThemeService.LoadThemeData();

        Log.Debug("Application theme changed successfully");
    }

    /// <summary>
    /// Sets the application font based on the settings or defaults to the first available font if not found.
    /// This method attempts to find the specified font in the application resources and updates the primary font resource if necessary.
    /// </summary>
    private void SetApplicationFont()
    {
        Log.Debug("Changing application font...");

        // Find the specified font name in available fonts, or default to the first available font if not found
        var fontName = Constants.AvailableFonts.Find(f => f.Equals(Settings.ApplicationFont)) ?? Constants.AvailableFonts.FirstOrDefault();
        if (fontName.IsStringNullOrEmpty())
        {
            Log.Warning("No available fonts found");
            return;
        }

        Log.Debug("Selected font: {FontName}", fontName);

        // Try to find the font in application resources
        if (Application.Current?.TryFindResource(fontName!, out var resource) != true || resource == null)
        {
            Log.Warning("Font resource '{FontName}' not found in application resources", fontName);
            return;
        }

        Log.Debug("Font resource found for: {FontName}", fontName);

        // Check if primary font resource exists and is different from the current font resource
        if (Application.Current.TryFindResource("PrimaryFont", out var primaryFont) && primaryFont?.Equals(resource) != true)
        {
            Application.Current.Resources["PrimaryFont"] = resource;
            Log.Debug("Primary font resource updated to: {FontName}", fontName);
        }
        else
        {
            Log.Debug("Primary font resource is already set to: {FontName}", fontName);
        }
    }

    #endregion
}
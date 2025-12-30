using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings.Views;

public class GeneralsViewModel : ViewModelBase
{
    #region Private Fields

    // Backing fields for properties
    private bool _startOnSystemStartup;
    private bool _useBrowserExtension;
    private bool _useManager;
    private bool _alwaysKeepManagerOnTop;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether the app should start on system startup.
    /// </summary>
    public bool StartOnSystemStartup
    {
        get => _startOnSystemStartup;
        set => this.RaiseAndSetIfChanged(ref _startOnSystemStartup, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the app should use browser extension.
    /// </summary>
    public bool UseBrowserExtension
    {
        get => _useBrowserExtension;
        set => this.RaiseAndSetIfChanged(ref _useBrowserExtension, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the app should use manager.
    /// </summary>
    public bool UseManager
    {
        get => _useManager;
        set => this.RaiseAndSetIfChanged(ref _useManager, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the manager should always keep on top.
    /// </summary>
    public bool AlwaysKeepManagerOnTop
    {
        get => _alwaysKeepManagerOnTop;
        set => this.RaiseAndSetIfChanged(ref _alwaysKeepManagerOnTop, value);
    }

    #endregion

    public GeneralsViewModel(IAppService appService) : base(appService)
    {
        LoadViewData();
    }

    #region Helpers

    /// <summary>
    /// Loads the data that needed for the view.
    /// </summary>
    private void LoadViewData()
    {
        var settings = AppService.SettingsService.Settings;
        StartOnSystemStartup = settings.StartOnSystemStartup;
        UseBrowserExtension = settings.UseBrowserExtension;
        UseManager = settings.UseManager;
        AlwaysKeepManagerOnTop = settings.AlwaysKeepManagerOnTop;
    }

    #endregion
}
using System;
using CrossPlatformDownloadManager.Data.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class GeneralsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _startOnSystemStartup;
    private bool _useBrowserExtension;
    private bool _darkMode;
    private bool _alwaysKeepManagerOnTop;

    #endregion

    #region Properties

    public bool StartOnSystemStartup
    {
        get => _startOnSystemStartup;
        set => this.RaiseAndSetIfChanged(ref _startOnSystemStartup, value);
    }
    
    public bool UseBrowserExtension
    {
        get => _useBrowserExtension;
        set => this.RaiseAndSetIfChanged(ref _useBrowserExtension, value);
    }
    
    public bool DarkMode
    {
        get => _darkMode;
        set => this.RaiseAndSetIfChanged(ref _darkMode, value);
    }

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

    private void LoadViewData()
    {
        var settings = AppService.SettingsService.Settings;
        StartOnSystemStartup = settings.StartOnSystemStartup;
        UseBrowserExtension = settings.UseBrowserExtension;
        DarkMode = settings.DarkMode;
        AlwaysKeepManagerOnTop = settings.AlwaysKeepManagerOnTop;
    }

    #endregion
}
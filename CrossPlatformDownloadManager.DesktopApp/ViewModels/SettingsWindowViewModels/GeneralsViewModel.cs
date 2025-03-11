using System.Collections.ObjectModel;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class GeneralsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _startOnSystemStartup;
    private bool _useBrowserExtension;
    private bool _darkMode;
    private bool _useManager;
    private bool _alwaysKeepManagerOnTop;
    private ObservableCollection<string> _fonts = [];
    private string? _selectedFont;

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
    
    public bool UseManager
    {
        get => _useManager;
        set => this.RaiseAndSetIfChanged(ref _useManager, value);
    }

    public bool AlwaysKeepManagerOnTop
    {
        get => _alwaysKeepManagerOnTop;
        set => this.RaiseAndSetIfChanged(ref _alwaysKeepManagerOnTop, value);
    }
    
    public ObservableCollection<string> Fonts
    {
        get => _fonts;
        set => this.RaiseAndSetIfChanged(ref _fonts, value);
    }
    
    public string? SelectedFont
    {
        get => _selectedFont;
        set => this.RaiseAndSetIfChanged(ref _selectedFont, value);
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
        UseManager = settings.UseManager;
        AlwaysKeepManagerOnTop = settings.AlwaysKeepManagerOnTop;
        
        // Load fonts
        Fonts = Constants.AvailableFonts.ToObservableCollection();
        SelectedFont = Constants.AvailableFonts.Find(f => f.Equals(settings.ApplicationFont)) ?? Fonts.FirstOrDefault();
    }

    #endregion
}
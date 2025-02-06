using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AppViewModel : ViewModelBase
{
    #region Private Fields

    private bool _isTrayIconVisible;

    #endregion

    #region Properties

    public bool IsTrayIconVisible
    {
        get => _isTrayIconVisible;
        set => this.RaiseAndSetIfChanged(ref _isTrayIconVisible, value);
    }

    #endregion

    public AppViewModel(IAppService appService) : base(appService)
    {
        IsTrayIconVisible = !AppService.SettingsService.Settings.UseManager;
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        
        IsTrayIconVisible = !AppService.SettingsService.Settings.UseManager;
        this.RaisePropertyChanged(nameof(IsTrayIconVisible));
    }
}
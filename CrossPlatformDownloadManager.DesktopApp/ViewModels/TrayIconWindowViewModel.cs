using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class TrayIconWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly TrayMenuWindow _trayMenuWindow;

    #endregion

    #region Properties

    public bool IsMenuVisible { get; set; }

    #endregion

    public TrayIconWindowViewModel(IAppService appService, TrayMenuWindow trayMenuWindow) : base(appService)
    {
        _trayMenuWindow = trayMenuWindow;
    }

    public void ShowMenu(Window? owner)
    {
        if (owner == null)
            return;

        IsMenuVisible = true;
        _trayMenuWindow.OwnerWindow = owner;
        _trayMenuWindow.Show();
    }

    public void HideMenu()
    {
        _trayMenuWindow.Hide();
        IsMenuVisible = false;
    }
}
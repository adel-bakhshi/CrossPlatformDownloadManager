using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class TrayIconWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly TrayMenuWindow _trayMenuWindow;
    private readonly DispatcherTimer _updateDownloadSpeedTimer;

    private string _downloadSpeed = "0 KB";

    #endregion

    #region Properties
    
    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set => this.RaiseAndSetIfChanged(ref _downloadSpeed, value);
    }

    public bool IsMenuVisible { get; set; }

    #endregion

    #region Commands

    public ICommand ExitProgramCommand { get; }

    #endregion

    public TrayIconWindowViewModel(IAppService appService, TrayMenuWindow trayMenuWindow) : base(appService)
    {
        _trayMenuWindow = trayMenuWindow;
        
        _updateDownloadSpeedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateDownloadSpeedTimer.Tick += UpdateDownloadSpeedTimerOnTick;
        _updateDownloadSpeedTimer.Start();

        ExitProgramCommand = ReactiveCommand.CreateFromTask(ExitProgramAsync);
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

    #region Helpers
    
    private async Task ExitProgramAsync()
    {
        try
        {
            if (App.Desktop == null)
                return;

            var result = await ShowInfoDialogAsync("Exit", "Are you sure you want to exit the app?", DialogButtons.YesNo);
            if (result != DialogResult.Yes)
                return;

            App.Desktop.Shutdown();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }

    private void UpdateDownloadSpeedTimerOnTick(object? sender, EventArgs e)
    {
        DownloadSpeed = AppService
            .DownloadFileService
            .GetDownloadSpeed();
    }

    #endregion
}
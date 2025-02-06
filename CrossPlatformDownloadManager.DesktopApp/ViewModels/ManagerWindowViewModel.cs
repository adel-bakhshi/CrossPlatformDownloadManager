using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class ManagerWindowViewModel : ViewModelBase
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
    public PointViewModel? ManagerPoint => AppService.SettingsService.Settings.ManagerPoint;
    public bool UseManager => AppService.SettingsService.Settings.UseManager;
    public bool AlwaysKeepManagerOnTop => AppService.SettingsService.Settings.AlwaysKeepManagerOnTop;

    #endregion

    #region Commands

    public ICommand ExitProgramCommand { get; }

    #endregion

    public ManagerWindowViewModel(IAppService appService, TrayMenuWindow trayMenuWindow) : base(appService)
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

    public async Task SaveManagerPointAsync(int x, int y)
    {
        // Check current point and compare with new
        var currentPoint = AppService.SettingsService.Settings.ManagerPoint;
        if (currentPoint != null && (int)currentPoint.X == x && (int)currentPoint.Y == y)
            return;

        AppService.SettingsService.Settings.ManagerPoint = new PointViewModel { X = x, Y = y };

        await AppService
            .SettingsService
            .SaveSettingsAsync(AppService.SettingsService.Settings);
    }

    #region Helpers

    private static async Task ExitProgramAsync()
    {
        try
        {
            if (App.Desktop == null)
                return;

            var result = await DialogBoxManager.ShowInfoDialogAsync("Exit", "Are you sure you want to exit the app?", DialogButtons.YesNo);
            if (result != DialogResult.Yes)
                return;

            App.Desktop.Shutdown();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while exit the app. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void UpdateDownloadSpeedTimerOnTick(object? sender, EventArgs e)
    {
        DownloadSpeed = AppService
            .DownloadFileService
            .GetDownloadSpeed();
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        
        this.RaisePropertyChanged(nameof(UseManager));
        this.RaisePropertyChanged(nameof(AlwaysKeepManagerOnTop));
    }

    #endregion
}
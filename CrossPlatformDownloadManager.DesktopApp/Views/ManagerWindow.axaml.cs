using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class ManagerWindow : MyWindowBase<ManagerWindowViewModel>
{
    #region Private Fields

    private readonly DispatcherTimer _saveManagerPointTimer;

    #endregion

    public ManagerWindow()
    {
        _saveManagerPointTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _saveManagerPointTimer.Tick += SaveManagerPointTimerOnTick;

        InitializeComponent();

        PositionChanged += ManagerWindowOnPositionChanged;
    }

    private void ManagerWindowOnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (Screens.Primary == null)
            return;

        var screenWidth = Screens.Primary.WorkingArea.Width;
        var screenHeight = Screens.Primary.WorkingArea.Height;

        var x = (int)Math.Clamp(Position.X, 0, screenWidth - Bounds.Width);
        var y = (int)Math.Clamp(Position.Y, 0, screenHeight - Bounds.Height);

        Position = new PixelPoint(x, y);

        // Restart timer
        _saveManagerPointTimer.Stop();
        _saveManagerPointTimer.Start();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Make sure ViewModel and Screens.Primary are not null
        if (Screens.Primary == null || ViewModel == null)
            return;

        // Subscribe to PropertyChanged event and keep an eye on UseManager property
        ViewModel.PropertyChanged += ManagerWindowViewModelOnPropertyChanged;
        
        // Check for show or hide manager
        if (ViewModel.UseManager)
            Show();
        else
            Hide();

        // Calculate position and set
        int x, y;
        var point = ViewModel.ManagerPoint;
        if (point == null)
        {
            var screenWidth = Screens.Primary.WorkingArea.Width;
            var screenHeight = Screens.Primary.WorkingArea.Height;

            x = (int)(screenWidth - Bounds.Width) - 20;
            y = (int)(screenHeight - Bounds.Height) - 20;
        }
        else
        {
            x = (int)point.X;
            y = (int)point.Y;
        }

        Position = new PixelPoint(x, y);
    }

    private void ManagerWindowViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel == null)
            return;

        switch (e.PropertyName)
        {
            case nameof(ViewModel.UseManager):
            {
                if (ViewModel.UseManager)
                    Show();
                else
                    Hide();
                
                break;
            }
        }
    }

    private void ManagerWindowOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel == null)
            return;

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (ViewModel.IsMenuVisible)
                ViewModel.HideMenu();
            else
                ViewModel.ShowMenu(this);
        }
        else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);

            if (ViewModel.IsMenuVisible)
                ViewModel.HideMenu();
        }
    }

    private async void CDMTextBlockOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            var mainWindow = App.Desktop?.MainWindow;
            if (mainWindow == null)
                throw new InvalidOperationException("Could not find main window.");

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to open main window.");
        }
    }

    #region Helpers

    private async void SaveManagerPointTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            _saveManagerPointTimer.Stop();
            if (ViewModel == null)
                return;

            await ViewModel.SaveManagerPointAsync(Position.X, Position.Y);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to save manager point.");
        }
    }

    #endregion
}
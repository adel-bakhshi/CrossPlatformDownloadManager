using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class TrayIconWindow : MyWindowBase<TrayIconWindowViewModel>
{
    public TrayIconWindow()
    {
        InitializeComponent();

        PositionChanged += WindowOnPositionChanged;
    }

    private void WindowOnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (Screens.Primary == null)
            return;

        var screenWidth = Screens.Primary.Bounds.Width;
        var screenHeight = Screens.Primary.Bounds.Height;

        var x = (int)Math.Clamp(Position.X, 0, screenWidth - Bounds.Width);
        var y = (int)Math.Clamp(Position.Y, 0, screenHeight - Bounds.Height);

        Position = new PixelPoint(x, y);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Screens.Primary == null)
            return;

        var screenWidth = Screens.Primary.WorkingArea.Width;
        var screenHeight = Screens.Primary.WorkingArea.Height;

        var x = (int)(screenWidth - Bounds.Width) - 20;
        var y = (int)(screenHeight - Bounds.Height) - 20;

        Position = new PixelPoint(x, y);
    }

    private void Window_OnPointerPressed(object? sender, PointerPressedEventArgs e)
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
}
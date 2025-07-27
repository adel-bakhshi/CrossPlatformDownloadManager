using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class ManagerWindow : MyWindowBase<ManagerWindowViewModel>
{
    #region Private Fields

    private readonly Debouncer _saveManagerPointDebouncer;

    #endregion

    public ManagerWindow()
    {
        _saveManagerPointDebouncer = new Debouncer(TimeSpan.FromSeconds(2));

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

        // Run debouncer to save manager point
        _saveManagerPointDebouncer.RunAsync(SaveManagerPointAsync);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Make sure ViewModel and Screens.Primary are not null
        if (Screens.Primary == null || ViewModel == null)
            return;

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
            Log.Error(ex, "An error occurred while trying to open main window.");
        }
    }

    #region Helpers

    private async Task SaveManagerPointAsync()
    {
        try
        {
            if (ViewModel == null)
                return;

            await ViewModel.SaveManagerPointAsync(Position.X, Position.Y);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to save manager point.");
        }
    }

    #endregion
}
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

/// <summary>
/// Represents a manager window that extends the base window functionality with specific features for a manager.
/// </summary>
public partial class ManagerWindow : MyWindowBase<ManagerWindowViewModel>
{
    #region Private Fields

    /// <summary>
    /// A debouncer instance used to delay saving the manager point position.
    /// </summary>
    private readonly Debouncer _saveManagerPointDebouncer;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagerWindow"/> class.
    /// </summary>
    public ManagerWindow()
    {
        // Initialize the debouncer with a 2-second delay
        _saveManagerPointDebouncer = new Debouncer(TimeSpan.FromSeconds(2));

        InitializeComponent();

        // Subscribe to the position changed event
        PositionChanged += ManagerWindowOnPositionChanged;
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
            // If no saved point, position in bottom right corner with some margin
            var screenWidth = Screens.Primary.WorkingArea.Width;
            var screenHeight = Screens.Primary.WorkingArea.Height;

            x = (int)(screenWidth - Bounds.Width) - 20;
            y = (int)(screenHeight - Bounds.Height) - 20;
        }
        else
        {
            // Use saved position
            x = (int)point.X;
            y = (int)point.Y;
        }

        // Set the window position
        Position = new PixelPoint(x, y);
    }

    /// <summary>
    /// Handles the position changed event for the window.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments containing the new position.</param>
    private void ManagerWindowOnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Return if primary screen is not available
        if (Screens.Primary == null)
            return;

        // Get the screen dimensions
        var screenWidth = Screens.Primary.WorkingArea.Width;
        var screenHeight = Screens.Primary.WorkingArea.Height;

        // Calculate clamped position to ensure window stays within screen bounds
        var x = (int)Math.Clamp(Position.X, 0, screenWidth - Bounds.Width);
        var y = (int)Math.Clamp(Position.Y, 0, screenHeight - Bounds.Height);

        // Update the window position
        Position = new PixelPoint(x, y);

        // Run debouncer to save manager point asynchronously
        _saveManagerPointDebouncer.RunAsync(SaveManagerPointAsync);
    }

    /// <summary>
    /// Handles pointer pressed events on the window.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void ManagerWindowOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Return if ViewModel is null
        if (ViewModel == null)
            return;

        // Handle right button click (menu toggle)
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (ViewModel.IsMenuVisible)
                ViewModel.HideMenu();
            else
                ViewModel.ShowMenu(this);
        }
        // Handle left button click (window move)
        else if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);

            // Hide menu if visible
            if (ViewModel.IsMenuVisible)
                ViewModel.HideMenu();
        }
    }

    /// <summary>
    /// Handles pointer pressed events on the CDM text block.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void CDMTextBlockOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            // Check if the startup window exists and view model is not null,
            // If so, show the main window
            if (App.Desktop?.MainWindow?.DataContext is StartupWindowViewModel viewModel)
                viewModel.ShowMainWindow();
        }
        catch (Exception ex)
        {
            // Show error dialog and log the exception
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to open main window.");
        }
    }

    #region Helpers

    /// <summary>
    /// Asynchronously saves the current manager point position.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task SaveManagerPointAsync()
    {
        try
        {
            // Return if ViewModel is null
            if (ViewModel == null)
                return;

            // Save the current position to the ViewModel
            await ViewModel.SaveManagerPointAsync(Position.X, Position.Y);
        }
        catch (Exception ex)
        {
            // Show error dialog and log the exception
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to save manager point.");
        }
    }

    #endregion
}
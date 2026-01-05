using System;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

/// <summary>
/// Represents the startup window of the application.
/// </summary>
public partial class StartupWindow : MyWindowBase<StartupWindowViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartupWindow"/> class with the specified view model.
    /// </summary>
    /// <param name="viewModel">The view model to be used by the window.</param>
    public StartupWindow(StartupWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            base.OnLoaded(e);

            // Hide startup window to show the main application window
            Hide();

            // Check if view model is null
            if (ViewModel == null)
                return;

            // Initialize application
            await ViewModel.InitializeApplicationAsync();

            // Load application data
            await ViewModel.LoadAppAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open startup window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
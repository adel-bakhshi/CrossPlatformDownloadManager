using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

/// <summary>
/// Represents a window for adding download links.
/// </summary>
public partial class AddDownloadLinkWindow : MyWindowBase<AddDownloadLinkWindowViewModel>
{
    #region Private Fields

    /// <summary>
    /// A private field for the Debouncer instance used to debounce URL text changes.
    /// </summary>
    private readonly Debouncer _urlTextChangedDebouncer;

    /// <summary>
    /// A private field for the DispatcherTimer used to manage the topmost window behavior.
    /// </summary>
    private readonly DispatcherTimer _removeTopmostTimer;

    #endregion

    /// <summary>
    /// Initializes a new instance of the AddDownloadLinkWindow class.
    /// </summary>
    public AddDownloadLinkWindow()
    {
        InitializeComponent();

        // Initialize the debouncer with a 1-second delay
        _urlTextChangedDebouncer = new Debouncer(TimeSpan.FromSeconds(1));

        // Initialize the timer for removing topmost behavior after 10 seconds
        _removeTopmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _removeTopmostTimer.Tick += RemoveTopmostTimerOnTick;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _removeTopmostTimer.Start();
    }

    #region Helpers

    /// <summary>
    /// Handles the text changed event for the URL text box.
    /// Debounces the input and triggers URL details retrieval asynchronously.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments for the text changed event.</param>
    private async void UrlTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        try
        {
            // Check if view model is null
            if (ViewModel == null)
                return;

            // Run debouncer to delay the execution of URL details retrieval
            _ = _urlTextChangedDebouncer.RunAsync(ViewModel.GetUrlDetailsAsync);
        }
        catch (Exception ex)
        {
            // Show error dialog and log the exception
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to get url details. Error message: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Handles the tick event for the timer that removes the topmost window behavior.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event arguments for the timer tick event.</param>
    private void RemoveTopmostTimerOnTick(object? sender, EventArgs e)
    {
        // Post a message to the UI thread to set Topmost to false
        Dispatcher.UIThread.Post(() => Topmost = false);
        _removeTopmostTimer.Stop();
    }

    #endregion
}
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

public partial class AddDownloadLinkWindow : MyWindowBase<AddDownloadLinkWindowViewModel>
{
    #region Private Fields

    private readonly Debouncer _urlTextChangedDebouncer;
    private readonly DispatcherTimer _removeTopmostTimer;

    #endregion

    public AddDownloadLinkWindow()
    {
        InitializeComponent();

        _urlTextChangedDebouncer = new Debouncer(TimeSpan.FromSeconds(1));

        _removeTopmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _removeTopmostTimer.Tick += RemoveTopmostTimerOnTick;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _removeTopmostTimer.Start();
    }

    #region Helpers

    private async void UrlTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        try
        {
            // Check if view model is null
            if (ViewModel == null)
                return;

            // Run debouncer
            _ = _urlTextChangedDebouncer.RunAsync(ViewModel.GetUrlDetailsAsync);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to get url details. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private void RemoveTopmostTimerOnTick(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() => Topmost = false);
        _removeTopmostTimer.Stop();
    }

    #endregion
}
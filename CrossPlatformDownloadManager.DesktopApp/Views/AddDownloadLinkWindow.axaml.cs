using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddDownloadLinkWindow : MyWindowBase<AddDownloadLinkWindowViewModel>
{
    #region Private Fields

    private readonly DispatcherTimer _urlTextBoxChangedTimer;
    private readonly DispatcherTimer _removeTopmostTimer;

    #endregion

    public AddDownloadLinkWindow()
    {
        InitializeComponent();

        _urlTextBoxChangedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _urlTextBoxChangedTimer.Tick += UrlTextBoxChangedTimerOnTick;

        _removeTopmostTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _removeTopmostTimer.Tick += RemoveTopmostTimerOnTick;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _removeTopmostTimer.Start();
    }

    #region Helpers

    private void UrlTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Reset timer when user still typing
        _urlTextBoxChangedTimer.Stop();
        _urlTextBoxChangedTimer.Start();
    }

    private async void UrlTextBoxChangedTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            _urlTextBoxChangedTimer.Stop();
            await ViewModel.GetUrlDetailsAsync();
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
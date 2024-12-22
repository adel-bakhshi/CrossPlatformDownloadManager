using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddDownloadLinkWindow : MyWindowBase<AddDownloadLinkWindowViewModel>
{
    #region Private Fields

    private readonly DispatcherTimer _urlTextBoxChangedTimer;

    #endregion

    public AddDownloadLinkWindow()
    {
        InitializeComponent();

        _urlTextBoxChangedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _urlTextBoxChangedTimer.Tick += UrlTextBoxChangedTimerOnTick;
    }

    private void UrlTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
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
            ViewModel?.ShowErrorDialogAsync(ex);
            Log.Error(ex, "Failed to get url details.");
        }
    }
}
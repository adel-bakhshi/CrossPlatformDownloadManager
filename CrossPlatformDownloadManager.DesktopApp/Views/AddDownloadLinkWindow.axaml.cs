using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddDownloadLinkWindow : Window
{
    #region Private Fields

    private readonly DispatcherTimer _urlTextChangedTimer;

    #endregion

    public AddDownloadLinkWindow()
    {
        InitializeComponent();

        _urlTextChangedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _urlTextChangedTimer.Tick += GetUrlInfo;
    }

    private void UrlTextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // Reset timer when user still typing
        _urlTextChangedTimer.Stop();
        _urlTextChangedTimer.Start();
    }

    private async void GetUrlInfo(object? sender, EventArgs e)
    {
        _urlTextChangedTimer.Stop();

        var vm = DataContext as AddDownloadLinkWindowViewModel;
        if (vm == null)
            return;

        await vm.GetUrlInfoAsync();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
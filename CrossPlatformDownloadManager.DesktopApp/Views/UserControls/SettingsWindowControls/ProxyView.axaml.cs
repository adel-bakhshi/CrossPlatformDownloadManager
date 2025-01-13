using System;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class ProxyView : MyUserControlBase<ProxyViewModel>
{
    public ProxyView()
    {
        InitializeComponent();
    }

    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        try
        {
            base.OnPropertyChanged(change);

            if (change.Property != IsVisibleProperty || !IsVisible || ViewModel == null)
                return;

            await ViewModel.LoadAvailableProxiesAsync();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to load available proxies. Error message: {ErrorMessage}", ex.Message);
        }
    }
}
using System;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class CaptureUrlWindow : MyWindowBase<CaptureUrlWindowViewModel>
{
    public CaptureUrlWindow()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            base.OnLoaded(e);

            if (ViewModel == null)
                return;

            await ViewModel.CaptureUrlFromClipboardAsync(this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while capturing url from clipboard. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
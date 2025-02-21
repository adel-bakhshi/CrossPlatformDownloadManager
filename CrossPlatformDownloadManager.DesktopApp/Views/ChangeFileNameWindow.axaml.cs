using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.CustomControls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class ChangeFileNameWindow : MyWindowBase<ChangeFileNameWindowViewModel>
{
    public ChangeFileNameWindow()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            base.OnLoaded(e);

            ViewModel?.SetFileNames();

            var textBox = this.FindControl<CustomTextBox>("NewFileNameTextBox");
            textBox?.Focus();
            textBox?.SelectAll();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open change file name window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            
            Close();
        }
    }
}
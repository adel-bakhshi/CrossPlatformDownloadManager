using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddEditCategoryWindow : MyWindowBase<AddEditCategoryWindowViewModel>
{
    public AddEditCategoryWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private async void BrowseButtonOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var topLevel = GetTopLevel(this);
            if (topLevel == null || ViewModel == null)
                return;

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false,
            };

            var directories = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            if (!directories.Any())
                return;

            var directory = directories[0];
            ViewModel.SaveDirectory = directory.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to select directory.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
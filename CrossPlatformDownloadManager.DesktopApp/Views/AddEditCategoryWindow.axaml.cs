using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

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

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Show message box
        try
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
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
            Console.WriteLine(ex);
        }
    }
}
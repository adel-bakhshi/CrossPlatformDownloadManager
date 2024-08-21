using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddNewCategoryWindow : Window
{
    public AddNewCategoryWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var vm = DataContext as AddNewCategoryWindowViewModel;
            if (vm == null)
                return;
            
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

            var directory = directories.First();
            vm.SaveDirectory = directory.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
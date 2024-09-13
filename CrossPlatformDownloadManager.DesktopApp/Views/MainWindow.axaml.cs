using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void DownloadFilesDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // TODO: Show message box
        double totalSize = 0;
        var vm = DataContext as MainWindowViewModel;
        if (vm == null)
            return;
        
        try
        {
            foreach (var selectedItem in DownloadFilesDataGrid.SelectedItems)
            {
                var downloadFile = selectedItem as DownloadFileViewModel;
                if (downloadFile == null)
                    continue;

                totalSize += downloadFile.Size ?? 0;
            }

            vm.SelectedFilesTotalSize = totalSize == 0 ? "0 KB" : totalSize.ToFileSize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            vm.SelectedFilesTotalSize = "0 KB";
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // TODO: Show message box
        try
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm == null)
                return;

            await vm.LoadDownloadFilesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
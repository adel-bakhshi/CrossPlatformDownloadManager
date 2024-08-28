using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddFilesToQueueWindow : Window
{
    public AddFilesToQueueWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close(new List<DownloadFileViewModel>());
    }

    private void FilesDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedItems = FilesDataGrid.SelectedItems;
        if (selectedItems == null || selectedItems.Count == 0)
            return;

        var vm = DataContext as AddFilesToQueueWindowViewModel;
        if (vm == null)
            return;

        var downloadFiles = new List<DownloadFileViewModel>();
        foreach (var item in selectedItems)
        {
            var downloadFile = item as DownloadFileViewModel;
            if (downloadFile == null)
                continue;

            downloadFiles.Add(downloadFile);
        }

        vm.SelectedDownloadFiles = downloadFiles;
    }
}
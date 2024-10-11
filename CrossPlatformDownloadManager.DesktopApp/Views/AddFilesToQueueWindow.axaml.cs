using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddFilesToQueueWindow : MyWindowBase<AddFilesToQueueWindowViewModel>
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
        if (selectedItems == null || selectedItems.Count == 0 || ViewModel == null)
            return;

        var downloadFiles = selectedItems.OfType<DownloadFileViewModel>().ToList();
        ViewModel.SelectedDownloadFiles = downloadFiles;
    }
}
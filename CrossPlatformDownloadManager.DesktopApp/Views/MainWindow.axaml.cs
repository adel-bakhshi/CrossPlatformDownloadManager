using System;
using System.Linq;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : MyWindowBase<MainWindowViewModel>
{
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;
    }

    private void DownloadFilesDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // TODO: Show message box
        if (ViewModel == null)
            return;

        try
        {
            var downloadFiles = DownloadFilesDataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            ViewModel.SelectAllDownloadFiles = ViewModel.DownloadFiles.Count > 0 &&
                                               downloadFiles.Count == ViewModel.DownloadFiles.Count;

            var totalSize = downloadFiles.Sum(downloadFile => downloadFile.Size ?? 0);
            ViewModel.SelectedFilesTotalSize = totalSize == 0 ? "0 KB" : totalSize.ToFileSize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ViewModel.SelectedFilesTotalSize = "0 KB";
        }
    }
}
using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : MyWindowBase<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void DownloadFilesDataGrid_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // TODO: Show message box
        try
        {
            var totalSize = DownloadFilesDataGrid.SelectedItems.OfType<DownloadFileViewModel>().Sum(downloadFile => downloadFile.Size ?? 0);
            ViewModel.SelectedFilesTotalSize = totalSize == 0 ? "0 KB" : totalSize.ToFileSize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            ViewModel.SelectedFilesTotalSize = "0 KB";
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // TODO: Show message box
        try
        {
            await ViewModel.LoadDownloadFilesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
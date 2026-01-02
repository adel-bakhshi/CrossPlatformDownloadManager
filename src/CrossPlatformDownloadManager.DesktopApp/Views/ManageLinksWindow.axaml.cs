using System;
using System.Linq;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class ManageLinksWindow : MyWindowBase<ManageLinksWindowViewModel>
{
    public ManageLinksWindow()
    {
        InitializeComponent();
    }

    private async void DownloadFilesDataGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            // Make sure ViewModel is not null
            if (ViewModel == null)
                return;

            // Get selected download files
            var downloadFiles = DownloadFilesDataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            // Check if all download files are selected
            ViewModel.SelectAllDownloadFiles = ViewModel.DownloadFiles.Count > 0 && downloadFiles.Count == ViewModel.DownloadFiles.Count;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to update select all state of the data grid. Error message: {ErrorMessage}", ex.Message);
        }
    }
}
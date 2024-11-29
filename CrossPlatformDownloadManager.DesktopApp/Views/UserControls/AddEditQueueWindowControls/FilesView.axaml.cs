using System.Linq;
using Avalonia;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.AddEditQueueWindowControls;

public partial class FilesView : MyUserControlBase<FilesViewModel>
{
    public FilesView()
    {
        InitializeComponent();
    }

    private void FilesDataGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var selectedDownloadFiles = FilesDataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .ToList();
        
        if (ViewModel == null)
            return;

        ViewModel.SelectedDownloadFiles = selectedDownloadFiles;
    }

    private void FilesDataGridOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != DataGrid.ItemsSourceProperty
            || ViewModel?.SelectedDownloadFiles == null
            || ViewModel.SelectedDownloadFiles.Count == 0)
            return;

        foreach (var downloadFile in ViewModel.SelectedDownloadFiles)
            FilesDataGrid.SelectedItems.Add(downloadFile);

        ViewModel.SelectedDownloadFiles = null;
    }
}
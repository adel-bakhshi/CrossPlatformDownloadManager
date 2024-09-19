using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddNewQueueWindow : MyWindowBase<AddNewQueueWindowViewModel>
{
    public AddNewQueueWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }

    private void FilesView_OnDownloadQueueListPriorityChanged(object? sender,
        DownloadQueueListPriorityChangedEventArgs e)
    {
        ViewModel.DownloadFiles = e.NewList.ToObservableCollection();
    }
}
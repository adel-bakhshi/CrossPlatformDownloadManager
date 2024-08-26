using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddNewQueueWindow : Window
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
        var vm = DataContext as AddNewQueueWindowViewModel;
        if (vm == null)
            return;

        vm.DownloadFiles = e.NewList.ToObservableCollection();
    }
}
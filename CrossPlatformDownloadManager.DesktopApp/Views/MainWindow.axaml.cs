using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : MyWindowBase<MainWindowViewModel>
{
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;
    }

    private void DownloadFilesDataGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var serviceProvider = this.GetServiceProvider();
        var appService = serviceProvider.GetService<IAppService>();
        var trayMenuWindow = serviceProvider.GetService<TrayMenuWindow>();
        var vm = new TrayIconWindowViewModel(appService!, trayMenuWindow!);
        var window = new TrayIconWindow { DataContext = vm };
        window.Show();
    }
}
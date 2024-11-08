using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
    #region Private Fields

    private Flyout? _downloadFilesDataGridContextMenuFlyout;

    #endregion

    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;
    }

    public void HideDownloadFilesDataGridContextMenu()
    {
        _downloadFilesDataGridContextMenuFlyout?.Hide();
        _downloadFilesDataGridContextMenuFlyout = null;
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

    private async void DownloadQueuesDataGridContextMenuOnOpening(object? sender, EventArgs e)
    {
        if (sender is not Flyout flyout || ViewModel == null)
            return;

        await ViewModel.ChangeContextFlyoutEnableStateAsync(this);
        _downloadFilesDataGridContextMenuFlyout = flyout;
    }

    public async Task<string?> ChangeSaveLocationAsync(string startDirectory)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null)
            return null;

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Directory",
            AllowMultiple = false,
            SuggestedStartLocation = await topLevel.StorageProvider.TryGetFolderFromPathAsync(startDirectory),
        };

        var directories = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
        return !directories.Any() ? null : directories[0].Path.AbsolutePath;
    }
}
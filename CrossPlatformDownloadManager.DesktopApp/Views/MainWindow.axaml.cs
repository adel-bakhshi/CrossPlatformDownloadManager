using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

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

        // Find AddToQueueFlyout and manage show/hide of it
        if (this.FindResource("AddToQueueFlyout") is Flyout addToQueueFlyout)
            ViewModel!.AddToQueueFlyout = addToQueueFlyout;
    }

    public void HideDownloadFilesDataGridContextMenu()
    {
        _downloadFilesDataGridContextMenuFlyout?.Hide();
        _downloadFilesDataGridContextMenuFlyout = null;
    }

    private async void DownloadFilesDataGridOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

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
            if (ViewModel != null)
                ViewModel.SelectedFilesTotalSize = "0 KB";

            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to update total size of download files.");
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            base.OnLoaded(e);

            var serviceProvider = this.GetServiceProvider();
            var appService = serviceProvider.GetService<IAppService>();
            var trayMenuWindow = serviceProvider.GetService<TrayMenuWindow>();
            var vm = new ManagerWindowViewModel(appService!, trayMenuWindow!);
            var window = new ManagerWindow { DataContext = vm };
            window.Show();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to open manager window.");
        }
    }

    private async void DownloadQueuesDataGridContextMenuOnOpening(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not Flyout flyout || ViewModel == null)
                return;

            await ViewModel.ChangeContextFlyoutEnableStateAsync(this);
            _downloadFilesDataGridContextMenuFlyout = flyout;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured during opening context menu.");
        }
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

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            base.OnClosing(e);
            e.Cancel = true;
            Hide();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured during closing window.");
        }
    }
}
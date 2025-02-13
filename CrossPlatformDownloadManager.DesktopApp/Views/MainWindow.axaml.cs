using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
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
            // Make sure ViewModel is not null
            if (ViewModel == null)
                return;

            // For some reason, when updating download files, selected items will be changed
            // We must remove the added items from selected items when updating download files
            if (ViewModel.IsUpdatingDownloadFiles)
            {
                foreach (var addedItem in e.AddedItems)
                    DownloadFilesDataGrid.SelectedItems.Remove(addedItem);
            }

            // Get selected download files
            var downloadFiles = DownloadFilesDataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            // Check if all download files are selected
            ViewModel.SelectAllDownloadFiles = ViewModel.DownloadFiles.Count > 0 && downloadFiles.Count == ViewModel.DownloadFiles.Count;
            // Calculate total size
            var totalSize = downloadFiles.Sum(downloadFile => downloadFile.Size ?? 0);
            ViewModel.SelectedFilesTotalSize = totalSize == 0 ? "0 KB" : totalSize.ToFileSize();
        }
        catch (Exception ex)
        {
            if (ViewModel != null)
                ViewModel.SelectedFilesTotalSize = "0 KB";

            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to update total size of download files. Error message: {ErrorMessage}", ex.Message);
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            base.OnLoaded(e);

            // Get settings service and show manager if needed
            var serviceProvider = this.GetServiceProvider();
            var settingsService = serviceProvider.GetService<ISettingsService>() ?? throw new InvalidOperationException("Settings service not found");
            if (settingsService.Settings.UseManager)
                settingsService.ShowManager();

            // Start download queues manager timer to manage queues
            var downloadQueueService = serviceProvider.GetService<IDownloadQueueService>();
            downloadQueueService!.StartScheduleManagerTimer();

            // Check if application has been run yet.
            // If application has been run before, hide window
            if (settingsService.Settings.HasApplicationBeenRunYet)
            {
                Hide();
            }
            else
            {
                settingsService.Settings.HasApplicationBeenRunYet = true;
                await settingsService.SaveSettingsAsync(settingsService.Settings, reloadData: true);
            }
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to open manager window. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occured during opening context menu. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occured during closing window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async void FileMenuItemOnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            ViewModel.ChangeFileSubMenusEnableState(DownloadFilesDataGrid);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured during opening context menu. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void DownloadFilesDataGridOnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.DoubleTapped += DownloadFilesDataGridRowOnDoubleTapped;
    }

    private void DownloadFilesDataGridOnUnloadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.DoubleTapped -= DownloadFilesDataGridRowOnDoubleTapped;
    }

    private void DownloadFilesDataGridRowOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel == null || sender is not DataGridRow { DataContext: DownloadFileViewModel downloadFile })
            return;

        ViewModel.DataGridRowDoubleTapAction(downloadFile);
    }
}
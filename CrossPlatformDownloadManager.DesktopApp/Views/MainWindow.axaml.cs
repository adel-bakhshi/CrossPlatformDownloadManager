using System;
using System.Collections.Generic;
using System.IO;
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
    private bool _isCtrlKeyPressed;
    private bool _isAltKeyPressed;

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
            Log.Error(ex, "An error occurred while trying to update total size of download files. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred while trying to open manager window. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred during opening context menu. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred during closing window. Error message: {ErrorMessage}", ex.Message);
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
            Log.Error(ex, "An error occurred during opening context menu. Error message: {ErrorMessage}", ex.Message);
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

    private async void DownloadFilesDataGridRowOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (ViewModel == null || sender is not DataGridRow { DataContext: DownloadFileViewModel downloadFile })
                return;

            await ViewModel.DataGridRowDoubleTapActionAsync(downloadFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during handling double tap. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void DownloadFilesDataGridOnKeyDown(object? sender, KeyEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            // Check ctrl key is pressed or not
            if (!_isCtrlKeyPressed)
                _isCtrlKeyPressed = e.Key is Key.LeftCtrl or Key.RightCtrl;

            // Check alt key is pressed or not
            if (!_isAltKeyPressed)
                _isAltKeyPressed = e.Key is Key.LeftAlt or Key.RightAlt;

            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (e.Key)
            {
                // Delete selected download files from data grid
                case Key.Delete:
                {
                    await ViewModel.RemoveDownloadFilesAsync(DownloadFilesDataGrid, excludeFilesInRunningQueues: false);
                    break;
                }

                // Copy selected download file
                case Key.C:
                {
                    // Make sure ctrl key and alt key are pressed
                    if (!_isCtrlKeyPressed || !_isAltKeyPressed || Clipboard == null || DownloadFilesDataGrid.SelectedItems.Count == 0)
                        break;

                    // Get data object
                    var dataObject = await GetFileDataObjectAsync();
                    if (dataObject == null)
                        break;

                    // Copy file to clipboard
                    await Clipboard.SetDataObjectAsync(dataObject);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during handling key down. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void DownloadFilesDataGridOnKeyUp(object? sender, KeyEventArgs e)
    {
        try
        {
            switch (e.Key)
            {
                // Change ctrl key pressed state
                case Key.LeftCtrl or Key.RightCtrl:
                {
                    _isCtrlKeyPressed = false;
                    break;
                }

                // Change alt key pressed state
                case Key.LeftAlt or Key.RightAlt:
                {
                    _isAltKeyPressed = false;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during handling key up. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async void DownloadFilesDataGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        try
        {
            // Make sure left mouse button is pressed and row is selected
            if (!e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !e.Row.IsSelected)
                return;

            // Get data object
            var dataObject = await GetFileDataObjectAsync();
            if (dataObject == null)
                return;

            // Do drag drop
            await DragDrop.DoDragDrop(e.PointerPressedEventArgs, dataObject, DragDropEffects.Move);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during handling pointer pressed. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #region Helpers

    private async Task<DataObject?> GetFileDataObjectAsync()
    {
        // Get selected download file
        var downloadFiles = DownloadFilesDataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .Where(df => df.IsCompleted && !df.SaveLocation.IsNullOrEmpty() && !df.FileName.IsNullOrEmpty())
            .ToList();

        // Make sure at least one download file is selected
        if (downloadFiles.Count == 0)
            return null;

        // Convert download files to file path
        var filePathList = downloadFiles.ConvertAll(df => Path.Combine(df.SaveLocation!, df.FileName!));
        // Create a list of files
        var files = new List<IStorageFile>();
        foreach (var filePath in filePathList)
        {
            // Get file from storage
            var file = await StorageProvider.TryGetFileFromPathAsync(filePath);
            if (file == null)
                continue;

            files.Add(file);
        }

        // Create data object
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Files, files);
        return dataObject;
    }

    #endregion
}
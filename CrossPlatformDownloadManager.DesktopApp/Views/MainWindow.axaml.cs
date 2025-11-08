using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : MyWindowBase<MainWindowViewModel>
{
    #region Private Fields

    private Flyout? _mainContextMenu;
    private bool _isCtrlKeyPressed;
    private bool _isAltKeyPressed;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Hides the context menu flyout of the download files data grid.
    /// </summary>
    public void HideDownloadFilesDataGridContextMenu()
    {
        _mainContextMenu?.Hide();
        _mainContextMenu = null;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            base.OnLoaded(e);

            // Make sure ViewModel is not null
            if (ViewModel == null)
                return;

            // Find AddToQueueFlyout and manage show/hide of it
            if (this.FindResource("AddToQueueFlyout") is Flyout addToQueueFlyout)
                ViewModel.AddToQueueFlyout = addToQueueFlyout;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to open manager window. Error message: {ErrorMessage}", ex.Message);
        }
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

    /// <summary>
    /// Handles the SelectionChanged event for the DownloadFilesDataGrid.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="SelectionChangedEventArgs"/> class that contains the event data.</param>
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

    /// <summary>
    /// Handles the Opening event for the context menu of the download files data grid.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="EventArgs"/> class that contains the event data.</param>
    private async void DownloadQueuesDataGridContextMenuOnOpening(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not Flyout flyout || ViewModel == null)
                return;

            await ViewModel.ChangeContextFlyoutEnableStateAsync(this);
            _mainContextMenu = flyout;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred during opening context menu. Error message: {ErrorMessage}", ex.Message);
        }
    }

    /// <summary>
    /// Handles the Opened event for the file menu item sub menu.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="RoutedEventArgs"/> class that contains the event data.</param>
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

    /// <summary>
    /// Handles the OnLoadingRow event for the DownloadFilesDataGrid control.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="DataGridRowEventArgs"/> class that contains the event data.</param>
    private void DownloadFilesDataGridOnLoadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.DoubleTapped += DownloadFilesDataGridRowOnDoubleTapped;
    }

    /// <summary>
    /// Handles the OnUnloadingRow event for the DownloadFilesDataGridRow control.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="DataGridRowEventArgs"/> class that contains the event data.</param>
    private void DownloadFilesDataGridOnUnloadingRow(object? sender, DataGridRowEventArgs e)
    {
        e.Row.DoubleTapped -= DownloadFilesDataGridRowOnDoubleTapped;
    }

    /// <summary>
    /// Handles the DoubleTapped event for the DownloadFilesDataGridRow control.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="TappedEventArgs"/> class that contains the event data.</param>
    private async void DownloadFilesDataGridRowOnDoubleTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            if (ViewModel == null || sender is not DataGridRow { DataContext: DownloadFileViewModel downloadFile })
                return;

            await ViewModel.DataGridRowDoubleTapActionAsync(downloadFile, this);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during handling double tap. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the KeyDown event for the DownloadFilesDataGrid control.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">An instance of the <see cref="KeyEventArgs"/> class that contains the event data.</param>
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
                    var dataTransfer = await GetFileDataTransferAsync();
                    if (dataTransfer == null)
                        break;

                    // Copy file to clipboard
                    await Clipboard.SetDataAsync(dataTransfer);
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

    /// <summary>
    /// Handles the KeyUp event for the DownloadFilesDataGrid control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object of <see cref="KeyEventArgs"/> that contains the event data.</param>
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

    /// <summary>
    /// Handles the DragOver event for the DownloadFilesDataGrid control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An object of <see cref="DataGridCellPointerPressedEventArgs"/> that contains the event data.</param>
    private async void DownloadFilesDataGridOnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        try
        {
            // Make sure left mouse button is pressed and row is selected
            if (!e.PointerPressedEventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed || !e.Row.IsSelected)
                return;

            // Get data object
            var dataTransfer = await GetFileDataTransferAsync();
            if (dataTransfer == null)
                return;

            // Do drag drop
            await DragDrop.DoDragDropAsync(e.PointerPressedEventArgs, dataTransfer, DragDropEffects.Move);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred during handling pointer pressed. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the LayoutUpdated event for the DownloadFilesDataGrid control.
    /// This method adjusts the corner radius of the last visible column header.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">Event data.</param>
    private void DownloadFilesDataGridOnLayoutUpdated(object? sender, EventArgs e)
    {
        // Get all column headers in the DataGrid
        var columnHeaders = DownloadFilesDataGrid
            .GetVisualDescendants()
            .OfType<DataGridColumnHeader>()
            .ToList();

        // If no column headers are found, exit the method
        if (columnHeaders.Count == 0)
            return;

        // Find the last visible column header and the scroll corner header
        var lastColumnHeader = columnHeaders.LastOrDefault(c => c.IsVisible);
        var scrollColumHeader = columnHeaders.Find(c => c.Name?.Equals("PART_TopRightCornerHeader") == true);
        // If no last visible column header is found, exit the method
        if (lastColumnHeader == null)
            return;

        // Post the UI update to the UI thread dispatcher
        Dispatcher.UIThread.Post(() =>
        {
            // If scroll corner header is not visible or doesn't exist, set corner radius for the last column header
            if (scrollColumHeader is not { IsVisible: true })
                lastColumnHeader.CornerRadius = new CornerRadius(0, 8, 8, 0);
        });
    }

    #region Helpers

    /// <summary>
    /// Gets the <see cref="DataTransfer"/> object for the selected items in data grid.
    /// </summary>
    /// <returns>Returns the <see cref="DataTransfer"/> object for the selected items in data grid.</returns>
    private async Task<DataTransfer?> GetFileDataTransferAsync()
    {
        // Get selected download file
        var downloadFiles = DownloadFilesDataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .Where(df => df.IsCompleted && !df.SaveLocation.IsStringNullOrEmpty() && !df.FileName.IsStringNullOrEmpty())
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
        var dataTransfer = new DataTransfer();
        foreach (var file in files)
        {
            var dataTransferItem = new DataTransferItem();
            dataTransferItem.SetFile(file);

            dataTransfer.Add(dataTransferItem);
        }

        return dataTransfer;
    }

    #endregion
}
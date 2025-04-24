using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

public class FilesViewModel : ViewModelBase
{
    #region Private Fields

    private readonly int _downloadQueueId;

    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];
    private int _downloadCountAtSameTime;
    private bool _includePausedFiles;

    #endregion

    #region Properties

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    public int DownloadCountAtSameTime
    {
        get => _downloadCountAtSameTime;
        set => this.RaiseAndSetIfChanged(ref _downloadCountAtSameTime, value);
    }

    public bool IncludePausedFiles
    {
        get => _includePausedFiles;
        set => this.RaiseAndSetIfChanged(ref _includePausedFiles, value);
    }

    public List<DownloadFileViewModel>? SelectedDownloadFiles { get; set; }

    #endregion

    #region Commands

    public ICommand? AddItemToDataGridCommand { get; }

    public ICommand? DeleteItemFromDataGridCommand { get; }

    public ICommand? ChangePriorityToLowerLevelCommand { get; }

    public ICommand? ChangePriorityToHigherLevelCommand { get; }

    #endregion

    public FilesViewModel(IAppService appService, DownloadQueueViewModel downloadQueue) : base(appService)
    {
        _downloadQueueId = downloadQueue.Id;
        DownloadCountAtSameTime = downloadQueue.DownloadCountAtSameTime;
        IncludePausedFiles = downloadQueue.IncludePausedFiles;

        LoadDownloadFiles();

        AddItemToDataGridCommand = ReactiveCommand.Create<Window?>(AddItemToDataGrid);
        DeleteItemFromDataGridCommand = ReactiveCommand.Create<DataGrid?>(DeleteItemFromDataGrid);
        ChangePriorityToLowerLevelCommand = ReactiveCommand.Create<DataGrid?>(ChangePriorityToLowerLevel);
        ChangePriorityToHigherLevelCommand = ReactiveCommand.Create<DataGrid?>(ChangePriorityToHigherLevel);
    }

    private void LoadDownloadFiles()
    {
        DownloadFiles = AppService
            .DownloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId != null && df.DownloadQueueId == _downloadQueueId)
            .OrderBy(df => df.DownloadQueuePriority)
            .ToObservableCollection();
    }

    private async void AddItemToDataGrid(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddFilesToQueueWindowViewModel(AppService) { DownloadQueueId = _downloadQueueId };
            var window = new AddFilesToQueueWindow { DataContext = vm };

            var result = await window.ShowDialog<List<DownloadFileViewModel>?>(owner);
            if (result == null || result.Count == 0)
                return;

            var primaryKeys = result
                .Select(df => df.Id)
                .Distinct()
                .ToList();

            var downloadFiles = DownloadFiles
                .Where(df => !primaryKeys.Contains(df.Id))
                .ToList();

            downloadFiles.AddRange(result);
            DownloadFiles = downloadFiles.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while opening the add files to queue window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void DeleteItemFromDataGrid(DataGrid? dataGrid)
    {
        if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
            return;

        SelectedDownloadFiles = [];

        var primaryKeys = dataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .Select(df => df.Id)
            .ToList();

        var downloadFiles = DownloadFiles
            .ToList();

        downloadFiles.RemoveAll(df => primaryKeys.Contains(df.Id));
        DownloadFiles = downloadFiles.ToObservableCollection();
    }

    private void ChangePriorityToLowerLevel(DataGrid? dataGrid)
    {
        ChangeItemsPriority(dataGrid, false);
    }

    private void ChangePriorityToHigherLevel(DataGrid? dataGrid)
    {
        ChangeItemsPriority(dataGrid, true);
    }

    #region Helpers

    private void ChangeItemsPriority(DataGrid? dataGrid, bool isHighPriority)
    {
        if (dataGrid == null || dataGrid.SelectedItems.Count == 0)
            return;

        SelectedDownloadFiles = [];

        var downloadFiles = DownloadFiles
            .ToList();

        var selectedDownloadFiles = dataGrid
            .SelectedItems
            .OfType<DownloadFileViewModel>()
            .ToList();

        for (var i = isHighPriority ? 0 : selectedDownloadFiles.Count - 1;
             isHighPriority ? i < selectedDownloadFiles.Count : i >= 0;
             i = isHighPriority ? i + 1 : i - 1)
        {
            var downloadFile = selectedDownloadFiles[i];
            SelectedDownloadFiles.Add(downloadFile);

            var index = downloadFiles.IndexOf(downloadFile);
            if (index == (isHighPriority ? 0 : downloadFiles.Count - 1))
                continue;

            if (dataGrid.SelectedItems.Contains(downloadFiles[isHighPriority ? index - 1 : index + 1]))
                continue;

            downloadFiles.RemoveAt(index);
            downloadFiles.Insert(isHighPriority ? index - 1 : index + 1, downloadFile);
        }

        DownloadFiles = downloadFiles.ToObservableCollection();
    }

    #endregion
}
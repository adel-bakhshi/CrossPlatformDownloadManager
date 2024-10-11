using System.Collections.ObjectModel;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

namespace CrossPlatformDownloadManager.Data.Services.DownloadQueueService;

public interface IDownloadQueueService
{
    #region Events

    event EventHandler? DataChanged;

    #endregion

    #region Properties

    ObservableCollection<DownloadQueueViewModel> DownloadQueues { get; }

    #endregion

    Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false);

    Task AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true);

    Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue);

    Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel);

    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel);
}
using System.Collections.ObjectModel;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

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

    Task DeleteDownloadQueueAsync(DownloadQueueViewModel? downloadQueue, bool reloadData = true);

    Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue);

    void StartDownloadQueue(DownloadQueueViewModel? downloadQueue);

    Task StopDownloadQueueAsync(DownloadQueueViewModel? downloadQueue);

    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueue,
        DownloadFileViewModel? downloadFile);

    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueue,
        DownloadFileViewModel? downloadFile);
}
using System.Collections.ObjectModel;
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

    Task AddNewDownloadQueueAsync(DownloadQueue downloadQueue, bool reloadData = true);

    Task DeleteDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel);

    Task UpdateDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel);

    Task StartDownloadQueueAsync(DownloadQueueViewModel downloadQueueView);

    Task StopDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel);

    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel,
        DownloadFileViewModel downloadFileViewModel);

    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel downloadQueueViewModel,
        DownloadFileViewModel downloadFileViewModel);
}
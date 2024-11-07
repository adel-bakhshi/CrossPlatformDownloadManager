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

    Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue);
    
    Task UpdateDownloadQueueAsync(DownloadQueueViewModel? viewModel);
    
    Task UpdateDownloadQueuesAsync(List<DownloadQueue>? downloadQueues);
    
    Task UpdateDownloadQueuesAsync(List<DownloadQueueViewModel>? viewModels);

    Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel);
    
    Task AddDownloadFilesToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        List<DownloadFileViewModel>? downloadFilesViewModels);

    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel,
        DownloadFileViewModel? downloadFileViewModel);
    
    Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel);
}
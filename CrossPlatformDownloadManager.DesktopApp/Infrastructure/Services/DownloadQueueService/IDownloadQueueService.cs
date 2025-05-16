using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;

public interface IDownloadQueueService
{
    #region Events

    event EventHandler? DataChanged;

    #endregion

    #region Properties

    ObservableCollection<DownloadQueueViewModel> DownloadQueues { get; }

    #endregion

    Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false);

    Task<int> AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true);

    Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true);
    
    Task UpdateDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);
    
    Task UpdateDownloadQueuesAsync(List<DownloadQueue>? downloadQueues, bool reloadData = true);
    
    Task UpdateDownloadQueuesAsync(List<DownloadQueueViewModel>? viewModels, bool reloadData = true);

    /// <summary>
    /// Starts a download queue and continues downloading files from the queue.
    /// </summary>
    /// <param name="viewModel">The download queue to start.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    /// <summary>
    /// Stops a download queue and stops downloading files from the queue.
    /// </summary>
    /// <param name="viewModel">The download queue to stop.</param>
    /// <param name="playSound">Indicates whether the sound should be played when the download queue is stopped.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool playSound = true);

    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, DownloadFileViewModel? downloadFileViewModel);
    
    Task AddDownloadFilesToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, List<DownloadFileViewModel>? downloadFilesViewModels);

    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, DownloadFileViewModel? downloadFileViewModel);
    
    Task RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, List<DownloadFileViewModel> downloadFileViewModels);
    
    Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);
    
    void StartScheduleManagerTimer();
}
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

    Task StartDownloadQueueAsync(DownloadQueueViewModel? viewModel);

    Task StopDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool playSound = true);

    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, DownloadFileViewModel? downloadFileViewModel);
    
    Task AddDownloadFilesToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, List<DownloadFileViewModel>? downloadFilesViewModels);

    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, DownloadFileViewModel? downloadFileViewModel);
    
    Task RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, List<DownloadFileViewModel> downloadFileViewModels);
    
    Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);
    
    void StartScheduleManagerTimer();
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadQueueService;

/// <summary>
/// Provides methods for managing download queues.
/// </summary>
public interface IDownloadQueueService
{
    #region Events

    /// <summary>
    /// An event that is raised when the data is changed.
    /// </summary>
    event EventHandler? DataChanged;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the value that indicates the collection of download queues.
    /// </summary>
    ObservableCollection<DownloadQueueViewModel> DownloadQueues { get; }

    #endregion

    /// <summary>
    /// Loads download queues from the database.
    /// </summary>
    /// <param name="addDefaultDownloadQueue">Indicates whether the default download queue should be added.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task LoadDownloadQueuesAsync(bool addDefaultDownloadQueue = false);

    /// <summary>
    /// Adds a new download queue to the database.
    /// </summary>
    /// <param name="downloadQueue">The download queue to add.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns the id of the added download queue.</returns>
    Task<int> AddNewDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true);

    /// <summary>
    /// Removes a download queue from the database.
    /// </summary>
    /// <param name="viewModel">The download queue to remove.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task DeleteDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Updates a download queue in the database.
    /// </summary>
    /// <param name="downloadQueue">The download queue to update.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task UpdateDownloadQueueAsync(DownloadQueue? downloadQueue, bool reloadData = true);

    /// <summary>
    /// Updates a download queue in the database.
    /// </summary>
    /// <param name="viewModel">The download queue to update.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task UpdateDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Updates download queues in the database.
    /// </summary>
    /// <param name="downloadQueues">The download queues to update.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task UpdateDownloadQueuesAsync(List<DownloadQueue>? downloadQueues, bool reloadData = true);

    /// <summary>
    /// Updates download queues in the database.
    /// </summary>
    /// <param name="viewModels">The download queues to update.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Adds a download file to the download queue.
    /// </summary>
    /// <param name="downloadQueueViewModel">The download queue to add the file to.</param>
    /// <param name="downloadFileViewModel">The download file to add.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task AddDownloadFileToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, DownloadFileViewModel? downloadFileViewModel);

    /// <summary>
    /// Adds download files to the download queue.
    /// </summary>
    /// <param name="downloadQueueViewModel">The download queue to add the files to.</param>
    /// <param name="downloadFilesViewModels">The download files to add.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task AddDownloadFilesToDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, List<DownloadFileViewModel>? downloadFilesViewModels);

    /// <summary>
    /// Removes download file from download queue.
    /// </summary>
    /// <param name="downloadQueueViewModel">The download queue to remove the file from.</param>
    /// <param name="downloadFileViewModel">The download file to remove.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task RemoveDownloadFileFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, DownloadFileViewModel? downloadFileViewModel);

    /// <summary>
    /// Removes download files from download queue.
    /// </summary>
    /// <param name="downloadQueueViewModel">The download queue to remove the files from.</param>
    /// <param name="downloadFileViewModels">The download files to remove.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueueViewModel? downloadQueueViewModel, List<DownloadFileViewModel> downloadFileViewModels);

    /// <summary>
    /// Changes the default download queue.
    /// </summary>
    /// <param name="viewModel">The download queue to set as default.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task ChangeDefaultDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Changes the last selected download queue.
    /// </summary>
    /// <param name="viewModel">The download queue to set as last selected.</param>
    /// <param name="reloadData">Indicates whether the data should be reloaded.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task ChangeLastSelectedDownloadQueueAsync(DownloadQueueViewModel? viewModel, bool reloadData = true);

    /// <summary>
    /// Starts the timer that manages schedules.
    /// </summary>
    void StartScheduleManagerTimer();
}
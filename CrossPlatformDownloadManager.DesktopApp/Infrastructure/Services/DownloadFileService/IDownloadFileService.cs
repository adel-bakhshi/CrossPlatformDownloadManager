using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;

/// <summary>
/// Base interface of download file service.
/// </summary>
public interface IDownloadFileService
{
    #region Properties

    /// <summary>
    /// Gets a value that indicates the list of download files in the application.
    /// </summary>
    ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event that occurs when <see cref="DownloadFiles"/> collection is changed.
    /// </summary>
    event EventHandler? DataChanged;

    #endregion

    /// <summary>
    /// Loads the download files from the database.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task LoadDownloadFilesAsync();

    /// <summary>
    /// Adds a new download file to the database.
    /// </summary>
    /// <param name="viewModel">The download file to add.</param>
    /// <param name="startDownloading">Indicates whether the download must be started or not.</param>
    /// <returns>Returns the added download file if operation is successful, otherwise returns null.</returns>
    Task<DownloadFileViewModel?> AddDownloadFileAsync(DownloadFileViewModel viewModel, bool startDownloading = false);

    /// <summary>
    /// Adds a new download file to the database.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="startDownloading">Indicates whether the download must be started or not.</param>
    /// <returns>Returns the added download file if operation is successful, otherwise returns null.</returns>
    Task<DownloadFileViewModel?> AddDownloadFileAsync(string? url, bool startDownloading = false);

    /// <summary>
    /// Updates the download file in the database.
    /// </summary>
    /// <param name="viewModel">The download file to update.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel);

    /// <summary>
    /// Updates the list of download files in the database.
    /// </summary>
    /// <param name="viewModels">The list of download files to update.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels);

    /// <summary>
    /// Deletes the download file from the database.
    /// </summary>
    /// <param name="viewModel">The download file that should be deleted.</param>
    /// <param name="alsoDeleteFile">Indicates whether the file must be deleted from the storage or not.</param>
    /// <param name="reloadData">Indicates whether the data must be reloaded or not.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile, bool reloadData = true);

    /// <summary>
    /// Starts the download of a file and creates a windows for showing the download progress.
    /// </summary>
    /// <param name="viewModel">The download file that should be started.</param>
    /// <param name="showWindow">Indicates whether the download window must be shown or not.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task StartDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true);

    /// <summary>
    /// Stops downloading of the download file.
    /// </summary>
    /// <param name="viewModel">The download file that should be stopped.</param>
    /// <param name="ensureStopped">Indicates whether the operation must wait until the download completely stopped or not.</param>
    /// <param name="playSound">Indicates whether the sound must be played or not.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task StopDownloadFileAsync(DownloadFileViewModel? viewModel, bool ensureStopped = false, bool playSound = true);

    /// <summary>
    /// Resumes downloading of the download file.
    /// </summary>
    /// <param name="viewModel">The download file that should be resumed.</param>
    void ResumeDownloadFile(DownloadFileViewModel? viewModel);

    /// <summary>
    /// Pauses downloading of the download file.
    /// </summary>
    /// <param name="viewModel">The download file that should be paused.</param>
    void PauseDownloadFile(DownloadFileViewModel? viewModel);

    /// <summary>
    /// Limits the download speed of the download file.
    /// </summary>
    /// <param name="viewModel">The download file that should be limited.</param>
    /// <param name="speed">The speed to limit the download file in bytes.</param>
    void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed);

    /// <summary>
    /// Re-downloads the download file and clear the old data of it.
    /// </summary>
    /// <param name="viewModel">The download file that should be re-downloaded.</param>
    /// <param name="showWindow">Indicates whether the download window must be shown or not.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true);

    /// <summary>
    /// Gets the total download speed of all download files.
    /// </summary>
    /// <returns>Returns the download speed in string format like "1.2 MB/s".</returns>
    string GetDownloadSpeed();

    /// <summary>
    /// Creates and gets a new download file and fill required properties of it from the URL.
    /// </summary>
    /// <param name="url">The URL to create the download file from.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>Returns the download file.</returns>
    Task<DownloadFileViewModel> GetDownloadFileFromUrlAsync(string? url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the download file and it's properties for adding to the database.
    /// </summary>
    /// <param name="viewModel">The download file to validate.</param>
    /// <param name="showMessage">Indicates whether the message of validation must be shown or not.</param>
    /// <returns>Returns true if the download file is valid, otherwise false.</returns>
    Task<bool> ValidateDownloadFileAsync(DownloadFileViewModel viewModel, bool showMessage = true);

    /// <summary>
    /// Shows the <see cref="Views.DuplicateDownloadLinkWindow"/> and gets the option of the user for the duplicate download link.
    /// </summary>
    /// <param name="url">The URL that is already in the database.</param>
    /// <param name="fileName">The file name related to the URL.</param>
    /// <param name="saveLocation">The save location of the download file.</param>
    /// <returns>Returns the user action for the duplicate download link.</returns>
    Task<DuplicateDownloadLinkAction> GetUserDuplicateActionAsync(string url, string fileName, string saveLocation);

    /// <summary>
    /// Gets a new file name for the duplicate download file.
    /// </summary>
    /// <param name="url">The URL of the duplicate download file.</param>
    /// <param name="fileName">The file name of the duplicate download file.</param>
    /// <param name="saveLocation">The save location of the duplicate download file.</param>
    /// <returns>Returns the new file name.</returns>
    string GetNewFileName(string url, string fileName, string saveLocation);

    /// <summary>
    /// Gets a new file name for the duplicate download file.
    /// </summary>
    /// <param name="fileName">The file name of the duplicate download file.</param>
    /// <param name="saveLocation">The save location of the duplicate download file.</param>
    /// <returns>Returns the new file name.</returns>
    string GetNewFileName(string fileName, string saveLocation);

    /// <summary>
    /// Shows of focuses download window of the specified download file.
    /// </summary>
    /// <param name="viewModel">The download file that is to be shown or focused.</param>
    void ShowOrFocusDownloadWindow(DownloadFileViewModel? viewModel);

    /// <summary>
    /// Adds a task to execute when a file download is complete.
    /// </summary>
    /// <param name="viewModel">A download file to which a task is added and monitored until the download is complete.</param>
    /// <param name="completedTask">The task to execute when the download is complete.</param>
    void AddCompletedTask(DownloadFileViewModel? viewModel, Func<DownloadFileViewModel?, Task> completedTask);

    /// <summary>
    /// Adds a task to execute when a file download is complete.
    /// </summary>
    /// <param name="viewModel">A download file to which a task is added and monitored until the download is complete.</param>
    /// <param name="completedTask">The task to execute when the download is complete.</param>
    void AddCompletedTask(DownloadFileViewModel? viewModel, Action<DownloadFileViewModel?> completedTask);
}
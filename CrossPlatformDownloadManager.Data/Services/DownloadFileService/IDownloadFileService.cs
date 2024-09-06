using System.Collections.ObjectModel;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

public interface IDownloadFileService
{
    #region Events

    event EventHandler<DownloadFileServiceEventArgs>? DataChanged;

    #endregion

    #region Properties

    ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    #endregion

    Task LoadFilesAsync();

    Task AddFileAsync(DownloadFile downloadFile);

    Task UpdateFileAsync(DownloadFile downloadFile);

    Task UpdateFileAsync(DownloadFileViewModel viewModel);

    Task UpdateFilesAsync(List<DownloadFile> downloadFiles);

    Task StartDownloadFileAsync(DownloadFileViewModel? downloadFile, Window? window);

    Task StopDownloadFileAsync(DownloadFileViewModel? downloadFile, bool closeWindow = false);

    void ResumeDownloadFile(DownloadFileViewModel? downloadFile);

    void PauseDownloadFile(DownloadFileViewModel? downloadFile);

    void LimitDownloadFileSpeed(DownloadFileViewModel? downloadFile, long speed);

    Task DeleteDownloadFileAsync(DownloadFileViewModel? downloadFile, bool alsoDeleteFile, bool reloadData = true);
}
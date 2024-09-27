using System.Collections.ObjectModel;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

public interface IDownloadFileService
{
    #region Events

    event EventHandler? DataChanged;

    #endregion

    #region Properties

    ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    #endregion

    Task LoadDownloadFilesAsync();

    Task AddDownloadFileAsync(DownloadFile downloadFile);

    Task UpdateDownloadFileAsync(DownloadFile downloadFile);

    Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel);

    Task UpdateDownloadFilesAsync(List<DownloadFile> downloadFiles);
    
    Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels);

    Task StartDownloadFileAsync(DownloadFileViewModel? downloadFile, Window? window);

    Task StopDownloadFileAsync(DownloadFileViewModel? downloadFile, bool closeWindow = false);

    void ResumeDownloadFile(DownloadFileViewModel? downloadFile);

    void PauseDownloadFile(DownloadFileViewModel? downloadFile);

    void LimitDownloadFileSpeed(DownloadFileViewModel? downloadFile, long speed);

    Task DeleteDownloadFileAsync(DownloadFileViewModel? downloadFile, bool alsoDeleteFile, bool reloadData = true);
}
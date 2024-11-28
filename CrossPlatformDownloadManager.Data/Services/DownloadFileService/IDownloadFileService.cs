using System.Collections.ObjectModel;
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
    
    Dictionary<int, List<Func<DownloadFileViewModel?, Task<DownloadFinishedTaskValue?>>>> DownloadFinishedTasks { get; }

    #endregion

    Task LoadDownloadFilesAsync();

    Task AddDownloadFileAsync(DownloadFile downloadFile);

    Task UpdateDownloadFileAsync(DownloadFile downloadFile);

    Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel);

    Task UpdateDownloadFilesAsync(List<DownloadFile> downloadFiles);

    Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels);

    Task StartDownloadFileAsync(DownloadFileViewModel? viewModel);

    Task StopDownloadFileAsync(DownloadFileViewModel? viewModel);

    void ResumeDownloadFile(DownloadFileViewModel? viewModel);

    void PauseDownloadFile(DownloadFileViewModel? viewModel);

    void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed);

    Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile, bool reloadData = true);

    Task DeleteDownloadFilesAsync(List<DownloadFileViewModel>? viewModels, bool alsoDeleteFile, bool reloadData = true);

    Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel);

    string GetDownloadSpeed();
}
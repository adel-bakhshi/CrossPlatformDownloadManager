using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Data.ViewModels.Services;
using CrossPlatformDownloadManager.Data.ViewModels.Services.DownloadFileService;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

public interface IDownloadFileService
{
    #region Properties

    ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    Dictionary<int, List<Func<DownloadFileViewModel?, Task<DownloadFinishedTaskValue?>>>> DownloadFinishedAsyncTasks { get; }

    Dictionary<int, List<Func<DownloadFileViewModel?, DownloadFinishedTaskValue?>>> DownloadFinishedSyncTasks { get; }

    #endregion

    #region Events

    event EventHandler? DataChanged;
    event EventHandler<DownloadFileErrorEventArgs>? ErrorOccurred;

    #endregion

    Task LoadDownloadFilesAsync();

    Task<ServiceResultViewModel<DownloadFileViewModel>> AddDownloadFileAsync(DownloadFileViewModel viewModel);

    Task<ServiceResultViewModel<UrlDetailsViewModel>> GetUrlDetailsAsync(string? url);

    Task<ServiceResultViewModel> ValidateDownloadFileAsync(DownloadFileViewModel viewModel);

    Task UpdateDownloadFileAsync(DownloadFile downloadFile);

    Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel);

    Task UpdateDownloadFilesAsync(List<DownloadFile> downloadFiles);

    Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels);

    Task StartDownloadFileAsync(DownloadFileViewModel? viewModel);

    Task StopDownloadFileAsync(DownloadFileViewModel? viewModel, bool ensureStopped = false);

    void ResumeDownloadFile(DownloadFileViewModel? viewModel);

    void PauseDownloadFile(DownloadFileViewModel? viewModel);

    void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed);

    Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile, bool reloadData = true);

    Task DeleteDownloadFilesAsync(List<DownloadFileViewModel>? viewModels, bool alsoDeleteFile, bool reloadData = true);

    Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel);

    string GetDownloadSpeed();
}
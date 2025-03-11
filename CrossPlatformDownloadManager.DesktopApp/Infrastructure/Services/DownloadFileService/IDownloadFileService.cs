using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.ViewModels;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;

public interface IDownloadFileService
{
    #region Properties

    ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    Dictionary<int, List<Func<DownloadFileViewModel?, Task<DownloadFinishedTaskValue?>>>> DownloadFinishedAsyncTasks { get; }

    Dictionary<int, List<Func<DownloadFileViewModel?, DownloadFinishedTaskValue?>>> DownloadFinishedSyncTasks { get; }

    #endregion

    #region Events

    event EventHandler? DataChanged;

    #endregion

    Task LoadDownloadFilesAsync();

    Task<DownloadFileViewModel?> AddDownloadFileAsync(DownloadFileViewModel viewModel,
        bool isUrlDuplicate = false,
        DuplicateDownloadLinkAction? duplicateAction = null,
        bool isFileNameDuplicate = false,
        bool startDownloading = false);

    Task UpdateDownloadFileAsync(DownloadFile downloadFile);

    Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel);

    Task UpdateDownloadFilesAsync(List<DownloadFile> downloadFiles);

    Task UpdateDownloadFilesAsync(List<DownloadFileViewModel> viewModels);

    Task StartDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true);

    Task StopDownloadFileAsync(DownloadFileViewModel? viewModel, bool ensureStopped = false, bool playSound = true);

    void ResumeDownloadFile(DownloadFileViewModel? viewModel);

    void PauseDownloadFile(DownloadFileViewModel? viewModel);

    void LimitDownloadFileSpeed(DownloadFileViewModel? viewModel, long speed);

    Task DeleteDownloadFileAsync(DownloadFileViewModel? viewModel, bool alsoDeleteFile, bool reloadData = true);

    Task DeleteDownloadFilesAsync(List<DownloadFileViewModel>? viewModels, bool alsoDeleteFile, bool reloadData = true);

    Task RedownloadDownloadFileAsync(DownloadFileViewModel? viewModel, bool showWindow = true);

    string GetDownloadSpeed();

    Task<UrlDetailsResultViewModel> GetUrlDetailsAsync(string? url, CancellationToken cancellationToken);

    ValidateUrlDetailsViewModel ValidateUrlDetails(UrlDetailsResultViewModel viewModel);

    Task<bool> ValidateDownloadFileAsync(DownloadFileViewModel viewModel);

    Task<DuplicateDownloadLinkAction> GetUserDuplicateActionAsync(string url, string fileName, string saveLocation);

    string GetNewFileName(string url, string fileName, string saveLocation);
    
    string GetNewFileName(string fileName, string saveLocation);

    void ShowOrFocusDownloadWindow(DownloadFileViewModel? viewModel);
}
using System.Collections.ObjectModel;
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

    Task UpdateFilesAsync(List<DownloadFile> downloadFiles);
}
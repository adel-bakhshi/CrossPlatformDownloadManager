using System.Collections.ObjectModel;
using System.ComponentModel;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

public interface IDownloadFileService
{
    #region Events

    event EventHandler<List<DownloadFileViewModel>>? DataChanged;
    
    #endregion

    #region Properties

    ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    #endregion

    Task LoadFilesAsync();

    Task AddFileAsync(DownloadFile downloadFile);

    Task UpdateFileAsync(DownloadFile downloadFile);

    Task UpdateFilesAsync(List<DownloadFile> downloadFiles);
}
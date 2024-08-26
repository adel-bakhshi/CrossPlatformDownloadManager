using System.ComponentModel;

namespace CrossPlatformDownloadManager.Data.DownloadFileService;

public interface IDownloadFileService : INotifyPropertyChanged
{
    Task LoadFilesAsync();
}
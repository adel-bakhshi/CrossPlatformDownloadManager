namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class DownloadFileServiceEventArgs : EventArgs
{
    public List<DownloadFileViewModel> DownloadFiles { get; set; } = [];
}
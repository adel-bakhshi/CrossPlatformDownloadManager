namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class DownloadQueueListPriorityChangedEventArgs : EventArgs
{
    public ICollection<DownloadFileViewModel> NewList { get; set; } = new List<DownloadFileViewModel>();
}
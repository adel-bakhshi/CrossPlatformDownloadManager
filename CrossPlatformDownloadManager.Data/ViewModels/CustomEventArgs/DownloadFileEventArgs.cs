namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class DownloadFileEventArgs : EventArgs
{
    public int Id { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
}
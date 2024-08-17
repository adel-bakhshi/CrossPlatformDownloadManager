namespace CrossPlatformDownloadManager.Data.Models;

public enum DownloadStatus : byte
{
    Completed = 0,
    Downloading,
    Pause,
    Error,
}
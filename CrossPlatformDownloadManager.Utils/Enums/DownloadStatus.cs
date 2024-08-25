namespace CrossPlatformDownloadManager.Utils.Enums;

public enum DownloadStatus : byte
{
    None = 0,
    Completed,
    Downloading,
    Pause,
    Error,
}
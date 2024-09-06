namespace CrossPlatformDownloadManager.Utils.Enums;

public enum DownloadFileStatus : byte
{
    None = 0,
    Completed,
    Downloading,
    Paused,
    Error,
}
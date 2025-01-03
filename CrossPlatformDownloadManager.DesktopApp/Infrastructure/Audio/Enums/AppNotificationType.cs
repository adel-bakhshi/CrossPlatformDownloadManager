namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio.Enums;

public enum AppNotificationType : byte
{
    DownloadCompleted,
    DownloadStopped,
    DownloadFailed,
    QueueStarted,
    QueueStopped,
    QueueFinished
}
namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class DownloadSpeedLimiterViewEventArgs : EventArgs
{
    public bool Enabled { get; set; }
    public double? Speed { get; set; }
    public string? Unit { get; set; }
}
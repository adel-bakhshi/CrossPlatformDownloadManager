namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class SpeedLimiterChangedEventArgs : EventArgs
{
    public bool Enabled { get; set; }
    public double? Speed { get; set; }
    public string? Unit { get; set; }
}
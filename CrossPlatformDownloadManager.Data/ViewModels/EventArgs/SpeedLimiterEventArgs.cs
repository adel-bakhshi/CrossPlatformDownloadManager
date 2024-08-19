namespace CrossPlatformDownloadManager.Data.ViewModels.EventArgs;

public class SpeedLimiterEventArgs
{
    public bool Enabled { get; set; }
    public double? Speed { get; set; }
    public string? Unit { get; set; }
}
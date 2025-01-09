namespace CrossPlatformDownloadManager.Utils.CustomEventArgs;

public class SpeedLimiterChangedEventArgs : EventArgs
{
    #region Properties

    public bool Enabled { get; set; }
    public double? Speed { get; set; }
    public string? Unit { get; set; }

    #endregion
}
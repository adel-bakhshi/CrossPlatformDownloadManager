namespace MultipartDownloader.Core.CustomEventArgs;

public class MergeProgressChangedEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// Gets the progress of the merge operation
    /// </summary>
    public double Progress { get; }

    #endregion Properties

    public MergeProgressChangedEventArgs(double progress)
    {
        Progress = progress;
    }
}
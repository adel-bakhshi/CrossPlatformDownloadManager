namespace CrossPlatformDownloadManager.Utils.CustomEventArgs;

public class DownloadOptionsChangedEventArgs : EventArgs
{
    #region Properties

    public bool OpenFolderAfterDownloadFinished { get; set; }
    public bool ExitProgramAfterDownloadFinished { get; set; }
    public bool TurnOffComputerAfterDownloadFinished { get; set; }
    public string? TurnOffComputerMode { get; set; }

    #endregion
}
namespace CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

public class DownloadOptionsChangedEventArgs : EventArgs
{
    public bool OpenFolderAfterDownloadFinished { get; set; }
    public bool ExitProgramAfterDownloadFinished { get; set; }
    public bool TurnOffComputerAfterDownloadFinished { get; set; }
    public string? TurnOffComputerMode { get; set; }
}
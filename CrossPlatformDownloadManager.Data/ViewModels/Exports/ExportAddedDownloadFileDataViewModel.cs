namespace CrossPlatformDownloadManager.Data.ViewModels.Exports;

public class ExportAddedDownloadFileDataViewModel
{
    #region Properties

    public string OldFileName { get; set; }
    public string NewFileName { get; set; }
    public string SaveLocation { get; set; }
    public int NewDownloadFileId { get; set; }

    #endregion

    public ExportAddedDownloadFileDataViewModel(string oldFileName, string newFileName, string saveLocation, int newDownloadFileId)
    {
        OldFileName = oldFileName;
        NewFileName = newFileName;
        SaveLocation = saveLocation;
        NewDownloadFileId = newDownloadFileId;
    }
}
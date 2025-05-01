namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Utils;

public class DownloadRequestOptions
{
    #region Properties

    public bool AllowAutoRedirect { get; set; } = true;
    public int MaxAutomaticRedirections { get; set; } = int.MaxValue;

    #endregion
}
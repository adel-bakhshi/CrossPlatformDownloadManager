using System;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

public class DownloadFinishedTaskValue
{
    #region Properties

    public bool UpdateDownloadFile { get; set; }
    public Exception? Exception { get; set; }

    #endregion
}
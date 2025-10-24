namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

/// <summary>
/// Represents the options for downloading a file.
/// </summary>
public class DownloadFileOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets the referer URL for the download request.
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// Gets or sets the page address from which the file is being downloaded.
    /// </summary>
    public string? PageAddress { get; set; }

    /// <summary>
    /// Gets or sets the description of the file to be downloaded.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the download should be started immediately.
    /// </summary>
    public bool StartDownloading { get; set; }

    #endregion
}
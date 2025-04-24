namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;

public class ValidateUrlDetailsViewModel
{
    #region Properties

    public bool IsValid { get; set; }
    public string? Title { get; set; }
    public string? Message { get; set; }

    #endregion
}
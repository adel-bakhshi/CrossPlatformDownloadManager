using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.ViewModels;

public class UrlDetailsResultViewModel
{
    #region Properties

    public string Url { get; set; } = string.Empty;
    public bool IsUrlValid { get; set; }
    public bool IsFile { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool ContentLengthNotProvided { get; set; }
    public double FileSize { get; set; }
    public CategoryViewModel? Category { get; set; }
    public bool IsUrlDuplicate { get; set; }
    public bool IsFileNameDuplicate { get; set; }

    #endregion
}
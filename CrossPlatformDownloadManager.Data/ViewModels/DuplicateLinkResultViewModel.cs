using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DuplicateLinkResultViewModel
{
    public bool IsSuccess { get; set; }
    public DuplicateDownloadLinkAction DuplicateAction { get; set; }
    public string NewFileName { get; set; } = string.Empty;
}
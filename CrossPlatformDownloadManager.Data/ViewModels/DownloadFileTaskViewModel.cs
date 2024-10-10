using Downloader;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadFileTaskViewModel
{
    public int Key { get; set; }
    public DownloadConfiguration? Configuration { get; set; }
    public DownloadService? Service { get; set; }
}
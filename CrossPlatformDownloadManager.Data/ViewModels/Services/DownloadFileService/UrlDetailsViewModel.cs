namespace CrossPlatformDownloadManager.Data.ViewModels.Services.DownloadFileService;

public class UrlDetailsViewModel
{
    public string Url { get; set; } = string.Empty;
    public bool IsUrlValid { get; set; }
    public bool IsUrlDuplicate { get; set; }
    public bool IsFile { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double FileSize { get; set; }
    public CategoryViewModel? Category { get; set; }
}
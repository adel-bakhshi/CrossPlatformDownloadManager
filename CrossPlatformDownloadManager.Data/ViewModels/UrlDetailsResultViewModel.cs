namespace CrossPlatformDownloadManager.Data.ViewModels;

public class UrlDetailsResultViewModel
{
    public string Url { get; set; } = string.Empty;
    public bool IsUrlValid { get; set; }
    public bool IsFile { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double FileSize { get; set; }
    public CategoryViewModel? Category { get; set; }
    public bool IsDuplicate { get; set; }
}
using System.IO.Enumeration;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadFileViewModel
{
    public int Id { get; set; }
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public string? QueueName { get; set; }
    public string? Size { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDownloading { get; set; }
    public bool IsPaused { get; set; }
    public bool IsError { get; set; }
    public double? DownloadProgress { get; set; }
    public string? TimeLeft { get; set; }
    public string? TransferRate { get; set; }
    public string? LastTryDate { get; set; }
    public string? DateAdded { get; set; }
}
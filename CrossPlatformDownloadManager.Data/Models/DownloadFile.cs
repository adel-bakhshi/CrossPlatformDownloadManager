using CrossPlatformDownloadManager.Utils.Enums;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("DownloadFiles")]
public class DownloadFile
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] [Indexed] public string Url { get; set; } = "";

    [NotNull] public string FileName { get; set; } = "";

    [Indexed] public int? DownloadQueueId { get; set; }

    [NotNull] public double Size { get; set; }
    
    public string? Description { get; set; }

    public DownloadStatus? Status { get; set; }

    public DateTime? LastTryDate { get; set; }

    [NotNull] public DateTime DateAdded { get; set; }

    public int? QueuePriority { get; set; }

    [Indexed] [NotNull] public int CategoryId { get; set; }

    [Ignore] public TimeSpan? TimeLeft { get; set; }

    [Ignore] public double? TransferRate { get; set; }
    
    [Ignore] public DownloadQueue? DownloadQueue { get; set; }
    
    [Ignore] public Category? Category { get; set; }
}
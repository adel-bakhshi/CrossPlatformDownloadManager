using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

public class DownloadFile
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] public string FileName { get; set; } = "";

    [NotNull] public DownloadFileType FileType { get; set; }

    [Indexed] public int? QueueId { get; set; }

    [NotNull] public double Size { get; set; }

    public DownloadStatus? Status { get; set; }

    public DateTime? LastTryDate { get; set; }

    [NotNull] public DateTime DateAdded { get; set; }

    [Ignore] public TimeSpan? TimeLeft { get; set; }

    [Ignore] public double? TransferRate { get; set; }
}
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

public class Queue
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] public string Title { get; set; } = "";

    [Ignore] public ICollection<DownloadFile> DownloadFiles { get; set; } = new List<DownloadFile>();
}
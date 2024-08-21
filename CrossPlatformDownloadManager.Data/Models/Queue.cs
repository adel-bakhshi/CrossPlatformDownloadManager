using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

public class Queue
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] public string Title { get; set; } = "";

    [NotNull] public bool StartOnApplicationStartup { get; set; }

    public TimeSpan? StartDownloadSchedule { get; set; }

    public TimeSpan? StopDownloadSchedule { get; set; }

    [NotNull] public bool IsDaily { get; set; }

    public DateTime? JustForDate { get; set; }

    public string? DaysOfWeek { get; set; }

    [NotNull] public bool RetryOnDownloadingFailed { get; set; }

    [NotNull] public int RetryCount { get; set; } = 3;

    public bool? ExitProgramWhenQueueFinish { get; set; }

    public bool? TurnOffComputerWhenQueueFinish { get; set; }

    public TurnOffComputerMode? TurnOffComputerMode { get; set; }

    [Ignore] public ICollection<DownloadFile> DownloadFiles { get; set; } = new List<DownloadFile>();
}
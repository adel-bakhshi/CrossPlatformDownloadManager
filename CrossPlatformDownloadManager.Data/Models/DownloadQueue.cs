using CrossPlatformDownloadManager.Utils.Enums;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("DownloadQueues")]
public class DownloadQueue
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

    public bool? ShowAlarmWhenDone { get; set; }

    public bool? ExitProgramWhenDone { get; set; }

    public bool? TurnOffComputerWhenDone { get; set; }

    public TurnOffComputerMode? TurnOffComputerMode { get; set; }

    [NotNull] public bool IsDefault { get; set; }

    [Ignore] public ICollection<DownloadFile> DownloadFiles { get; set; } = new List<DownloadFile>();
}
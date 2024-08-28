using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Data.Models;

public class DownloadQueue
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] [MaxLength(100)] public string Title { get; set; } = "";

    [Required] public bool StartOnApplicationStartup { get; set; }

    public TimeSpan? StartDownloadSchedule { get; set; }

    public TimeSpan? StopDownloadSchedule { get; set; }

    [Required] public bool IsDaily { get; set; }

    public DateTime? JustForDate { get; set; }

    [MaxLength(500)]
    public string? DaysOfWeek { get; set; }

    [Required] public bool RetryOnDownloadingFailed { get; set; }

    [Required] public int RetryCount { get; set; } = 3;

    public bool? ShowAlarmWhenDone { get; set; }

    public bool? ExitProgramWhenDone { get; set; }

    public bool? TurnOffComputerWhenDone { get; set; }

    public TurnOffComputerMode? TurnOffComputerMode { get; set; }

    [Required] public bool IsDefault { get; set; }

    public ICollection<DownloadFile> DownloadFiles { get; set; } = new List<DownloadFile>();
}
using System.ComponentModel.DataAnnotations;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Data.Models;

public class DownloadQueue : DbModelBase
{
    [Required] [MaxLength(100)] public string Title { get; set; } = string.Empty;

    [Required] public bool StartOnApplicationStartup { get; set; }

    public TimeSpan? StartDownloadSchedule { get; set; }

    public TimeSpan? StopDownloadSchedule { get; set; }

    [Required] public bool IsDaily { get; set; }

    public DateTime? JustForDate { get; set; }

    [MaxLength(500)] public string? DaysOfWeek { get; set; }

    [Required] public bool RetryOnDownloadingFailed { get; set; }

    [Required] public int RetryCount { get; set; }

    public bool ShowAlarmWhenDone { get; set; }

    public bool ExitProgramWhenDone { get; set; }

    public bool TurnOffComputerWhenDone { get; set; }

    public TurnOffComputerMode? TurnOffComputerMode { get; set; }

    [Required] public bool IsDefault { get; set; }

    [Required] public int DownloadCountAtSameTime { get; set; }

    public ICollection<DownloadFile> DownloadFiles { get; } = [];

    public DownloadQueue()
    {
    }

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not DownloadQueue downloadQueue)
            return;
        
        Title = downloadQueue.Title;
        StartOnApplicationStartup = downloadQueue.StartOnApplicationStartup;
        StartDownloadSchedule = downloadQueue.StartDownloadSchedule;
        StopDownloadSchedule = downloadQueue.StopDownloadSchedule;
        IsDaily = downloadQueue.IsDaily;
        JustForDate = downloadQueue.JustForDate;
        DaysOfWeek = downloadQueue.DaysOfWeek;
        RetryOnDownloadingFailed = downloadQueue.RetryOnDownloadingFailed;
        RetryCount = downloadQueue.RetryCount;
        ShowAlarmWhenDone = downloadQueue.ShowAlarmWhenDone;
        ExitProgramWhenDone = downloadQueue.ExitProgramWhenDone;
        TurnOffComputerWhenDone = downloadQueue.TurnOffComputerWhenDone;
        TurnOffComputerMode = downloadQueue.TurnOffComputerMode;
        IsDefault = downloadQueue.IsDefault;
        DownloadCountAtSameTime = downloadQueue.DownloadCountAtSameTime;
        
        DownloadFiles.Clear();
    }
}
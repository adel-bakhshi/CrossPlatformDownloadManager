using CrossPlatformDownloadManager.Utils.Enums;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.Exports;

public class ExportDownloadQueueViewModel
{
    #region Properties

    [JsonProperty("id")] public int Id { get; set; }
    [JsonProperty("title")] public string Title { get; set; } = string.Empty;

    [JsonProperty("startOnApplicationStartup")]
    public bool StartOnApplicationStartup { get; set; }

    [JsonProperty("startDownloadSchedule")]
    public TimeSpan? StartDownloadSchedule { get; set; }

    [JsonProperty("stopDownloadSchedule")] public TimeSpan? StopDownloadSchedule { get; set; }
    [JsonProperty("isDaily")] public bool IsDaily { get; set; }
    [JsonProperty("justForDate")] public DateTime? JustForDate { get; set; }
    [JsonProperty("daysOfWeek")] public string? DaysOfWeek { get; set; }

    [JsonProperty("retryOnDownloadingFailed")]
    public bool RetryOnDownloadingFailed { get; set; }

    [JsonProperty("retryCount")] public int RetryCount { get; set; }
    [JsonProperty("showAlarmWhenDone")] public bool ShowAlarmWhenDone { get; set; }
    [JsonProperty("exitProgramWhenDone")] public bool ExitProgramWhenDone { get; set; }

    [JsonProperty("turnOffComputerWhenDone")]
    public bool TurnOffComputerWhenDone { get; set; }

    [JsonProperty("turnOffComputerMode")] public TurnOffComputerMode? TurnOffComputerMode { get; set; }

    [JsonProperty("downloadCountAtSameTime")]
    public int DownloadCountAtSameTime { get; set; }

    [JsonProperty("includePausedFiles")] public bool IncludePausedFiles { get; set; }

    #endregion

    public static ExportDownloadQueueViewModel CreateExportFile(DownloadQueueViewModel downloadQueue)
    {
        return new ExportDownloadQueueViewModel
        {
            Id = downloadQueue.Id,
            Title = downloadQueue.Title ?? string.Empty,
            StartOnApplicationStartup = downloadQueue.StartOnApplicationStartup,
            StartDownloadSchedule = downloadQueue.StartDownloadSchedule,
            StopDownloadSchedule = downloadQueue.StopDownloadSchedule,
            IsDaily = downloadQueue.IsDaily,
            JustForDate = downloadQueue.JustForDate,
            DaysOfWeek = downloadQueue.DaysOfWeek,
            RetryOnDownloadingFailed = downloadQueue.RetryOnDownloadingFailed,
            RetryCount = downloadQueue.RetryCount,
            ShowAlarmWhenDone = downloadQueue.ShowAlarmWhenDone,
            ExitProgramWhenDone = downloadQueue.ExitProgramWhenDone,
            TurnOffComputerWhenDone = downloadQueue.TurnOffComputerWhenDone,
            TurnOffComputerMode = downloadQueue.TurnOffComputerMode,
            DownloadCountAtSameTime = downloadQueue.DownloadCountAtSameTime,
            IncludePausedFiles = downloadQueue.IncludePausedFiles
        };
    }
}
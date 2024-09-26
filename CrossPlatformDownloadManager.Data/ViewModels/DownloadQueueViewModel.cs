using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

[AddINotifyPropertyChangedInterface]
public class DownloadQueueViewModel
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public bool StartOnApplicationStartup { get; set; }
    public TimeSpan? StartDownloadSchedule { get; set; }
    public TimeSpan? StopDownloadSchedule { get; set; }
    public bool IsDaily { get; set; }
    public DateTime? JustForDate { get; set; }
    public string? DaysOfWeek { get; set; }
    public bool RetryOnDownloadingFailed { get; set; }
    public int RetryCount { get; set; }
    public bool ShowAlarmWhenDone { get; set; }
    public bool ExitProgramWhenDone { get; set; }
    public bool TurnOffComputerWhenDone { get; set; }
    public TurnOffComputerMode? TurnOffComputerMode { get; set; }
    public bool IsDefault { get; set; }
    public int DownloadCountAtSameTime { get; set; }
    public bool StartDownloadScheduleEnabled { get; set; }
    public double? StartDownloadHour { get; set; }
    public double? StartDownloadMinute { get; set; }
    public bool StopDownloadScheduleEnabled { get; set; }
    public double? StopDownloadHour { get; set; }
    public double? StopDownloadMinute { get; set; }
    public ObservableCollection<string> TimesOfDay { get; set; } = [];
    public string? SelectedStartTimeOfDay { get; set; }
    public string? SelectedStopTimeOfDay { get; set; }
    public ObservableCollection<string> TurnOffComputerModes { get; set; } = [];
    public string? SelectedTurnOffComputerMode { get; set; }
    public DaysOfWeekViewModel DaysOfWeekViewModel { get; set; } = new();
    public bool IsRunning { get; set; }

    public DownloadQueueViewModel()
    {
        TimesOfDay = Constants.TimesOfDay.ToObservableCollection();
        SelectedStartTimeOfDay = SelectedStopTimeOfDay = TimesOfDay.FirstOrDefault();

        TurnOffComputerModes = Constants.TurnOffComputerModes.ToObservableCollection();
        SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault();
    }

    #region Helpers

    public void LoadViewData()
    {
        StartDownloadScheduleEnabled = StartDownloadSchedule != null;
        StopDownloadScheduleEnabled = StopDownloadSchedule != null;

        if (StartDownloadScheduleEnabled)
        {
            var timeSpan = StartDownloadSchedule!;
            var startDownloadHour = timeSpan.Value.TotalMinutes / 60;
            timeSpan = timeSpan.Value.Add(TimeSpan.FromMinutes(-startDownloadHour * 60));
            var startDownloadMinute = timeSpan.Value.TotalMinutes;

            var isAfternoon = startDownloadHour > 12;

            StartDownloadHour = startDownloadHour - (isAfternoon ? 12 : 0);
            StartDownloadMinute = startDownloadMinute;
            SelectedStartTimeOfDay =
                TimesOfDay.FirstOrDefault(tod =>
                    tod.Equals(isAfternoon ? "PM" : "AM", StringComparison.OrdinalIgnoreCase)) ??
                TimesOfDay.FirstOrDefault();
        }

        if (StopDownloadScheduleEnabled)
        {
            var timeSpan = StopDownloadSchedule!;
            var stopDownloadHour = timeSpan.Value.TotalMinutes / 60;
            timeSpan = timeSpan.Value.Add(TimeSpan.FromMinutes(-stopDownloadHour * 60));
            var stopDownloadMinute = timeSpan.Value.TotalMinutes;

            var isAfternoon = stopDownloadHour > 12;

            StopDownloadHour = stopDownloadHour - (isAfternoon ? 12 : 0);
            StopDownloadMinute = stopDownloadMinute;
            SelectedStopTimeOfDay =
                TimesOfDay.FirstOrDefault(tod =>
                    tod.Equals(isAfternoon ? "PM" : "AM", StringComparison.OrdinalIgnoreCase)) ??
                TimesOfDay.FirstOrDefault();
        }

        if (TurnOffComputerWhenDone)
        {
            var turnOffComputerMode = Enum
                .GetName(typeof(TurnOffComputerMode), TurnOffComputerMode!);

            if (turnOffComputerMode.IsNullOrEmpty())
                return;

            if (turnOffComputerMode!.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
                turnOffComputerMode = "Shut down";

            SelectedTurnOffComputerMode =
                TurnOffComputerModes.FirstOrDefault(m =>
                    m.Equals(turnOffComputerMode, StringComparison.OrdinalIgnoreCase)) ??
                TurnOffComputerModes.FirstOrDefault();
        }

        var daysOfWeek = DaysOfWeek
            .ConvertFromJson<DaysOfWeekViewModel>();

        if (daysOfWeek == null)
            return;

        DaysOfWeekViewModel = daysOfWeek;
    }

    #endregion
}
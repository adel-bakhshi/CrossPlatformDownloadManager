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
    public int DownloadCountAtSameTime { get; set; } = 1;
    public ObservableCollection<DownloadFileViewModel> DownloadFiles { get; set; } = [];
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
            var isAfternoon = StartDownloadSchedule!.Value.Hours > 12;

            StartDownloadHour = StartDownloadSchedule!.Value.Hours - (isAfternoon ? 12 : 0);
            StartDownloadMinute = StartDownloadSchedule!.Value.Minutes;

            var timeOfDay = isAfternoon ? "PM" : "AM";
            SelectedStartTimeOfDay =
                TimesOfDay.FirstOrDefault(t => t.Equals(timeOfDay, StringComparison.OrdinalIgnoreCase)) ??
                TimesOfDay.FirstOrDefault();
        }

        if (StopDownloadScheduleEnabled)
        {
            var isAfternoon = StopDownloadSchedule!.Value.Hours > 12;

            StopDownloadHour = StopDownloadSchedule!.Value.Hours - (isAfternoon ? 12 : 0);
            StopDownloadMinute = StopDownloadSchedule!.Value.Minutes;

            var timeOfDay = isAfternoon ? "PM" : "AM";
            SelectedStopTimeOfDay =
                TimesOfDay.FirstOrDefault(t => t.Equals(timeOfDay, StringComparison.OrdinalIgnoreCase)) ??
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
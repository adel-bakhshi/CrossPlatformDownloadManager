using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueue.Views;

public class OptionsViewModel : ViewModelBase
{
    #region Private Fields

    private string _downloadQueueTitle = string.Empty;
    private bool _isDownloadQueueDefault;
    private bool _startOnApplicationStartup;
    private bool _startDownloadScheduleEnabled;
    private bool _stopDownloadScheduleEnabled;
    private double? _startDownloadHour;
    private double? _startDownloadMinute;
    private double? _stopDownloadHour;
    private double? _stopDownloadMinute;
    private ObservableCollection<string> _timesOfDay = [];
    private string? _selectedStartTimeOfDay;
    private string? _selectedStopTimeOfDay;
    private bool _retryOnDownloadingFailed;
    private int _retryCount;
    private bool _showAlarmWhenDone;
    private bool _exitProgramWhenDone;
    private bool _turnOffComputerWhenDone;
    private ObservableCollection<string> _turnOffComputerModes = [];
    private string? _selectedTurnOffComputerMode;
    private ObservableCollection<string> _startDownloadDateOptions = [];
    private string? _selectedStartDownloadDateOption;
    private ObservableCollection<string> _daysOfWeekOptions = [];
    private DateTime? _selectedDate;
    private bool _isApplicationDefaultQueue;

    #endregion

    #region Properties

    public string DownloadQueueTitle
    {
        get => _downloadQueueTitle;
        set => this.RaiseAndSetIfChanged(ref _downloadQueueTitle, value);
    }

    public bool IsDownloadQueueDefault
    {
        get => _isDownloadQueueDefault;
        set => this.RaiseAndSetIfChanged(ref _isDownloadQueueDefault, value);
    }

    public bool StartOnApplicationStartup
    {
        get => _startOnApplicationStartup;
        set => this.RaiseAndSetIfChanged(ref _startOnApplicationStartup, value);
    }

    public bool StartDownloadScheduleEnabled
    {
        get => _startDownloadScheduleEnabled;
        set => this.RaiseAndSetIfChanged(ref _startDownloadScheduleEnabled, value);
    }

    public bool StopDownloadScheduleEnabled
    {
        get => _stopDownloadScheduleEnabled;
        set => this.RaiseAndSetIfChanged(ref _stopDownloadScheduleEnabled, value);
    }

    public double? StartDownloadHour
    {
        get => _startDownloadHour;
        set => this.RaiseAndSetIfChanged(ref _startDownloadHour, value);
    }

    public double? StartDownloadMinute
    {
        get => _startDownloadMinute;
        set => this.RaiseAndSetIfChanged(ref _startDownloadMinute, value);
    }

    public double? StopDownloadHour
    {
        get => _stopDownloadHour;
        set => this.RaiseAndSetIfChanged(ref _stopDownloadHour, value);
    }

    public double? StopDownloadMinute
    {
        get => _stopDownloadMinute;
        set => this.RaiseAndSetIfChanged(ref _stopDownloadMinute, value);
    }

    public ObservableCollection<string> TimesOfDay
    {
        get => _timesOfDay;
        set => this.RaiseAndSetIfChanged(ref _timesOfDay, value);
    }

    public string? SelectedStartTimeOfDay
    {
        get => _selectedStartTimeOfDay;
        set => this.RaiseAndSetIfChanged(ref _selectedStartTimeOfDay, value);
    }

    public string? SelectedStopTimeOfDay
    {
        get => _selectedStopTimeOfDay;
        set => this.RaiseAndSetIfChanged(ref _selectedStopTimeOfDay, value);
    }

    public bool RetryOnDownloadingFailed
    {
        get => _retryOnDownloadingFailed;
        set => this.RaiseAndSetIfChanged(ref _retryOnDownloadingFailed, value);
    }

    public int RetryCount
    {
        get => _retryCount;
        set => this.RaiseAndSetIfChanged(ref _retryCount, value);
    }

    public bool ShowAlarmWhenDone
    {
        get => _showAlarmWhenDone;
        set => this.RaiseAndSetIfChanged(ref _showAlarmWhenDone, value);
    }

    public bool ExitProgramWhenDone
    {
        get => _exitProgramWhenDone;
        set => this.RaiseAndSetIfChanged(ref _exitProgramWhenDone, value);
    }

    public bool TurnOffComputerWhenDone
    {
        get => _turnOffComputerWhenDone;
        set => this.RaiseAndSetIfChanged(ref _turnOffComputerWhenDone, value);
    }

    public ObservableCollection<string> TurnOffComputerModes
    {
        get => _turnOffComputerModes;
        set => this.RaiseAndSetIfChanged(ref _turnOffComputerModes, value);
    }

    public string? SelectedTurnOffComputerMode
    {
        get => _selectedTurnOffComputerMode;
        set => this.RaiseAndSetIfChanged(ref _selectedTurnOffComputerMode, value);
    }

    public ObservableCollection<string> StartDownloadDateOptions
    {
        get => _startDownloadDateOptions;
        set => this.RaiseAndSetIfChanged(ref _startDownloadDateOptions, value);
    }

    public string? SelectedStartDownloadDateOption
    {
        get => _selectedStartDownloadDateOption;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedStartDownloadDateOption, value);
            ChangeStartDownloadDateOption();
        }
    }

    public ObservableCollection<string> DaysOfWeekOptions
    {
        get => _daysOfWeekOptions;
        set => this.RaiseAndSetIfChanged(ref _daysOfWeekOptions, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set => this.RaiseAndSetIfChanged(ref _selectedDate, value);
    }

    public bool IsDaily { get; set; }
    public DaysOfWeekViewModel? DaysOfWeekViewModel { get; set; }
    public List<string> DaysOfWeek { get; set; } = [];

    public bool IsApplicationDefaultQueue
    {
        get => _isApplicationDefaultQueue;
        set => this.RaiseAndSetIfChanged(ref _isApplicationDefaultQueue, value);
    }

    #endregion

    #region Commands

    public ICommand SelectStartDownloadDateCommand { get; }

    public ICommand ChangeDefaultDownloadQueueCommand { get; }

    #endregion

    public OptionsViewModel(IAppService appService, DownloadQueueViewModel downloadQueue, bool isApplicationDefaultQueue) : base(appService)
    {
        StartDownloadDateOptions = ["Once", "Daily"];
        SelectedStartDownloadDateOption = StartDownloadDateOptions.FirstOrDefault();
        DaysOfWeekOptions = ["Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
        SelectedDate = DateTime.Now;
        IsApplicationDefaultQueue = isApplicationDefaultQueue;

        LoadDownloadQueueData(downloadQueue);

        SelectStartDownloadDateCommand = ReactiveCommand.Create<CalendarDatePicker?>(SelectStartDownloadDate);
        ChangeDefaultDownloadQueueCommand = ReactiveCommand.Create(ChangeDefaultDownloadQueue);
    }

    private void LoadDownloadQueueData(DownloadQueueViewModel downloadQueue)
    {
        TimesOfDay = Constants.TimesOfDay.ToObservableCollection();
        SelectedStartTimeOfDay = SelectedStopTimeOfDay = TimesOfDay.FirstOrDefault();

        TurnOffComputerModes = Constants.TurnOffComputerModes.ToObservableCollection();
        SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault();

        DownloadQueueTitle = downloadQueue.Title ?? string.Empty;
        IsDownloadQueueDefault = downloadQueue.IsDefault;
        StartOnApplicationStartup = downloadQueue.StartOnApplicationStartup;
        StartDownloadScheduleEnabled = downloadQueue.StartDownloadSchedule != null;
        StopDownloadScheduleEnabled = downloadQueue.StopDownloadSchedule != null;
        TurnOffComputerWhenDone = downloadQueue.TurnOffComputerWhenDone;

        if (StartDownloadScheduleEnabled)
        {
            var isAfternoon = downloadQueue.StartDownloadSchedule!.Value.Hours > 12;

            StartDownloadHour = downloadQueue.StartDownloadSchedule!.Value.Hours - (isAfternoon ? 12 : 0);
            StartDownloadMinute = downloadQueue.StartDownloadSchedule!.Value.Minutes;

            var timeOfDay = isAfternoon ? "PM" : "AM";
            SelectedStartTimeOfDay =
                TimesOfDay.FirstOrDefault(t => t.Equals(timeOfDay, StringComparison.OrdinalIgnoreCase)) ??
                TimesOfDay.FirstOrDefault();
        }

        if (StopDownloadScheduleEnabled)
        {
            var isAfternoon = downloadQueue.StopDownloadSchedule!.Value.Hours > 12;

            StopDownloadHour = downloadQueue.StopDownloadSchedule!.Value.Hours - (isAfternoon ? 12 : 0);
            StopDownloadMinute = downloadQueue.StopDownloadSchedule!.Value.Minutes;

            var timeOfDay = isAfternoon ? "PM" : "AM";
            SelectedStopTimeOfDay =
                TimesOfDay.FirstOrDefault(t => t.Equals(timeOfDay, StringComparison.OrdinalIgnoreCase)) ??
                TimesOfDay.FirstOrDefault();
        }

        if (TurnOffComputerWhenDone)
        {
            var turnOffComputerMode = Enum.GetName(typeof(TurnOffComputerMode), downloadQueue.TurnOffComputerMode!);
            if (turnOffComputerMode.IsStringNullOrEmpty())
                return;

            if (turnOffComputerMode!.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
                turnOffComputerMode = "Shut down";

            SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault(m => m.Equals(turnOffComputerMode, StringComparison.OrdinalIgnoreCase)) ??
                                          TurnOffComputerModes.FirstOrDefault();
        }

        DaysOfWeekViewModel = downloadQueue.DaysOfWeek.ConvertFromJson<DaysOfWeekViewModel?>() ?? new DaysOfWeekViewModel();
        IsDaily = downloadQueue.IsDaily;

        // Set selected start download date option
        SelectedStartDownloadDateOption = IsDaily ? "Daily" : "Once";
        if (IsDaily)
        {
            // Create days of week list
            var daysOfWeek = new List<string>();
            if (DaysOfWeekViewModel.Saturday)
                daysOfWeek.Add("Saturday");

            if (DaysOfWeekViewModel.Sunday)
                daysOfWeek.Add("Sunday");

            if (DaysOfWeekViewModel.Monday)
                daysOfWeek.Add("Monday");

            if (DaysOfWeekViewModel.Tuesday)
                daysOfWeek.Add("Tuesday");

            if (DaysOfWeekViewModel.Wednesday)
                daysOfWeek.Add("Wednesday");

            if (DaysOfWeekViewModel.Thursday)
                daysOfWeek.Add("Thursday");

            if (DaysOfWeekViewModel.Friday)
                daysOfWeek.Add("Friday");

            DaysOfWeek = daysOfWeek;
        }
        else
        {
            SelectedDate = downloadQueue.JustForDate;
        }

        RetryOnDownloadingFailed = downloadQueue.RetryOnDownloadingFailed;
        RetryCount = downloadQueue.RetryCount;
        ShowAlarmWhenDone = downloadQueue.ShowAlarmWhenDone;
        ExitProgramWhenDone = downloadQueue.ExitProgramWhenDone;
    }

    public void ChangeDaysOfWeek(List<string> selectedItems)
    {
        DaysOfWeekViewModel ??= new DaysOfWeekViewModel();

        DaysOfWeekViewModel.Saturday = selectedItems.Contains("Saturday");
        DaysOfWeekViewModel.Sunday = selectedItems.Contains("Sunday");
        DaysOfWeekViewModel.Monday = selectedItems.Contains("Monday");
        DaysOfWeekViewModel.Tuesday = selectedItems.Contains("Tuesday");
        DaysOfWeekViewModel.Wednesday = selectedItems.Contains("Wednesday");
        DaysOfWeekViewModel.Thursday = selectedItems.Contains("Thursday");
        DaysOfWeekViewModel.Friday = selectedItems.Contains("Friday");
    }

    #region Helpers

    private static void SelectStartDownloadDate(CalendarDatePicker? datePicker)
    {
        if (datePicker == null)
            return;

        datePicker.IsDropDownOpen = !datePicker.IsDropDownOpen;
    }

    private void ChangeDefaultDownloadQueue()
    {
        IsDownloadQueueDefault = !IsDownloadQueueDefault;
    }

    private void ChangeStartDownloadDateOption()
    {
        IsDaily = SelectedStartDownloadDateOption?.Equals("Daily") ?? false;
    }

    #endregion
}
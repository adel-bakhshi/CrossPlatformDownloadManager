using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadQueueViewModel : PropertyChangedBase
{
    #region Private Fields

    private IUnitOfWork? _unitOfWork;
    private IDownloadFileService? _downloadFileService;

    private int _id;
    private string? _title;
    private bool _startOnApplicationStartup;
    private TimeSpan? _startDownloadSchedule;
    private TimeSpan? _stopDownloadSchedule;
    private bool _isDaily;
    private DateTime? _justForDate;
    private string? _daysOfWeek;
    private bool _retryOnDownloadingFailed;
    private int _retryCount;
    private bool _showAlarmWhenDone;
    private bool _exitProgramWhenDone;
    private bool _turnOffComputerWhenDone;
    private TurnOffComputerMode? _turnOffComputerMode;
    private bool _isDefault;
    private bool _isLastChoice;
    private int _downloadCountAtSameTime = 1;
    private bool _includePausedFiles;
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];
    private bool _startDownloadScheduleEnabled;
    private double? _startDownloadHour;
    private double? _startDownloadMinute;
    private bool _stopDownloadScheduleEnabled;
    private double? _stopDownloadHour;
    private double? _stopDownloadMinute;
    private readonly ObservableCollection<string> _timesOfDay = [];
    private string? _selectedStartTimeOfDay;
    private string? _selectedStopTimeOfDay;
    private readonly ObservableCollection<string> _turnOffComputerModes = [];
    private string? _selectedTurnOffComputerMode;
    private DaysOfWeekViewModel? _daysOfWeekViewModel;
    private bool _isRunning;

    private List<DownloadFileViewModel> _downloadingFiles = [];

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string? Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public bool StartOnApplicationStartup
    {
        get => _startOnApplicationStartup;
        set => SetField(ref _startOnApplicationStartup, value);
    }

    public TimeSpan? StartDownloadSchedule
    {
        get => _startDownloadSchedule;
        set => SetField(ref _startDownloadSchedule, value);
    }

    public TimeSpan? StopDownloadSchedule
    {
        get => _stopDownloadSchedule;
        set => SetField(ref _stopDownloadSchedule, value);
    }

    public bool IsDaily
    {
        get => _isDaily;
        set => SetField(ref _isDaily, value);
    }

    public DateTime? JustForDate
    {
        get => _justForDate;
        set => SetField(ref _justForDate, value);
    }

    public string? DaysOfWeek
    {
        get => _daysOfWeek;
        set => SetField(ref _daysOfWeek, value);
    }

    public bool RetryOnDownloadingFailed
    {
        get => _retryOnDownloadingFailed;
        set => SetField(ref _retryOnDownloadingFailed, value);
    }

    public int RetryCount
    {
        get => _retryCount;
        set => SetField(ref _retryCount, value);
    }

    public bool ShowAlarmWhenDone
    {
        get => _showAlarmWhenDone;
        set => SetField(ref _showAlarmWhenDone, value);
    }

    public bool ExitProgramWhenDone
    {
        get => _exitProgramWhenDone;
        set => SetField(ref _exitProgramWhenDone, value);
    }

    public bool TurnOffComputerWhenDone
    {
        get => _turnOffComputerWhenDone;
        set => SetField(ref _turnOffComputerWhenDone, value);
    }

    public TurnOffComputerMode? TurnOffComputerMode
    {
        get => _turnOffComputerMode;
        set => SetField(ref _turnOffComputerMode, value);
    }

    public bool IsDefault
    {
        get => _isDefault;
        set => SetField(ref _isDefault, value);
    }

    public bool IsLastChoice
    {
        get => _isLastChoice;
        set => SetField(ref _isLastChoice, value);
    }

    public int DownloadCountAtSameTime
    {
        get => _downloadCountAtSameTime;
        set => SetField(ref _downloadCountAtSameTime, value);
    }

    public bool IncludePausedFiles
    {
        get => _includePausedFiles;
        set => SetField(ref _includePausedFiles, value);
    }

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => SetField(ref _downloadFiles, value);
    }

    public bool StartDownloadScheduleEnabled
    {
        get => _startDownloadScheduleEnabled;
        set => SetField(ref _startDownloadScheduleEnabled, value);
    }

    public double? StartDownloadHour
    {
        get => _startDownloadHour;
        set => SetField(ref _startDownloadHour, value);
    }

    public double? StartDownloadMinute
    {
        get => _startDownloadMinute;
        set => SetField(ref _startDownloadMinute, value);
    }

    public bool StopDownloadScheduleEnabled
    {
        get => _stopDownloadScheduleEnabled;
        set => SetField(ref _stopDownloadScheduleEnabled, value);
    }

    public double? StopDownloadHour
    {
        get => _stopDownloadHour;
        set => SetField(ref _stopDownloadHour, value);
    }

    public double? StopDownloadMinute
    {
        get => _stopDownloadMinute;
        set => SetField(ref _stopDownloadMinute, value);
    }

    public ObservableCollection<string> TimesOfDay
    {
        get => _timesOfDay;
        private init => SetField(ref _timesOfDay, value);
    }

    public string? SelectedStartTimeOfDay
    {
        get => _selectedStartTimeOfDay;
        set => SetField(ref _selectedStartTimeOfDay, value);
    }

    public string? SelectedStopTimeOfDay
    {
        get => _selectedStopTimeOfDay;
        set => SetField(ref _selectedStopTimeOfDay, value);
    }

    public ObservableCollection<string> TurnOffComputerModes
    {
        get => _turnOffComputerModes;
        private init => SetField(ref _turnOffComputerModes, value);
    }

    public string? SelectedTurnOffComputerMode
    {
        get => _selectedTurnOffComputerMode;
        set => SetField(ref _selectedTurnOffComputerMode, value);
    }

    public DaysOfWeekViewModel? DaysOfWeekViewModel
    {
        get => _daysOfWeekViewModel;
        set => SetField(ref _daysOfWeekViewModel, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set => SetField(ref _isRunning, value);
    }

    public List<DownloadFileViewModel> DownloadingFiles
    {
        get => _downloadingFiles;
        set => SetField(ref _downloadingFiles, value);
    }

    #endregion

    public DownloadQueueViewModel()
    {
        TimesOfDay = Constants.TimesOfDay.ToObservableCollection();
        SelectedStartTimeOfDay = SelectedStopTimeOfDay = TimesOfDay.FirstOrDefault();

        TurnOffComputerModes = Constants.TurnOffComputerModes.ToObservableCollection();
        SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault();

        DaysOfWeekViewModel = new DaysOfWeekViewModel();
    }

    public async Task StartDownloadQueueAsync(IUnitOfWork? unitOfWork, IDownloadFileService? downloadFileService)
    {
        if (unitOfWork == null || downloadFileService == null)
            return;

        _unitOfWork = unitOfWork;
        _downloadFileService = downloadFileService;

        await ContinueDownloadQueueAsync();
    }

    public async Task ContinueDownloadQueueAsync()
    {
        if (_unitOfWork == null || _downloadFileService == null)
            return;

        var primaryKeys = await _unitOfWork
            .DownloadFileRepository
            .GetAllAsync(where: df => df.DownloadQueueId == Id, select: df => df.Id, distinct: true);

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == Id
                         && primaryKeys.Contains(df.Id)
                         && df is { IsDownloading: false, IsPaused: false, IsCompleted: false }
                         && (!RetryOnDownloadingFailed || df.CountOfError < RetryCount))
            .ToList();

        if (IncludePausedFiles)
        {
            var pausedDownloadFiles = _downloadFileService
                .DownloadFiles
                .Where(df => df.DownloadQueueId == Id
                             && primaryKeys.Contains(df.Id)
                             && df.IsPaused
                             && (!RetryOnDownloadingFailed || df.CountOfError < RetryCount))
                .ToList();

            downloadFiles = downloadFiles
                .Union(pausedDownloadFiles)
                .ToList();
        }

        downloadFiles = downloadFiles
            .OrderBy(df => df.DownloadQueuePriority)
            .ToList();

        if (downloadFiles.Count == 0 && DownloadingFiles.Count == 0)
        {
            await StopDownloadQueueAsync();
            return;
        }

        if (!IsRunning)
            IsRunning = true;

        var taskIndex = 0;
        while (DownloadingFiles.Count < DownloadCountAtSameTime && taskIndex < downloadFiles.Count)
        {
            var downloadFile = downloadFiles[taskIndex];
            downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
            downloadFile.DownloadPaused += DownloadFileOnDownloadPaused;

            _ = _downloadFileService.StartDownloadFileAsync(downloadFile);

            DownloadingFiles.Add(downloadFile);

            taskIndex++;
        }

        if (DownloadingFiles.Count == 0)
            await StopDownloadQueueAsync();
    }

    public async Task StopDownloadQueueAsync()
    {
        if (_downloadFileService == null)
            return;

        IsRunning = false;

        var downloadFiles = _downloadFileService
            .DownloadFiles
            .Where(df => df.DownloadQueueId == Id && df.IsDownloading)
            .ToList();

        foreach (var downloadFile in downloadFiles)
            await _downloadFileService.StopDownloadFileAsync(downloadFile);

        downloadFiles = downloadFiles
            .Select(df =>
            {
                df.CountOfError = 0;
                return df;
            })
            .ToList();

        await _downloadFileService.UpdateDownloadFilesAsync(downloadFiles);
    }

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
            var turnOffComputerMode = Enum.GetName(typeof(TurnOffComputerMode), TurnOffComputerMode!);
            if (turnOffComputerMode.IsNullOrEmpty())
                return;

            if (turnOffComputerMode!.Equals("Shutdown", StringComparison.OrdinalIgnoreCase))
                turnOffComputerMode = "Shut down";

            SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault(m => m.Equals(turnOffComputerMode, StringComparison.OrdinalIgnoreCase)) ??
                                          TurnOffComputerModes.FirstOrDefault();
        }

        var daysOfWeek = DaysOfWeek.ConvertFromJson<DaysOfWeekViewModel>();
        if (daysOfWeek == null)
            return;

        DaysOfWeekViewModel = daysOfWeek;
    }

    #region Helpers

    private async void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        if (_unitOfWork == null || _downloadFileService == null)
            return;

        var downloadFile = DownloadingFiles.Find(df => df.Id == e.Id);
        if (downloadFile == null)
        {
            if (sender is not DownloadFileViewModel downloadFileViewModel)
                return;

            downloadFileViewModel.DownloadFinished -= DownloadFileOnDownloadFinished;
            downloadFileViewModel.DownloadPaused -= DownloadFileOnDownloadPaused;

            return;
        }

        downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
        downloadFile.DownloadPaused -= DownloadFileOnDownloadPaused;

        if (downloadFile.IsCompleted)
        {
            downloadFile.DownloadQueueId = null;
            downloadFile.DownloadQueueName = null;
            downloadFile.DownloadQueuePriority = null;
        }
        else if ((downloadFile.IsError || downloadFile.IsStopped) && IsRunning)
        {
            var maxPriority = await _unitOfWork
                .DownloadFileRepository
                .GetMaxAsync(selector: df => df.DownloadQueuePriority,
                    where: df => df.DownloadQueueId == Id);

            downloadFile.DownloadQueuePriority = maxPriority + 1;

            if (downloadFile.IsError)
                downloadFile.CountOfError++;
        }

        if (downloadFile.IsCompleted || downloadFile.IsError || downloadFile.IsStopped)
            DownloadingFiles.Remove(downloadFile);

        await _downloadFileService.UpdateDownloadFileAsync(downloadFile);

        if (IsRunning)
            _ = ContinueDownloadQueueAsync();
    }

    private void DownloadFileOnDownloadPaused(object? sender, DownloadFileEventArgs e)
    {
        if (IncludePausedFiles || !IsRunning)
            return;

        _ = ContinueDownloadQueueAsync();
    }

    #endregion
}
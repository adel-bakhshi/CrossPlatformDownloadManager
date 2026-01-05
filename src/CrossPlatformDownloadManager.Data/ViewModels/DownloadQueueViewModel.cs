using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DownloadQueueViewModel : PropertyChangedBase
{
    #region Private Fields

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

    public bool IsStartSoundPlayed { get; set; }
    public bool IsScheduleEnabled { get; set; }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddNewQueueWindowViewModel : ViewModelBase
{
    #region Properties

    private int? _downloadQueueId;

    public int? DownloadQueueId
    {
        get => _downloadQueueId;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadQueueId, value);
            DownloadFiles = GetDownloadFiles();
        }
    }

    private string? _queueTitle;

    public string? QueueTitle
    {
        get => _queueTitle;
        set => this.RaiseAndSetIfChanged(ref _queueTitle, value?.Trim());
    }

    private decimal? _startDownloadHour;

    public decimal? StartDownloadHour
    {
        get => _startDownloadHour;
        set => this.RaiseAndSetIfChanged(ref _startDownloadHour, value);
    }

    private decimal? _startDownloadMinute;

    public decimal? StartDownloadMinute
    {
        get => _startDownloadMinute;
        set => this.RaiseAndSetIfChanged(ref _startDownloadMinute, value);
    }

    private decimal? _stopDownloadHour;

    public decimal? StopDownloadHour
    {
        get => _stopDownloadHour;
        set => this.RaiseAndSetIfChanged(ref _stopDownloadHour, value);
    }

    private decimal? _stopDownloadMinute;

    public decimal? StopDownloadMinute
    {
        get => _stopDownloadMinute;
        set => this.RaiseAndSetIfChanged(ref _stopDownloadMinute, value);
    }

    private ObservableCollection<string> _timesOfDay = [];

    public ObservableCollection<string> TimesOfDay
    {
        get => _timesOfDay;
        set => this.RaiseAndSetIfChanged(ref _timesOfDay, value);
    }

    private string? _selectedStartTimeOfDay;

    public string? SelectedStartTimeOfDay
    {
        get => _selectedStartTimeOfDay;
        set => this.RaiseAndSetIfChanged(ref _selectedStartTimeOfDay, value);
    }

    private string? _selectedStopTimeOfDay;

    public string? SelectedStopTimeOfDay
    {
        get => _selectedStopTimeOfDay;
        set => this.RaiseAndSetIfChanged(ref _selectedStopTimeOfDay, value);
    }

    private bool _isDailyDownload = false;

    public bool IsDailyDownload
    {
        get => _isDailyDownload;
        set => this.RaiseAndSetIfChanged(ref _isDailyDownload, value);
    }

    private decimal? _numberOfRetries = 3;

    public decimal? NumberOfRetries
    {
        get => _numberOfRetries;
        set => this.RaiseAndSetIfChanged(ref _numberOfRetries, value);
    }

    private ObservableCollection<string> _turnOffComputerModes = [];

    public ObservableCollection<string> TurnOffComputerModes
    {
        get => _turnOffComputerModes;
        set => this.RaiseAndSetIfChanged(ref _turnOffComputerModes, value);
    }

    private string? _selectedTurnOffComputerMode;

    public string? SelectedTurnOffComputerMode
    {
        get => _selectedTurnOffComputerMode;
        set => this.RaiseAndSetIfChanged(ref _selectedTurnOffComputerMode, value);
    }

    private bool _startDownloadOnApplicationStartup;

    public bool StartDownloadOnApplicationStartup
    {
        get => _startDownloadOnApplicationStartup;
        set => this.RaiseAndSetIfChanged(ref _startDownloadOnApplicationStartup, value);
    }

    private bool _startDownloadScheduleEnabled;

    public bool StartDownloadScheduleEnabled
    {
        get => _startDownloadScheduleEnabled;
        set => this.RaiseAndSetIfChanged(ref _startDownloadScheduleEnabled, value);
    }

    private bool _stopDownloadScheduleEnabled;

    public bool StopDownloadScheduleEnabled
    {
        get => _stopDownloadScheduleEnabled;
        set => this.RaiseAndSetIfChanged(ref _stopDownloadScheduleEnabled, value);
    }

    private DaysOfWeekViewModel _daysOfWeek = new();

    public DaysOfWeekViewModel DaysOfWeek
    {
        get => _daysOfWeek;
        set => this.RaiseAndSetIfChanged(ref _daysOfWeek, value);
    }

    private bool _retryOnDownloadFailed;

    public bool RetryOnDownloadFailed
    {
        get => _retryOnDownloadFailed;
        set => this.RaiseAndSetIfChanged(ref _retryOnDownloadFailed, value);
    }

    private bool _showAlarmWhenDone;

    public bool ShowAlarmWhenDone
    {
        get => _showAlarmWhenDone;
        set => this.RaiseAndSetIfChanged(ref _showAlarmWhenDone, value);
    }

    private bool _exitProgramWhenDone;

    public bool ExitProgramWhenDone
    {
        get => _exitProgramWhenDone;
        set => this.RaiseAndSetIfChanged(ref _exitProgramWhenDone, value);
    }

    private bool _turnOffComputerWhenDone;

    public bool TurnOffComputerWhenDone
    {
        get => _turnOffComputerWhenDone;
        set => this.RaiseAndSetIfChanged(ref _turnOffComputerWhenDone, value);
    }

    private bool _showOptionsView = true;

    public bool ShowOptionsView
    {
        get => _showOptionsView;
        set => this.RaiseAndSetIfChanged(ref _showOptionsView, value);
    }

    private bool _showFilesView;

    public bool ShowFilesView
    {
        get => _showFilesView;
        set => this.RaiseAndSetIfChanged(ref _showFilesView, value);
    }

    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand ChangeStartDownloadDateCommand { get; }

    public ICommand ChangeViewCommand { get; }

    #endregion

    public AddNewQueueWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper)
        : base(unitOfWork, downloadFileService, mapper)
    {
        TimesOfDay = Constants.TimesOfDay.ToObservableCollection();
        SelectedStartTimeOfDay = SelectedStopTimeOfDay = TimesOfDay.FirstOrDefault();
        TurnOffComputerModes = Constants.TurnOffComputerModes.ToObservableCollection();
        SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault();
        DownloadFiles = GetDownloadFiles();

        SaveCommand = ReactiveCommand.Create<Window?>(Save);
        ChangeStartDownloadDateCommand = ReactiveCommand.Create<string?>(ChangeStartDownloadDate);
        ChangeViewCommand = ReactiveCommand.Create<ToggleButton?>(ChangeView);
    }

    private ObservableCollection<DownloadFileViewModel> GetDownloadFiles()
    {
        if (DownloadQueueId == null)
            return [];

        return DownloadFileService.DownloadFiles
            .Where(df => df.DownloadQueueId == DownloadQueueId)
            .ToObservableCollection();
    }

    private void ChangeView(ToggleButton? button)
    {
        var buttonName = button?.Name;
        switch (buttonName)
        {
            case "OptionsButton":
            {
                ShowFilesView = false;
                ShowOptionsView = true;
                break;
            }

            case "FilesButton":
            {
                ShowOptionsView = false;
                ShowFilesView = true;
                break;
            }
        }
    }

    private void ChangeStartDownloadDate(string? value)
    {
        if (value.IsNullOrEmpty())
            return;

        IsDailyDownload = value!.Equals("Daily");
    }

    private async void Save(Window? owner)
    {
        try
        {
            if (owner == null || QueueTitle.IsNullOrEmpty())
                return;

            TimeSpan? startSchedule = null;
            TimeSpan? stopSchedule = null;

            if (StartDownloadScheduleEnabled)
            {
                if (StartDownloadHour == null || StartDownloadMinute == null)
                    return;

                var is24Hour = !SelectedStartTimeOfDay.IsNullOrEmpty() && SelectedStartTimeOfDay!.Equals("PM");
                startSchedule = TimeSpan
                    .FromHours((double)StartDownloadHour.Value + (is24Hour ? 12 : 0))
                    .Add(TimeSpan.FromMinutes((double)StartDownloadMinute));
            }

            if (StopDownloadScheduleEnabled)
            {
                if (StopDownloadHour == null || StopDownloadMinute == null)
                    return;

                var is24Hour = !SelectedStopTimeOfDay.IsNullOrEmpty() && SelectedStopTimeOfDay!.Equals("PM");
                stopSchedule = TimeSpan.FromHours((double)StopDownloadHour.Value + (is24Hour ? 12 : 0))
                    .Add(TimeSpan.FromMinutes((double)StopDownloadMinute));
            }

            if (RetryOnDownloadFailed && NumberOfRetries == null)
                return;

            TurnOffComputerMode? turnOffComputerMode = null;
            if (TurnOffComputerWhenDone)
            {
                if (SelectedTurnOffComputerMode.IsNullOrEmpty())
                    return;

                switch (SelectedTurnOffComputerMode)
                {
                    case "Shut down":
                    {
                        turnOffComputerMode = TurnOffComputerMode.Shutdown;
                        break;
                    }

                    case "Sleep":
                    {
                        turnOffComputerMode = TurnOffComputerMode.Sleep;
                        break;
                    }

                    case "Hibernate":
                    {
                        turnOffComputerMode = TurnOffComputerMode.Hibernate;
                        break;
                    }
                }
            }

            var downloadQueue = new DownloadQueue
            {
                Title = QueueTitle!.Trim(),
                StartOnApplicationStartup = StartDownloadOnApplicationStartup,
                StartDownloadSchedule = startSchedule,
                StopDownloadSchedule = stopSchedule,
                IsDaily = IsDailyDownload,
                JustForDate = IsDailyDownload ? null : DateTime.Now,
                DaysOfWeek = IsDailyDownload ? DaysOfWeek.ConvertToJson() : null,
                RetryOnDownloadingFailed = RetryOnDownloadFailed,
                RetryCount = (int)NumberOfRetries!,
                ShowAlarmWhenDone = ShowAlarmWhenDone,
                ExitProgramWhenDone = ExitProgramWhenDone,
                TurnOffComputerWhenDone = TurnOffComputerWhenDone,
                TurnOffComputerMode = turnOffComputerMode,
                IsDefault = false,
            };

            await UnitOfWork.DownloadQueueRepository.AddAsync(downloadQueue);
            await UnitOfWork.SaveAsync();

            var primaryKeys = DownloadFiles.Select(df => df.Id).Distinct().ToList();
            var downloadFiles = await UnitOfWork.DownloadFileRepository
                .GetAllAsync(where: df => primaryKeys.Contains(df.Id));

            var maxQueuePriority = (await UnitOfWork.DownloadFileRepository
                    .GetAllAsync(where: df => df.DownloadQueueId == downloadQueue.Id, select: df => df.QueuePriority))
                .Max() ?? 0;

            for (int i = 0; i < downloadFiles.Count; i++)
            {
                downloadFiles[i].DownloadQueueId = downloadQueue.Id;
                downloadFiles[i].QueuePriority = maxQueuePriority + 1 + i;
            }

            await DownloadFileService.UpdateFilesAsync(downloadFiles);
            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
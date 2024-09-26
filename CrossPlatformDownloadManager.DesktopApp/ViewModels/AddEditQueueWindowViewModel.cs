using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddEditQueueWindowViewModel : ViewModelBase
{
    #region Properties

    public string Title => IsEditMode ? "CDM - Edit Queue" : "CDM - Add New Queue";

    private bool _isEditMode;

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEditMode, value);
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    private ObservableCollection<string> _tabItems = [];

    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    private string? _selectedTabItem;

    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
    }

    private DownloadQueueViewModel _downloadQueue;

    public DownloadQueueViewModel DownloadQueue
    {
        get => _downloadQueue;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadQueue, value);
            LoadDownloadQueueData();
        }
    }

    private FilesViewModel? _filesViewModel;

    public FilesViewModel? FilesViewModel
    {
        get => _filesViewModel;
        set => this.RaiseAndSetIfChanged(ref _filesViewModel, value);
    }

    #endregion

    #region Commands

    public ICommand? SaveCommand { get; }

    public ICommand? ChangeStartDownloadDateCommand { get; }

    #endregion

    public AddEditQueueWindowViewModel(IAppService appService) : base(appService)
    {
        DownloadQueue = new DownloadQueueViewModel();
        TabItems = ["Options", "Files"];
        SelectedTabItem = TabItems.FirstOrDefault();

        FilesViewModel = new FilesViewModel(appService) { DownloadQueue = DownloadQueue };

        SaveCommand = ReactiveCommand.Create<Window?>(Save);
        ChangeStartDownloadDateCommand = ReactiveCommand.Create<string?>(ChangeStartDownloadDate);
    }

    private void LoadDownloadQueueData()
    {
        if (!IsEditMode)
            return;

        DownloadQueue.LoadViewData();
    }

    private void ChangeStartDownloadDate(string? value)
    {
        if (value.IsNullOrEmpty())
            return;

        DownloadQueue.IsDaily = value!.Equals("Daily");
    }

    private async void Save(Window? owner)
    {
        try
        {
            if (owner == null || DownloadQueue.Title.IsNullOrEmpty())
                return;

            TimeSpan? startSchedule = null;
            TimeSpan? stopSchedule = null;

            if (DownloadQueue.StartDownloadScheduleEnabled)
            {
                if (DownloadQueue.StartDownloadHour == null || DownloadQueue.StartDownloadMinute == null)
                    return;

                var isAfternoon = !DownloadQueue.SelectedStartTimeOfDay.IsNullOrEmpty() &&
                                  DownloadQueue.SelectedStartTimeOfDay!.Equals("PM");
                startSchedule = TimeSpan
                    .FromHours(DownloadQueue.StartDownloadHour.Value + (isAfternoon ? 12 : 0))
                    .Add(TimeSpan.FromMinutes(DownloadQueue.StartDownloadMinute.Value));
            }

            if (DownloadQueue.StopDownloadScheduleEnabled)
            {
                if (DownloadQueue.StopDownloadHour == null || DownloadQueue.StopDownloadMinute == null)
                    return;

                var isAfternoon = !DownloadQueue.SelectedStopTimeOfDay.IsNullOrEmpty() &&
                                  DownloadQueue.SelectedStopTimeOfDay!.Equals("PM");
                stopSchedule = TimeSpan.FromHours(DownloadQueue.StopDownloadHour.Value + (isAfternoon ? 12 : 0))
                    .Add(TimeSpan.FromMinutes(DownloadQueue.StopDownloadMinute.Value));
            }

            if (DownloadQueue is { RetryOnDownloadingFailed: true, RetryCount: 0 })
                return;

            TurnOffComputerMode? turnOffComputerMode = null;
            if (DownloadQueue.TurnOffComputerWhenDone)
            {
                if (DownloadQueue.SelectedTurnOffComputerMode.IsNullOrEmpty())
                    return;

                switch (DownloadQueue.SelectedTurnOffComputerMode)
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

            if (FilesViewModel == null)
                return;

            DownloadQueue.Title = DownloadQueue.Title!.Trim();
            DownloadQueue.StartDownloadSchedule = startSchedule;
            DownloadQueue.StopDownloadSchedule = stopSchedule;
            // TODO: This is incorrect
            DownloadQueue.JustForDate = DownloadQueue.IsDaily ? null : DateTime.Now;
            DownloadQueue.DaysOfWeek = DownloadQueue.IsDaily ? DownloadQueue.DaysOfWeekViewModel.ConvertToJson() : null;
            DownloadQueue.TurnOffComputerMode = turnOffComputerMode;
            DownloadQueue.IsDefault = false;
            // TODO: I think this is properly filled in FilesViewModel but check it again
            // DownloadQueue.DownloadCountAtSameTime = FilesViewModel.DownloadFilesCountAtTheSameTime;

            var downloadQueue = AppService
                .Mapper
                .Map<DownloadQueue>(DownloadQueue);

            await AppService
                .DownloadQueueService
                .AddNewDownloadQueueAsync(downloadQueue);

            var primaryKeys = FilesViewModel
                .DownloadFiles
                .Select(df => df.Id)
                .Distinct()
                .ToList();

            var downloadFiles = await AppService
                .UnitOfWork
                .DownloadFileRepository
                .GetAllAsync(where: df => primaryKeys.Contains(df.Id));

            var maxQueuePriority = await AppService
                .UnitOfWork
                .DownloadFileRepository
                .GetMaxAsync(selector: df => df.DownloadQueuePriority,
                    where: df => df.DownloadQueueId == downloadQueue.Id) ?? 0;

            for (var i = 0; i < downloadFiles.Count; i++)
            {
                downloadFiles[i].DownloadQueueId = downloadQueue.Id;
                downloadFiles[i].DownloadQueuePriority = maxQueuePriority + 1 + i;
            }

            await AppService
                .DownloadFileService
                .UpdateDownloadFilesAsync(downloadFiles);

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;
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

    private OptionsViewModel? _optionsViewModel;

    public OptionsViewModel? OptionsViewModel
    {
        get => _optionsViewModel;
        set => this.RaiseAndSetIfChanged(ref _optionsViewModel, value);
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

    #endregion

    public AddEditQueueWindowViewModel(IAppService appService) : base(appService)
    {
        DownloadQueue = new DownloadQueueViewModel();
        TabItems = ["Options", "Files"];
        SelectedTabItem = TabItems.FirstOrDefault();

        OptionsViewModel = new OptionsViewModel(appService) { DownloadQueue = DownloadQueue };
        FilesViewModel = new FilesViewModel(appService) { DownloadQueue = DownloadQueue };

        SaveCommand = ReactiveCommand.Create<Window?>(Save);
    }

    private void LoadDownloadQueueData()
    {
        if (!IsEditMode)
            return;

        DownloadQueue.LoadViewData();
        this.RaisePropertyChanged(nameof(DownloadQueue));
        OptionsViewModel!.DownloadQueue = DownloadQueue;
        FilesViewModel!.DownloadQueue = DownloadQueue;
    }

    private async void Save(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (DownloadQueue.IsRunning)
                return;

            if (OptionsViewModel == null || FilesViewModel == null)
                return;

            if (DownloadQueue.Title.IsNullOrEmpty())
                return;

            if (DownloadQueue is { RetryOnDownloadingFailed: true, RetryCount: 0 })
                return;

            if (DownloadQueue.DownloadCountAtSameTime == 0)
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

            DownloadQueue.Title = DownloadQueue.Title!.Trim();
            DownloadQueue.StartDownloadSchedule = startSchedule;
            DownloadQueue.StopDownloadSchedule = stopSchedule;
            // TODO: This is incorrect
            DownloadQueue.JustForDate = DownloadQueue.IsDaily ? null : DateTime.Now;
            DownloadQueue.DaysOfWeek = DownloadQueue.IsDaily ? DownloadQueue.DaysOfWeekViewModel.ConvertToJson() : null;
            DownloadQueue.TurnOffComputerMode = turnOffComputerMode;

            if (!IsEditMode)
                DownloadQueue.IsDefault = false;

            var downloadQueue = AppService
                .Mapper
                .Map<DownloadQueue>(DownloadQueue);

            if (!IsEditMode)
            {
                await AppService
                    .DownloadQueueService
                    .AddNewDownloadQueueAsync(downloadQueue);
            }
            else
            {
                await AppService
                    .DownloadQueueService
                    .UpdateDownloadQueueAsync(downloadQueue);
            }

            if (IsEditMode)
            {
                var oldDownloadFiles = await AppService
                    .UnitOfWork
                    .DownloadFileRepository
                    .GetAllAsync(where: df => df.DownloadQueueId == downloadQueue.Id);

                if (oldDownloadFiles.Count > 0)
                {
                    foreach (var downloadFile in oldDownloadFiles)
                    {
                        downloadFile.DownloadQueueId = null;
                        downloadFile.DownloadQueuePriority = null;
                    }

                    await AppService
                        .DownloadFileService
                        .UpdateDownloadFilesAsync(oldDownloadFiles);
                }
            }

            var maxQueuePriority = await AppService
                .UnitOfWork
                .DownloadFileRepository
                .GetMaxAsync(selector: df => df.DownloadQueuePriority,
                    where: df => df.DownloadQueueId == downloadQueue.Id) ?? 0;

            var downloadFiles = FilesViewModel
                .DownloadFiles
                .Select((df, i) =>
                {
                    df.DownloadQueueId = downloadQueue.Id;
                    df.DownloadQueuePriority = maxQueuePriority + 1 + i;
                    return df;
                })
                .ToList();

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
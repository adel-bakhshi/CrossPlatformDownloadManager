using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
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

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null || OptionsViewModel == null || FilesViewModel == null)
                throw new InvalidOperationException("An error occured while trying to save queue.");

            if (DownloadQueue.IsRunning)
            {
                var result = await ShowWarningDialogAsync("Warning", "Queue is running. Do you want to stop it?", DialogButtons.YesNo);
                if (result != DialogResult.Yes)
                    return;

                await AppService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(DownloadQueue);
            }

            if (DownloadQueue.Title.IsNullOrEmpty())
            {
                await ShowInfoDialogAsync("Attention", "Please enter a title.", DialogButtons.Ok);
                return;
            }

            if (DownloadQueue is { RetryOnDownloadingFailed: true, RetryCount: < 1 })
            {
                var result = await ShowInfoDialogAsync("Attention", "Retry count must be greater than 0. Do you want to set it to 1?", DialogButtons.YesNo);
                if (result != DialogResult.Yes)
                    return;

                DownloadQueue.RetryCount = 1;
            }

            if (DownloadQueue.DownloadCountAtSameTime == 0)
            {
                var result = await ShowInfoDialogAsync("Attention", "Download count at same time must be greater than 0. Do you want to set it to 1?", DialogButtons.YesNo);
                if (result != DialogResult.Yes)
                    return;

                DownloadQueue.DownloadCountAtSameTime = 1;
            }

            switch (DownloadQueue)
            {
                case { IsDaily: false, JustForDate: null }:
                {
                    await ShowInfoDialogAsync("Attention", "When you choose 'Once', please select a date.", DialogButtons.Ok);
                    return;
                }

                case { IsDaily: true, DaysOfWeekViewModel: not { IsAnyDaySelected: true } }:
                {
                    await ShowInfoDialogAsync("Attention", "When you choose 'Daily', please select at least one day.", DialogButtons.Ok);
                    return;
                }
            }

            TimeSpan? startSchedule = null;
            TimeSpan? stopSchedule = null;

            if (DownloadQueue.StartDownloadScheduleEnabled)
            {
                if (DownloadQueue.StartDownloadHour == null || DownloadQueue.StartDownloadMinute == null)
                {
                    await ShowInfoDialogAsync("Attention", "Please select a start time for your queue.", DialogButtons.Ok);
                    return;
                }

                if (DownloadQueue.SelectedStartTimeOfDay.IsNullOrEmpty())
                {
                    await ShowInfoDialogAsync("Attention", "Please select a start time of day for your queue.", DialogButtons.Ok);
                    return;
                }

                var isAfternoon = DownloadQueue.SelectedStartTimeOfDay!.Equals("PM");
                startSchedule = TimeSpan
                    .FromHours(DownloadQueue.StartDownloadHour.Value + (isAfternoon ? 12 : 0))
                    .Add(TimeSpan.FromMinutes(DownloadQueue.StartDownloadMinute.Value));
            }

            if (DownloadQueue.StopDownloadScheduleEnabled)
            {
                if (DownloadQueue.StopDownloadHour == null || DownloadQueue.StopDownloadMinute == null)
                {
                    await ShowInfoDialogAsync("Attention", "Please select a stop time for your queue.", DialogButtons.Ok);
                    return;
                }

                if (DownloadQueue.SelectedStopTimeOfDay.IsNullOrEmpty())
                {
                    await ShowInfoDialogAsync("Attention", "Please select a stop time of day for your queue.", DialogButtons.Ok);
                    return;
                }

                var isAfternoon = DownloadQueue.SelectedStopTimeOfDay!.Equals("PM");
                stopSchedule = TimeSpan
                    .FromHours(DownloadQueue.StopDownloadHour.Value + (isAfternoon ? 12 : 0))
                    .Add(TimeSpan.FromMinutes(DownloadQueue.StopDownloadMinute.Value));
            }

            TurnOffComputerMode? turnOffComputerMode = null;
            if (DownloadQueue.TurnOffComputerWhenDone)
            {
                if (DownloadQueue.SelectedTurnOffComputerMode.IsNullOrEmpty())
                {
                    await ShowInfoDialogAsync("Attention", "Please select a turn off computer mode for your queue.", DialogButtons.Ok);
                    return;
                }

                turnOffComputerMode = DownloadQueue.SelectedTurnOffComputerMode switch
                {
                    "Shut down" => TurnOffComputerMode.Shutdown,
                    "Sleep" => TurnOffComputerMode.Sleep,
                    "Hibernate" => TurnOffComputerMode.Hibernate,
                    _ => turnOffComputerMode
                };
            }

            DownloadQueue.Title = DownloadQueue.Title!.Trim();
            DownloadQueue.StartDownloadSchedule = startSchedule;
            DownloadQueue.StopDownloadSchedule = stopSchedule;

            if (DownloadQueue.IsDaily)
            {
                DownloadQueue.JustForDate = null;
                DownloadQueue.DaysOfWeek = DownloadQueue.DaysOfWeekViewModel.ConvertToJson();
            }
            else
            {
                DownloadQueue.DaysOfWeek = null;
            }

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
                .GetMaxAsync(selector: df => df.DownloadQueuePriority, where: df => df.DownloadQueueId == downloadQueue.Id) ?? 0;

            var downloadFiles = FilesViewModel
                .DownloadFiles
                .Select((df, i) =>
                {
                    df.DownloadQueueId = downloadQueue.Id;
                    df.DownloadQueueName = downloadQueue.Title;
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
            await ShowErrorDialogAsync(ex);
        }
    }
}
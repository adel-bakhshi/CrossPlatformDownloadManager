using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddEditQueueWindowViewModel : ViewModelBase
{
    #region Private Fields

    private ObservableCollection<string> _tabItems = [];
    private string? _selectedTabItem;
    private DownloadQueueViewModel _downloadQueue = new();
    private OptionsViewModel? _optionsViewModel;
    private FilesViewModel? _filesViewModel;

    #endregion

    #region Properties

    public string Title => IsEditMode ? "CDM - Edit Queue" : "CDM - Add New Queue";
    public bool IsEditMode => DownloadQueue is { Id: > 0 };

    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
    }

    public DownloadQueueViewModel DownloadQueue
    {
        get => _downloadQueue;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadQueue, value);
            this.RaisePropertyChanged(nameof(IsEditMode));
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    public OptionsViewModel? OptionsViewModel
    {
        get => _optionsViewModel;
        set => this.RaiseAndSetIfChanged(ref _optionsViewModel, value);
    }

    public FilesViewModel? FilesViewModel
    {
        get => _filesViewModel;
        set => this.RaiseAndSetIfChanged(ref _filesViewModel, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public AddEditQueueWindowViewModel(IAppService appService, DownloadQueueViewModel? downloadQueue) : base(appService)
    {
        if (downloadQueue != null)
            DownloadQueue = downloadQueue;

        TabItems = ["Options", "Files"];
        SelectedTabItem = TabItems.FirstOrDefault();

        OptionsViewModel = new OptionsViewModel(appService, DownloadQueue);
        FilesViewModel = new FilesViewModel(appService, DownloadQueue);

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask<Window?>(DeleteAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null || OptionsViewModel == null || FilesViewModel == null)
                throw new InvalidOperationException("An error occured while trying to save queue.");

            if (DownloadQueue.IsRunning)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Warning", "Queue is running. Do you want to stop it?", DialogButtons.YesNo);
                if (result != DialogResult.Yes)
                    return;

                await AppService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(DownloadQueue);
            }

            if (OptionsViewModel.DownloadQueueTitle.IsNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please enter a title.", DialogButtons.Ok);
                return;
            }

            if (OptionsViewModel is { RetryOnDownloadingFailed: true, RetryCount: < 1 })
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Attention",
                    "Retry count must be greater than 0. Do you want to set it to 1?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                OptionsViewModel.RetryCount = 1;
            }
            else if (!OptionsViewModel.RetryOnDownloadingFailed)
            {
                OptionsViewModel.RetryCount = 0;
            }

            if (FilesViewModel.DownloadCountAtSameTime == 0)
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Attention",
                    "Download count at same time must be greater than 0. Do you want to set it to 1?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                FilesViewModel.DownloadCountAtSameTime = 1;
            }

            // Calculate start and end schedule times
            TimeSpan? startSchedule = null;
            TimeSpan? stopSchedule = null;
            
            if (OptionsViewModel.StartDownloadScheduleEnabled)
            {
                switch (OptionsViewModel)
                {
                    case { IsDaily: false }:
                    {
                        // if (DownloadQueue.JustForDate == null)
                        if (OptionsViewModel.SelectedDate == null)
                        {
                            await DialogBoxManager.ShowInfoDialogAsync("Start date", "When you choose 'Once', please select a date.", DialogButtons.Ok);
                            return;
                        }

                        if (OptionsViewModel.SelectedDate.Value.Date < DateTime.Now.Date)
                        {
                            await DialogBoxManager.ShowInfoDialogAsync("Start date", "You can't select a date in the past.", DialogButtons.Ok);
                            return;
                        }

                        break;
                    }

                    case { IsDaily: true, DaysOfWeekViewModel: not { IsAnyDaySelected: true } }:
                    {
                        await DialogBoxManager.ShowInfoDialogAsync("Attention", "When you choose 'Daily', please select at least one day.", DialogButtons.Ok);
                        return;
                    }
                }

                if (OptionsViewModel.StartDownloadHour == null || OptionsViewModel.StartDownloadMinute == null)
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please select a start time for your queue.", DialogButtons.Ok);
                    return;
                }

                if (OptionsViewModel.SelectedStartTimeOfDay.IsNullOrEmpty())
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please select a start time of day for your queue.", DialogButtons.Ok);
                    return;
                }

                var isAfternoon = OptionsViewModel.SelectedStartTimeOfDay!.Equals("PM");
                startSchedule = TimeSpan
                    .FromHours(OptionsViewModel.StartDownloadHour.Value + (isAfternoon ? 12 : 0))
                    .Add(TimeSpan.FromMinutes(OptionsViewModel.StartDownloadMinute.Value));
            }

            if (OptionsViewModel.StopDownloadScheduleEnabled)
            {
                if (OptionsViewModel.StopDownloadHour == null || OptionsViewModel.StopDownloadMinute == null)
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please select a stop time for your queue.", DialogButtons.Ok);
                    return;
                }

                if (OptionsViewModel.SelectedStopTimeOfDay.IsNullOrEmpty())
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please select a stop time of day for your queue.", DialogButtons.Ok);
                    return;
                }

                var isAfternoon = OptionsViewModel.SelectedStopTimeOfDay!.Equals("PM");
                stopSchedule = TimeSpan
                    .FromHours(OptionsViewModel.StopDownloadHour.Value + (isAfternoon ? 12 : 0))
                    .Add(TimeSpan.FromMinutes(OptionsViewModel.StopDownloadMinute.Value));
            }

            // Compare start and end schedule times
            if (startSchedule > stopSchedule)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Attention", "Start time must be before stop time.", DialogButtons.Ok);
                return;
            }

            TurnOffComputerMode? turnOffComputerMode = null;
            if (OptionsViewModel.TurnOffComputerWhenDone)
            {
                if (OptionsViewModel.SelectedTurnOffComputerMode.IsNullOrEmpty())
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Attention", "Please select a turn off computer mode for your queue.", DialogButtons.Ok);
                    return;
                }

                turnOffComputerMode = OptionsViewModel.SelectedTurnOffComputerMode switch
                {
                    "Shut down" => TurnOffComputerMode.Shutdown,
                    "Sleep" => TurnOffComputerMode.Sleep,
                    "Hibernate" => TurnOffComputerMode.Hibernate,
                    _ => turnOffComputerMode
                };
            }

            DownloadQueue.Title = OptionsViewModel.DownloadQueueTitle.Trim();
            DownloadQueue.IsDefault = OptionsViewModel.IsDownloadQueueDefault;
            DownloadQueue.StartOnApplicationStartup = OptionsViewModel.StartOnApplicationStartup;
            DownloadQueue.StartDownloadSchedule = startSchedule;
            DownloadQueue.StopDownloadSchedule = stopSchedule;
            DownloadQueue.RetryOnDownloadingFailed = OptionsViewModel.RetryOnDownloadingFailed;
            DownloadQueue.RetryCount = OptionsViewModel.RetryCount;
            DownloadQueue.ShowAlarmWhenDone = OptionsViewModel.ShowAlarmWhenDone;
            DownloadQueue.ExitProgramWhenDone = OptionsViewModel.ExitProgramWhenDone;
            DownloadQueue.TurnOffComputerWhenDone = OptionsViewModel.TurnOffComputerWhenDone;
            DownloadQueue.TurnOffComputerMode = turnOffComputerMode;
            DownloadQueue.IsDaily = OptionsViewModel.IsDaily;
            DownloadQueue.JustForDate = DownloadQueue.IsDaily ? null : OptionsViewModel.SelectedDate;
            DownloadQueue.DaysOfWeek = DownloadQueue.IsDaily ? OptionsViewModel.DaysOfWeekViewModel.ConvertToJson() : null;
            DownloadQueue.DownloadCountAtSameTime = FilesViewModel.DownloadCountAtSameTime;
            DownloadQueue.IncludePausedFiles = FilesViewModel.IncludePausedFiles;
            
            // Get all download queues that are default
            var defaultDownloadQueues = AppService
                .DownloadQueueService
                .DownloadQueues
                .Where(dq => dq.IsDefault && dq.Id != DownloadQueue.Id)
                .ToList();

            if (defaultDownloadQueues.Count > 0)
            {
                // Set all default download queues to not default
                foreach (var defaultDownloadQueue in defaultDownloadQueues)
                    defaultDownloadQueue.IsDefault = false;

                // Update default download queues
                await AppService
                    .DownloadQueueService
                    .UpdateDownloadQueuesAsync(defaultDownloadQueues);
            }

            var downloadQueue = AppService.Mapper.Map<DownloadQueue>(DownloadQueue);
            if (!IsEditMode)
            {
                DownloadQueue.Id = await AppService
                    .DownloadQueueService
                    .AddNewDownloadQueueAsync(downloadQueue);
            }
            else
            {
                await AppService
                    .DownloadQueueService
                    .UpdateDownloadQueueAsync(downloadQueue);
            }

            List<DownloadFileViewModel> stoppedDownloadFiles = [];
            if (IsEditMode)
            {
                var oldDownloadFiles = AppService
                    .DownloadFileService
                    .DownloadFiles
                    .Where(df => df.DownloadQueueId == DownloadQueue.Id)
                    .ToList();

                if (oldDownloadFiles.Exists(df => df.IsDownloading || df.IsPaused))
                {
                    foreach (var downloadFile in oldDownloadFiles.Where(df => df.IsDownloading || df.IsPaused).ToList())
                    {
                        await AppService
                            .DownloadFileService
                            .StopDownloadFileAsync(downloadFile, ensureStopped: true, playSound: false);
                        
                        stoppedDownloadFiles.Add(downloadFile);
                    }
                }
                
                await AppService
                    .DownloadQueueService
                    .RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueue, oldDownloadFiles);
            }

            await AppService
                .DownloadQueueService
                .AddDownloadFilesToDownloadQueueAsync(DownloadQueue, FilesViewModel.DownloadFiles.ToList());

            foreach (var downloadFile in stoppedDownloadFiles)
            {
                _ = AppService
                    .DownloadFileService
                    .StartDownloadFileAsync(downloadFile);
            }

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to save queue.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to cancel.");

            if (DownloadQueue.Id == 0)
                return;

            var downloadQueue = AppService
                .DownloadQueueService
                .DownloadQueues
                .FirstOrDefault(dq => dq.Id == DownloadQueue.Id);

            if (downloadQueue == null)
                return;

            var result = await DialogBoxManager.ShowWarningDialogAsync("Delete queue",
                $"You are about to remove '{downloadQueue.Title}'. Do you want to confirm this action?",
                DialogButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            if (downloadQueue.IsRunning)
            {
                await AppService
                    .DownloadQueueService
                    .StopDownloadQueueAsync(downloadQueue, playSound: false);
            }

            await AppService
                .DownloadQueueService
                .DeleteDownloadQueueAsync(downloadQueue);

            await CancelAsync(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to delete queue.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occured while trying to cancel.");

            owner.Close(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to cancel.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
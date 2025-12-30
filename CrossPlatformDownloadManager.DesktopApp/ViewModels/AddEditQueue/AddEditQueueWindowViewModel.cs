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
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueue.Views;
using CrossPlatformDownloadManager.DesktopApp.Views.AddEditQueue.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueue;

public class AddEditQueueWindowViewModel : ViewModelBase
{
    #region Private Fields

    private ObservableCollection<string> _tabItems = [];
    private string? _selectedTabItem;
    private DownloadQueueViewModel _downloadQueue = new();
    private OptionsView? _optionsView;
    private FilesView? _filesView;
    private bool _isApplicationDefaultQueue;

    #endregion

    #region Properties

    public string Title => IsEditMode ? "CDM - Edit Queue" : "CDM - Add New Queue";
    public bool IsEditMode => DownloadQueue is { Id: > 0 };
    public bool IsDeleteButtonEnabled => IsEditMode && !IsApplicationDefaultQueue;

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
            this.RaisePropertyChanged(nameof(IsDeleteButtonEnabled));
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    public OptionsView? OptionsView
    {
        get => _optionsView;
        set
        {
            this.RaiseAndSetIfChanged(ref _optionsView, value);
            this.RaisePropertyChanged(nameof(OptionsViewModel));
        }
    }

    public OptionsViewModel? OptionsViewModel => OptionsView?.DataContext as OptionsViewModel;

    public FilesView? FilesView
    {
        get => _filesView;
        set
        {
            this.RaiseAndSetIfChanged(ref _filesView, value);
            this.RaisePropertyChanged(nameof(FilesViewModel));
        }
    }

    public FilesViewModel? FilesViewModel => FilesView?.DataContext as FilesViewModel;

    public bool IsApplicationDefaultQueue
    {
        get => _isApplicationDefaultQueue;
        set
        {
            this.RaiseAndSetIfChanged(ref _isApplicationDefaultQueue, value);
            this.RaisePropertyChanged(nameof(IsDeleteButtonEnabled));
        }
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

        IsApplicationDefaultQueue = DownloadQueue.Title?.Equals(Constants.DefaultDownloadQueueTitle) == true;
        TabItems = ["Options", "Files"];
        SelectedTabItem = TabItems.FirstOrDefault();

        LoadViews();

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        DeleteCommand = ReactiveCommand.CreateFromTask<Window?>(DeleteAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private void LoadViews()
    {
        var optionsViewModel = new OptionsViewModel(AppService, DownloadQueue, IsApplicationDefaultQueue);
        OptionsView = new OptionsView { DataContext = optionsViewModel };

        var filesViewModel = new FilesViewModel(AppService, DownloadQueue);
        FilesView = new FilesView { DataContext = filesViewModel };
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null || OptionsViewModel == null || FilesViewModel == null)
                throw new InvalidOperationException("An error occurred while trying to save queue.");

            if (DownloadQueue.IsRunning)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Warning",
                    "The queue is currently running and needs to be stopped to save your changes. Would you like to stop it now?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                await AppService.DownloadQueueService.StopDownloadQueueAsync(DownloadQueue);
            }

            if (OptionsViewModel.DownloadQueueTitle.IsStringNullOrEmpty())
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

                if (OptionsViewModel.SelectedStartTimeOfDay.IsStringNullOrEmpty())
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

                if (OptionsViewModel.SelectedStopTimeOfDay.IsStringNullOrEmpty())
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
                if (OptionsViewModel.SelectedTurnOffComputerMode.IsStringNullOrEmpty())
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

            switch (OptionsViewModel.IsDownloadQueueDefault)
            {
                case true:
                {
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

                    // Set current download queue as default download queue
                    DownloadQueue.IsDefault = true;
                    break;
                }

                // Unset default download queue flag from current download queue
                case false when DownloadQueue.IsDefault:
                {
                    DownloadQueue.IsDefault = false;
                    break;
                }
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

            var downloadFiles = FilesViewModel.DownloadFiles.ToList();
            var stoppedDownloadFiles = new List<DownloadFileViewModel>();
            if (IsEditMode)
            {
                var oldDownloadFiles = AppService
                    .DownloadFileService
                    .DownloadFiles
                    .Where(df => df.DownloadQueueId == DownloadQueue.Id)
                    .ToList();

                var downloadingFiles = oldDownloadFiles.Where(df => df.IsDownloading || df.IsPaused).ToList();
                foreach (var downloadFile in downloadingFiles)
                {
                    await AppService
                        .DownloadFileService
                        .StopDownloadFileAsync(downloadFile, ensureStopped: true, playSound: false);

                    stoppedDownloadFiles.Add(downloadFile);
                }

                await AppService
                    .DownloadQueueService
                    .RemoveDownloadFilesFromDownloadQueueAsync(DownloadQueue, oldDownloadFiles);
            }

            await AppService
                .DownloadQueueService
                .AddDownloadFilesToDownloadQueueAsync(DownloadQueue, downloadFiles);

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
            Log.Error(ex, "An error occurred while trying to save queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to cancel.");

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
            Log.Error(ex, "An error occurred while trying to delete queue. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                throw new InvalidOperationException("An error occurred while trying to cancel.");

            owner.Close(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to cancel. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
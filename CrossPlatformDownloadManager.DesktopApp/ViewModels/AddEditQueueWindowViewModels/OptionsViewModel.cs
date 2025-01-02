using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

public class OptionsViewModel : ViewModelBase
{
    #region Private Fields

    private DownloadQueueViewModel _downloadQueue = new();
    private ObservableCollection<string> _startDownloadDateOptions = [];
    private string? _selectedStartDownloadDateOption;
    private ObservableCollection<string> _daysOfWeekOptions = [];
    private string? _selectedDaysOfWeekOption;
    private DateTime? _selectedDate;

    #endregion

    #region Properties

    public DownloadQueueViewModel DownloadQueue
    {
        get => _downloadQueue;
        set => this.RaiseAndSetIfChanged(ref _downloadQueue, value);
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

    public string? SelectedDaysOfWeekOption
    {
        get => _selectedDaysOfWeekOption;
        set => this.RaiseAndSetIfChanged(ref _selectedDaysOfWeekOption, value);
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedDate, value);
            DownloadQueue.JustForDate = value;
        }
    }

    #endregion

    #region Commands

    public ICommand SelectStartDownloadDateCommand { get; }

    public ICommand ChangeDefaultDownloadQueueCommand { get; }

    #endregion

    public OptionsViewModel(IAppService appService) : base(appService)
    {
        DownloadQueue = new DownloadQueueViewModel();
        StartDownloadDateOptions = ["Once", "Daily"];
        SelectedStartDownloadDateOption = StartDownloadDateOptions.FirstOrDefault();
        DaysOfWeekOptions = ["Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
        SelectedDate = DateTime.Now;

        SelectStartDownloadDateCommand = ReactiveCommand.CreateFromTask<CalendarDatePicker?>(SelectStartDownloadDateAsync);
        ChangeDefaultDownloadQueueCommand = ReactiveCommand.CreateFromTask(ChangeDefaultDownloadQueueAsync);
    }

    public void ChangeDaysOfWeek(List<string> selectedItems)
    {
        if (DownloadQueue.DaysOfWeekViewModel == null)
            throw new InvalidOperationException("An error occured while trying to change days of week.");

        DownloadQueue.DaysOfWeekViewModel.Saturday = selectedItems.Contains("Saturday");
        DownloadQueue.DaysOfWeekViewModel.Sunday = selectedItems.Contains("Sunday");
        DownloadQueue.DaysOfWeekViewModel.Monday = selectedItems.Contains("Monday");
        DownloadQueue.DaysOfWeekViewModel.Tuesday = selectedItems.Contains("Tuesday");
        DownloadQueue.DaysOfWeekViewModel.Wednesday = selectedItems.Contains("Wednesday");
        DownloadQueue.DaysOfWeekViewModel.Thursday = selectedItems.Contains("Thursday");
        DownloadQueue.DaysOfWeekViewModel.Friday = selectedItems.Contains("Friday");
    }

    #region Helpers

    private async Task SelectStartDownloadDateAsync(CalendarDatePicker? datePicker)
    {
        try
        {
            if (datePicker == null)
                return;

            datePicker.IsDropDownOpen = !datePicker.IsDropDownOpen;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task ChangeDefaultDownloadQueueAsync()
    {
        try
        {
            DownloadQueue.IsDefault = !DownloadQueue.IsDefault;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to change the default download queue.");
        }
    }

    private void ChangeStartDownloadDateOption()
    {
        DownloadQueue.IsDaily = SelectedStartDownloadDateOption?.Equals("Daily") ?? false;
    }

    #endregion
}
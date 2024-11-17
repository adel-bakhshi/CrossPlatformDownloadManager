using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

public class OptionsViewModel : ViewModelBase
{
    #region Private Fields

    private DownloadQueueViewModel _downloadQueue;
    private ObservableCollection<string> _startDownloadDateOptions;
    private string? _selectedStartDownloadDateOption;
    private ObservableCollection<string> _daysOfWeekOptions;
    private string? _selectedDaysOfWeekOption;
    private DateTime? _startDownloadDate;

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
        set => this.RaiseAndSetIfChanged(ref _selectedStartDownloadDateOption, value);
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

    public DateTime? StartDownloadDate
    {
        get => _startDownloadDate;
        set => this.RaiseAndSetIfChanged(ref _startDownloadDate, value);
    }

    #endregion

    #region Commands

    public ICommand ChangeStartDownloadDateCommand { get; }

    public ICommand SelectStartDownloadDateCommand { get; }

    #endregion

    public OptionsViewModel(IAppService appService) : base(appService)
    {
        DownloadQueue = new DownloadQueueViewModel();
        StartDownloadDateOptions = ["Once", "Daily"];
        SelectedStartDownloadDateOption = StartDownloadDateOptions.FirstOrDefault();
        DaysOfWeekOptions = ["Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];
        StartDownloadDate = DateTime.Now;

        ChangeStartDownloadDateCommand = ReactiveCommand.Create<string?>(ChangeStartDownloadDate);
        SelectStartDownloadDateCommand =
            ReactiveCommand.CreateFromTask<CalendarDatePicker?>(SelectStartDownloadDateAsync);
    }

    private void ChangeStartDownloadDate(string? value)
    {
        if (value.IsNullOrEmpty())
            return;

        DownloadQueue.IsDaily = value!.Equals("Daily");
    }

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
            await ShowErrorDialogAsync(ex);
        }
    }
}
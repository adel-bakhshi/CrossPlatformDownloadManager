using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.AddEditQueueWindowControls;

public partial class OptionsView : MyUserControlBase<OptionsViewModel>
{
    #region Private Fields

    private bool _changingDaysOfWeek = false;

    #endregion

    public OptionsView()
    {
        InitializeComponent();
    }

    private void StartDownloadScheduleControlOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var width = StartDownloadScheduleHour.Bounds.Width + StartDownloadScheduleColon.Bounds.Width +
                    StartDownloadScheduleMinute.Bounds.Width + StartDownloadScheduleTimeOfDay.Bounds.Width + 40;

        StartDownloadDatePickerBorder.MinWidth = width;
    }

    private async void DaysOfWeekSelectBoxOnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ViewModel == null || _changingDaysOfWeek)
                return;

            if (DaysOfWeekSelectBox.SelectedItems == null || DaysOfWeekSelectBox.SelectedItems.Count == 0)
            {
                _changingDaysOfWeek = false;
                return;
            }

            _changingDaysOfWeek = true;
            var removedItems = e.RemovedItems
                .OfType<string>()
                .ToList();

            var addedItems = e.AddedItems
                .OfType<string>()
                .Where(item => !removedItems.Remove(item))
                .ToList();

            removedItems.AddRange(addedItems);

            DaysOfWeekSelectBox.SelectedItems.Clear();
            foreach (var item in removedItems)
                DaysOfWeekSelectBox.SelectedItems.Add(item);

            ViewModel.ChangeDaysOfWeek(removedItems);
            _changingDaysOfWeek = false;
        }
        catch (Exception ex)
        {
            if (ViewModel == null)
            {
                Console.WriteLine(ex);
                return;
            }

            await ViewModel.ShowErrorDialogAsync(ex);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        LoadData();
    }

    private void LoadData()
    {
        // If ViewModel is null or is not edit mode, don't need to load data and return
        if (ViewModel == null || ViewModel.DownloadQueue.Id == 0)
            return;

        // Set selected start download date option
        ViewModel.SelectedStartDownloadDateOption = ViewModel.DownloadQueue.IsDaily ? "Daily" : "Once";
        if (!ViewModel.DownloadQueue.IsDaily)
            ViewModel.SelectedDate = ViewModel.DownloadQueue.JustForDate;

        // Make sure DaysOfWeekViewModel is not null
        if (ViewModel.DownloadQueue.DaysOfWeekViewModel == null)
            return;

        // Create days of week list
        var daysOfWeek = new List<string>();

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Saturday)
            daysOfWeek.Add("Saturday");

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Sunday)
            daysOfWeek.Add("Sunday");

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Monday)
            daysOfWeek.Add("Monday");

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Tuesday)
            daysOfWeek.Add("Tuesday");

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Wednesday)
            daysOfWeek.Add("Wednesday");

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Thursday)
            daysOfWeek.Add("Thursday");

        if (ViewModel.DownloadQueue.DaysOfWeekViewModel.Friday)
            daysOfWeek.Add("Friday");
        
        // Set selected days of week
        _changingDaysOfWeek = true;
        DaysOfWeekSelectBox.SelectedItems?.Clear();
        foreach (var day in daysOfWeek)
            DaysOfWeekSelectBox.SelectedItems?.Add(day);
        
        _changingDaysOfWeek = false;
    }
}
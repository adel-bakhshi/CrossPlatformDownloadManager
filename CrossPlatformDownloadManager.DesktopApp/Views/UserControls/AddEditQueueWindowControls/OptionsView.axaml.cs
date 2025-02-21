using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.AddEditQueueWindowControls;

public partial class OptionsView : MyUserControlBase<OptionsViewModel>
{
    #region Private Fields

    private bool _changingDaysOfWeek;

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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to change days of week. Error message: {ErrorMessage}", ex.Message);
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
        if (ViewModel == null || ViewModel.DaysOfWeek.Count == 0)
            return;

        // Set selected days of week
        _changingDaysOfWeek = true;
        DaysOfWeekSelectBox.SelectedItems ??= new List<string>();
        DaysOfWeekSelectBox.SelectedItems?.Clear();
        foreach (var day in ViewModel.DaysOfWeek)
            DaysOfWeekSelectBox.SelectedItems?.Add(day);

        _changingDaysOfWeek = false;
    }
}
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.AddEditQueueWindowControls;

public partial class OptionsView : MyUserControlBase<FilesViewModel>
{
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
}
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class PowerOffWindow : MyWindowBase<PowerOffWindowViewModel>
{
    public PowerOffWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        ViewModel?.StopReverseTimer();
        base.OnClosing(e);
    }
}
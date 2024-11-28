using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddEditQueueWindow : MyWindowBase<AddEditQueueWindowViewModel>
{
    public AddEditQueueWindow()
    {
        InitializeComponent();
    }
}
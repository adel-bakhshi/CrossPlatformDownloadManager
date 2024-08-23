using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddNewQueueWindow : Window
{
    public AddNewQueueWindow()
    {
        InitializeComponent();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close(false);
    }
}
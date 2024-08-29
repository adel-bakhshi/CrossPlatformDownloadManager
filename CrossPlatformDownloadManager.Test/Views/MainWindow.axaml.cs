using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Downloader;

namespace CrossPlatformDownloadManager.Test.Views;

public partial class MainWindow : Window
{
    private DownloadService _downloadService;
    private bool _isPaused;
    
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void StartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await DownloadFile().ConfigureAwait(false);
    }

    private async Task DownloadFile()
    {
        var downloadOptions = new DownloadConfiguration
        {
            MaximumBytesPerSecond = 64 * 1024,
        };

        _downloadService = new DownloadService(downloadOptions);
        _downloadService.DownloadProgressChanged += DownloadServiceOnDownloadProgressChanged;

        var fileName = "C:\\Users\\adelb\\Desktop\\VideoPad.Video.Editor.Pro.16.34.rar";
        var url = "https://dl2.soft98.ir/soft/u-v/VideoPad.Video.Editor.Pro.16.34.rar?1724932721";
        await _downloadService.DownloadFileTaskAsync(url, fileName).ConfigureAwait(false);
    }

    private void DownloadServiceOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => ProgressTextBlock.Text = e.ProgressPercentage.ToString("00.00"));
    }

    private async void StopButton_OnClick(object? sender, RoutedEventArgs e)
    {
        await _downloadService.CancelTaskAsync();
        ProgressTextBlock.Text = "Stopped";
    }
}
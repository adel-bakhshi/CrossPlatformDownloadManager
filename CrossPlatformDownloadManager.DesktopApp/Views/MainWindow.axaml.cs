using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly DownloadWindowViewModel _downloadWindowViewModel;

    public MainWindow(MainWindowViewModel viewModel, DownloadWindowViewModel downloadWindowViewModel)
    {
        _viewModel = viewModel;
        _downloadWindowViewModel = downloadWindowViewModel;
        InitializeComponent();
        
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var window = new DownloadWindow
        {
            DataContext = _downloadWindowViewModel,
        };
        
        window.Show();
    }
}
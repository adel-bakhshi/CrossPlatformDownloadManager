using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var serviceProvider = this.GetServiceProvider();
        var vm = serviceProvider.GetService<AddDownloadLinkWindowViewModel>();
        var window = new AddDownloadLinkWindow
        {
            DataContext = vm,
        };
        
        window.Show();
    }
}
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Downloader;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.ViewModels;

public class DownloadFileTaskViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _key;
    private DownloadConfiguration? _configuration;
    private DownloadService? _service;
    private bool _stopOperationFinished;
    private bool _stopping;

    #endregion

    #region Properties

    public int Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    public DownloadConfiguration? Configuration
    {
        get => _configuration;
        set => SetField(ref _configuration, value);
    }

    public DownloadService? Service
    {
        get => _service;
        set => SetField(ref _service, value);
    }

    public bool StopOperationFinished
    {
        get => _stopOperationFinished;
        set => SetField(ref _stopOperationFinished, value);
    }

    public bool Stopping
    {
        get => _stopping;
        set => SetField(ref _stopping, value);
    }

    public DownloadWindow? DownloadWindow { get; private set; }

    #endregion

    public void CreateDownloadWindow(DownloadFileViewModel? downloadFile, bool showWindow = true)
    {
        if (downloadFile == null)
            return;

        var serviceProvider = Application.Current?.GetServiceProvider();
        var appService = serviceProvider?.GetService<IAppService>();
        if (appService == null)
            throw new InvalidOperationException("App service not found.");

        var viewModel = new DownloadWindowViewModel(appService, downloadFile);
        DownloadWindow = new DownloadWindow { DataContext = viewModel };
        DownloadWindow.Closing += DownloadWindowOnClosing;
        
        if (showWindow)
            Dispatcher.UIThread.Post(() => DownloadWindow.Show());
    }

    private async void DownloadWindowOnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            if (DownloadWindow is not { DataContext: DownloadWindowViewModel viewModel })
                return;
            
            DownloadWindow.Closing -= DownloadWindowOnClosing;
            DownloadWindow.StopUpdateChunksDataTimer();
            viewModel.RemoveEventHandlers();

            if (viewModel.DownloadFile.IsDownloading || viewModel.DownloadFile.IsPaused)
                await viewModel.StopDownloadAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while closing download window.");

            await DialogBoxManager.ShowDangerDialogAsync("Error closing download window",
                $"An error occurred while closing download window.\nError message: {ex.Message}",
                DialogButtons.Ok);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class DownloadWindow : Window
{
    #region Private Fields

    private readonly List<Rectangle> _chunksDataRectangles;
    private readonly DispatcherTimer _updateChunksDataTimer;

    private readonly bool _minimizeWindow;
    private readonly bool _hideWindow;

    #endregion

    public DownloadWindow(bool minimizeWindow, bool hideWindow)
    {
        InitializeComponent();

        _minimizeWindow = minimizeWindow;
        _hideWindow = hideWindow;

        _chunksDataRectangles = [];
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateChunksDataTimer.Tick += UpdateChunksDataTimerOnTick;
    }

    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs e)
    {
        var vm = DataContext as DownloadWindowViewModel;
        if (vm == null || vm.IsPaused)
            return;

        var chunksData = vm.DownloadFile.ChunksData;
        var bounds = ChunksProgressBarsCanvas.Bounds;
        var chunksCount = chunksData.Count;
        var divisionsWidth = bounds.Width / chunksCount;

        if (!_chunksDataRectangles.Any())
        {
            var heightBinding = new Binding
            {
                Path = "Bounds.Height",
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                {
                    AncestorType = typeof(Border),
                },
            };

            for (int i = 0; i < chunksCount; i++)
            {
                var rect = new Rectangle();
                rect.Bind(Rectangle.HeightProperty, heightBinding);
                rect.Fill = this.FindResource("ChunkProgressGradientBrush") as IBrush;
                rect.Width = chunksData[i].TotalSize == 0
                    ? 0
                    : chunksData[i].DownloadedSize * divisionsWidth / chunksData[i].TotalSize;

                Canvas.SetLeft(rect, divisionsWidth * i);
                Canvas.SetTop(rect, 0);

                ChunksProgressBarsCanvas.Children.Add(rect);
                _chunksDataRectangles.Add(rect);
            }
        }

        for (int i = 0; i < chunksCount; i++)
        {
            _chunksDataRectangles[i].Width = chunksData[i].TotalSize == 0
                ? 0
                : chunksData[i].DownloadedSize * divisionsWidth / chunksData[i].TotalSize;
        }
    }

    private void DownloadSpeedLimiterView_OnSpeedLimiterStateChanged(object? sender,
        DownloadSpeedLimiterViewEventArgs e)
    {
        var vm = DataContext as DownloadWindowViewModel;
        if (vm == null)
            return;

        vm.ChangeSpeedLimiterState(e);
    }

    private void DownloadOptionsView_OnOptionsStateChanged(object? sender, DownloadOptionsViewEventArgs e)
    {
        var vm = DataContext as DownloadWindowViewModel;
        if (vm == null)
            return;

        vm.ChangeOptions(e);
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        // TODO: Show message box
        try
        {
            var vm = DataContext as DownloadWindowViewModel;
            if (vm == null)
                return;

            _updateChunksDataTimer.Start();
            await vm.StartDownloadAsync(window: this, minimizeDownloadWindow: _minimizeWindow,
                hideDownloadWindow: _hideWindow);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
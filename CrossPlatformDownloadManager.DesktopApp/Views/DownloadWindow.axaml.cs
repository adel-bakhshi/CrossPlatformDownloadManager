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
    private readonly DispatcherTimer _timer;

    #endregion

    public DownloadWindow()
    {
        InitializeComponent();

        _chunksDataRectangles = [];
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += UpdateChunksDataTimerOnTick;
    }

    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs e)
    {
        var vm = DataContext as DownloadWindowViewModel;
        if (vm == null || vm.IsPaused)
            return;

        var bounds = ChunksProgressBarsCanvas.Bounds;
        var chunksCount = vm.ChunksData.Count;
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
                rect.Width = vm.ChunksData[i].TotalSize == 0
                    ? 0
                    : vm.ChunksData[i].DownloadedSize * divisionsWidth / vm.ChunksData[i].TotalSize;

                Canvas.SetLeft(rect, divisionsWidth * i);
                Canvas.SetTop(rect, 0);

                ChunksProgressBarsCanvas.Children.Add(rect);
                _chunksDataRectangles.Add(rect);
            }
        }

        for (int i = 0; i < chunksCount; i++)
        {
            _chunksDataRectangles[i].Width = vm.ChunksData[i].TotalSize == 0
                ? 0
                : vm.ChunksData[i].DownloadedSize * divisionsWidth / vm.ChunksData[i].TotalSize;
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
        var vm = DataContext as DownloadWindowViewModel;
        if (vm == null)
            return;

        _timer.Start();
        await vm.StartDownloadAsync().ConfigureAwait(false);
    }
}
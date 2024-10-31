using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class DownloadWindow : MyWindowBase<DownloadWindowViewModel>
{
    #region Private Fields

    // Update chunks data
    private readonly List<Rectangle> _chunksDataRectangles;
    private readonly DispatcherTimer _updateChunksDataTimer;

    // Is window closing flag
    private bool _isClosing;

    #endregion

    public DownloadWindow()
    {
        InitializeComponent();

        _chunksDataRectangles = [];
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateChunksDataTimer.Tick += UpdateChunksDataTimerOnTick;
    }

    private void DownloadSpeedLimiterView_OnSpeedLimiterStateChanged(object? sender,
        DownloadSpeedLimiterViewEventArgs e)
    {
        ViewModel?.ChangeSpeedLimiterState(e);
    }

    private void DownloadOptionsView_OnOptionsStateChanged(object? sender, DownloadOptionsViewEventArgs e)
    {
        ViewModel?.ChangeOptions(e);
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        if (ViewModel == null)
            return;

        ViewModel.DownloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
        _updateChunksDataTimer.Start();

        // Wait for 0.5s to focus the window
        await Task.Delay(500);
        Focus();
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (ViewModel == null)
            return;

        _isClosing = true;
        ViewModel.DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;

        if (ViewModel.DownloadFile.IsDownloading)
            await ViewModel.StopDownloadAsync(this, closeWindow: false);

        base.OnClosing(e);
    }

    #region Helpers

    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs e)
    {
        if (ViewModel == null)
            return;

        var chunksData = ViewModel.DownloadFile.ChunksData;
        var bounds = ChunksProgressBarsCanvas.Bounds;
        var chunksCount = chunksData.Count;
        var divisionsWidth = bounds.Width / chunksCount;

        if (_chunksDataRectangles.Count == 0)
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

            for (var i = 0; i < chunksCount; i++)
            {
                var rect = new Rectangle();
                rect.Bind(Rectangle.HeightProperty, heightBinding);
                rect.Fill = this.FindResource("ChunkProgressGradientBrush") as IBrush;

                Canvas.SetLeft(rect, divisionsWidth * i);
                Canvas.SetTop(rect, 0);

                ChunksProgressBarsCanvas.Children.Add(rect);
                _chunksDataRectangles.Add(rect);
            }
        }

        for (var i = 0; i < chunksCount; i++)
        {
            _chunksDataRectangles[i].Width = chunksData[i].TotalSize == 0
                ? 0
                : chunksData[i].DownloadedSize * divisionsWidth / chunksData[i].TotalSize;
        }
    }

    private void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ViewModel == null)
                return;

            ViewModel.DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
            if (!_isClosing)
                Close();
        });
    }

    #endregion
}
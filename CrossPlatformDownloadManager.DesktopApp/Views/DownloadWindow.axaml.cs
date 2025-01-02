using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class DownloadWindow : MyWindowBase<DownloadWindowViewModel>
{
    #region Private Fields

    // Update chunks data
    private readonly List<Rectangle> _chunksDataRectangles;
    private readonly DispatcherTimer _updateChunksDataTimer;

    // Focus timer
    private readonly DispatcherTimer _focusTimer;

    // Is window closing flag
    private bool _isClosing;

    #endregion

    public DownloadWindow()
    {
        InitializeComponent();

        _chunksDataRectangles = [];
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _updateChunksDataTimer.Tick += UpdateChunksDataTimerOnTick;

        _focusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
        _focusTimer.Tick += FocusTimerOnTick;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            ViewModel.DownloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
            ViewModel.DownloadFile.DownloadStopped += DownloadFileOnDownloadStopped;
            _updateChunksDataTimer.Start();

            // Start focus window
            _focusTimer.Start();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured during loading download window.");
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            _isClosing = true;
            ViewModel.DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
            ViewModel.DownloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;

            if (ViewModel.DownloadFile.IsDownloading)
                await ViewModel.StopDownloadAsync(this);

            ViewModel.RemoveEventHandlers();
            _updateChunksDataTimer.Stop();
            base.OnClosing(e);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured during closing download window.");
        }
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
            _chunksDataRectangles[i].Width = chunksData[i].TotalSize == 0 ? 0 : chunksData[i].DownloadedSize * divisionsWidth / chunksData[i].TotalSize;
    }

    private void FocusTimerOnTick(object? sender, EventArgs e)
    {
        if (!IsFocused)
        {
            Focus();
            return;
        }

        _focusTimer.Stop();
    }

    private void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        _ = Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                if (ViewModel == null)
                    return;

                ViewModel.DownloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;
                ViewModel.DownloadFile.DownloadStopped -= DownloadFileOnDownloadStopped;
                ViewModel.RemoveEventHandlers();

                if (!_isClosing)
                    Close();
            }
            catch (Exception ex)
            {
                await DialogBoxManager.ShowErrorDialogAsync(ex);
                Log.Error(ex, "An error occured while completing the file download.");
            }
        });
    }

    private void DownloadFileOnDownloadStopped(object? sender, DownloadFileEventArgs e)
    {
        Hide();
    }

    #endregion
}
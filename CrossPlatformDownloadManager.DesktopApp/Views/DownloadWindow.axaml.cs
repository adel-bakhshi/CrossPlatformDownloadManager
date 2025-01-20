using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
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
    private readonly DispatcherTimer _focusWindowTimer;

    #endregion

    public DownloadWindow()
    {
        InitializeComponent();

        _chunksDataRectangles = [];
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.25) };
        _updateChunksDataTimer.Tick += UpdateChunksDataTimerOnTick;

        _focusWindowTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _focusWindowTimer.Tick += FocusWindowTimerOnTick;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;
            
            // Start update chunks data timer
            _updateChunksDataTimer.Start();
            // Start focus window timer
            _focusWindowTimer.Start();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured during loading download window. Error message: {ErrorMessage}", ex.Message);
        }
    }
    
    public void StopUpdateChunksDataTimer() => _updateChunksDataTimer.Stop();

    #region Helpers

    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs e)
    {
        if (ViewModel == null || !IsVisible)
            return;

        Title = $"CDM - {(ViewModel.IsPaused ? "Paused" : "Downloading")} {Math.Floor(ViewModel.DownloadFile.DownloadProgress ?? 0):00}%";

        if (ViewModel.IsPaused)
            return;

        var chunksData = ViewModel.DownloadFile.ChunksData;
        var bounds = ChunksProgressBarsCanvas.Bounds;
        var divisionsWidth = bounds.Width / chunksData.Count;

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

            for (var i = 0; i < chunksData.Count; i++)
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

        for (var i = 0; i < chunksData.Count; i++)
            _chunksDataRectangles[i].Width = chunksData[i].TotalSize == 0 ? 0 : chunksData[i].DownloadedSize * divisionsWidth / chunksData[i].TotalSize;
    }

    private void FocusWindowTimerOnTick(object? sender, EventArgs e)
    {
        if (!IsFocused)
        {
            Focus();
            return;
        }

        _focusWindowTimer.Stop();
    }

    #endregion
}
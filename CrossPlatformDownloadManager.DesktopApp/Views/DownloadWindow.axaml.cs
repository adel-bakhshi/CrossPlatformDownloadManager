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
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.2) };
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

            // Create views for the download window
            await ViewModel.CreateViewsAsync();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred during loading download window. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public void StopUpdateChunksDataTimer() => _updateChunksDataTimer.Stop();

    #region Helpers

    /// <summary>
    /// Handles the timer tick event for updating chunks data visualization.
    /// This method updates the progress bars for each chunk of the file being downloaded.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The event arguments.</param>
    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs e)
    {
        // Return if ViewModel is null, paused, or the control is not visible
        if (ViewModel == null || ViewModel.IsPaused || !IsVisible)
            return;

        // Get chunks data and calculate the width division for each chunk
        var chunksData = ViewModel.DownloadFile.ChunksData;
        var bounds = ChunksProgressBarsCanvas.Bounds;
        var divisionsWidth = bounds.Width / chunksData.Count;

        // Initialize rectangles if they haven't been created yet
        if (_chunksDataRectangles.Count == 0)
        {
            // Create a binding for the height property
            var heightBinding = new Binding
            {
                Path = "Bounds.Height",
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                {
                    AncestorType = typeof(Border),
                },
            };

            // Create a rectangle for each chunk
            for (var i = 0; i < chunksData.Count; i++)
            {
                var rect = new Rectangle();
                rect.Bind(Rectangle.HeightProperty, heightBinding);
                rect.Fill = this.FindResource("ChunkProgressColor") as IBrush;

                // Position the rectangle on the canvas
                Canvas.SetLeft(rect, divisionsWidth * i);
                Canvas.SetTop(rect, 0);

                // Add rectangle to canvas and collection
                ChunksProgressBarsCanvas.Children.Add(rect);
                _chunksDataRectangles.Add(rect);
            }
        }

        // Update the width of each rectangle based on download progress
        for (var i = 0; i < chunksData.Count; i++)
            _chunksDataRectangles[i].Width = chunksData[i].TotalSize == 0 ? 0 : chunksData[i].DownloadedSize * divisionsWidth / chunksData[i].TotalSize;
    }

    /// <summary>
    /// Handles the Tick event for the FocusWindowTimer.
    /// This method is called each time the timer interval elapses.
    /// </summary>
    /// <param name="sender">The source of the event, typically the timer that triggered the event.</param>
    /// <param name="e">Event arguments that contain no data.</param>
    private void FocusWindowTimerOnTick(object? sender, EventArgs e)
    {
        // Check if the current window is not focused
        if (!IsFocused)
        {
            // If not focused, bring the window to the foreground
            Focus();
            return;
        }

        // If the window is already focused, stop the timer
        _focusWindowTimer.Stop();
    }

    #endregion
}
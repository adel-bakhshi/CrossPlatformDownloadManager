using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class DownloadWindow : Window
{
    #region Private Fields

    private readonly DownloadWindowViewModel _viewModel;

    #endregion

    public DownloadWindow(DownloadWindowViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();
        this.DataContext = _viewModel;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = ChunksProgressBarsCanvas.Bounds;
        var chunksCount = _viewModel.ChunksData.Count;
        var divisionsWidth = bounds.Width / chunksCount;

        var heightBinding = new Binding
        {
            Path = "Height",
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
            rect.Width = _viewModel.ChunksData[i].DownloadedSize / _viewModel.ChunksData[i].TotalSize * divisionsWidth;

            Canvas.SetLeft(rect, divisionsWidth * i);
            Canvas.SetTop(rect, 0);

            Dispatcher.UIThread.Post(() => ChunksProgressBarsCanvas.Children.Add(rect));
        }
    }
}
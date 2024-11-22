using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class TrayMenuWindow : MyWindowBase<TrayMenuWindowViewModel>
{
    #region Properties

    public Window? OwnerWindow { get; set; }

    #endregion

    public TrayMenuWindow(TrayMenuWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        SizeChanged += WindowOnSizeChanged;
    }

    private void WindowOnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        ChangeWindowPosition();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (ViewModel == null)
            return;

        var scrollViewer = this.FindControl<ScrollViewer>("MainScrollViewer");
        if (scrollViewer != null)
        {
            Height = scrollViewer.Extent.Height;

            if (OwnerWindow != null)
                OwnerWindow.PositionChanged += OwnerWindowOnPositionChanged;

            ChangeWindowPosition();
        }

        ViewModel.TrayMenuWindow = this;
    }

    private void OwnerWindowOnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        ChangeWindowPosition();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        ChangeWindowPosition();
    }

    #region Helpers

    private void ChangeWindowPosition()
    {
        if (OwnerWindow == null || Screens.Primary == null)
            return;

        var ownerWindowX = OwnerWindow.Position.X;
        var ownerWindowY = OwnerWindow.Position.Y;

        var widthDiff = Bounds.Width - OwnerWindow.Bounds.Width;

        int x, y;
        if (ownerWindowX - widthDiff / 2 < 0)
            x = 0;
        else if (ownerWindowX - widthDiff / 2 + Bounds.Width > Screens.Primary.Bounds.Width)
            x = (int)(Screens.Primary.Bounds.Width - Bounds.Width);
        else
            x = (int)(ownerWindowX - widthDiff / 2);

        Position = Position.WithX(x);

        if (ownerWindowY - Bounds.Height - 5 < 0)
            y = (int)(ownerWindowY + OwnerWindow.Bounds.Height + 5);
        else
            y = (int)(ownerWindowY - Bounds.Height - 5);

        Position = Position.WithY(y);
    }

    #endregion
}
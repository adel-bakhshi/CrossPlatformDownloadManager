using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class FileTypesView : MyUserControlBase<FileTypesViewModel>
{
    #region Properties

    public static readonly StyledProperty<double> DataGridHeightProperty =
        AvaloniaProperty.Register<FileTypesView, double>(name: nameof(DataGridHeight), defaultValue: double.NaN);

    public double DataGridHeight
    {
        get => GetValue(DataGridHeightProperty);
        set => SetValue(DataGridHeightProperty, value);
    }

    #endregion

    public FileTypesView()
    {
        InitializeComponent();
    }
}
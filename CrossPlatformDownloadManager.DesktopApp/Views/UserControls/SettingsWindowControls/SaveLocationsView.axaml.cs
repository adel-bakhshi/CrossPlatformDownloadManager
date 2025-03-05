using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class SaveLocationsView : MyUserControlBase<SaveLocationsViewModel>
{
    public SaveLocationsView()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != IsVisibleProperty || !IsVisible || ViewModel == null)
            return;

        ViewModel.LoadFileExtensions();
    }
}
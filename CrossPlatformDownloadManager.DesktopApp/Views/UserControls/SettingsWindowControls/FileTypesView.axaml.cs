using System;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;
using Serilog;

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

    protected override async void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        try
        {
            base.OnPropertyChanged(change);

            if (change.Property != IsVisibleProperty || !IsVisible || ViewModel == null)
                return;

            await ViewModel.LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            if (ViewModel != null)
                await ViewModel.ShowErrorDialogAsync(ex);

            Log.Error(ex, "An error occured while trying to load file extensions.");
        }
    }
}
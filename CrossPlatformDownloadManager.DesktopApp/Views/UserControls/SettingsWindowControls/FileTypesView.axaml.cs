using System;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
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

            ViewModel.LoadFileExtensions();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to load file extensions. Error message: {ErrorMessage}", ex.Message);
        }
    }
}
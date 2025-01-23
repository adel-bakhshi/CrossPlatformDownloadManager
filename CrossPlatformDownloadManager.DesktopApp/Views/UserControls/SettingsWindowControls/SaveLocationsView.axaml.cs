using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;
using Serilog;

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

    private async void BrowseButtonOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel?.SelectedCategory?.CategorySaveDirectory == null)
                return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
                return;

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false,
            };

            var directories = await topLevel.StorageProvider.OpenFolderPickerAsync(options);
            if (!directories.Any())
                return;

            var directory = directories[0];
            ViewModel.SelectedCategory.CategorySaveDirectory.SaveDirectory = directory.Path.LocalPath;
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to select directory for a category. Error message: {ErrorMessage}", ex.Message);
        }
    }
}
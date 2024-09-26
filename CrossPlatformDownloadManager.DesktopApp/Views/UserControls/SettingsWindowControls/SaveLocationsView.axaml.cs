using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

        if (!change.Property.Name.Equals("IsVisible"))
            return;

        if (!IsVisible || ViewModel == null)
            return;

        ViewModel.LoadFileExtensions();
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        // TODO: Show message box
        try
        {
            if (ViewModel?.SelectedCategory == null)
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
            ViewModel.SelectedCategory.CategorySaveDirectory = directory.Path.LocalPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
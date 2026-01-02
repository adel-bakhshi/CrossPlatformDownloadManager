using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class AddEditCategoryWindow : MyWindowBase<AddEditCategoryWindowViewModel>
{
    public AddEditCategoryWindow()
    {
        InitializeComponent();
    }

    private async void BrowseButtonOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (ViewModel == null)
                return;

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false
            };

            var directories = await StorageProvider.OpenFolderPickerAsync(options);
            if (!directories.Any())
                return;

            var directory = directories[0];
            ViewModel.SaveDirectory = directory.Path.IsAbsoluteUri ? directory.Path.LocalPath : directory.Path.OriginalString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to select directory. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void ExtensionTextBlockOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        ViewModel?.RaisePropertyChanged(nameof(ViewModel.IsDeleteClearFileExtensionButtonEnabled));
        ViewModel?.RaisePropertyChanged(nameof(ViewModel.IsSaveFileExtensionButtonEnabled));
    }

    private void AlisTextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
    {
        ViewModel?.RaisePropertyChanged(nameof(ViewModel.IsDeleteClearFileExtensionButtonEnabled));
        ViewModel?.RaisePropertyChanged(nameof(ViewModel.IsSaveFileExtensionButtonEnabled));
    }
}
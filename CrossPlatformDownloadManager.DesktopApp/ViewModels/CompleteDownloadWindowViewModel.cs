using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class CompleteDownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    private bool _dontShowThisDialogAgain;
    private DownloadFileViewModel _downloadFile;

    #endregion

    #region Properties

    public bool DontShowThisDialogAgain
    {
        get => _dontShowThisDialogAgain;
        set
        {
            this.RaiseAndSetIfChanged(ref _dontShowThisDialogAgain, value);
            _ = SaveDontShowThisDialogAgainOptionAsync();
        }
    }

    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set => this.RaiseAndSetIfChanged(ref _downloadFile, value);
    }

    #endregion

    #region Commands

    public ICommand OpenFileCommand { get; }

    public ICommand OpenFolderCommand { get; }

    public ICommand CloseCommand { get; }

    #endregion

    public CompleteDownloadWindowViewModel(IAppService appService, DownloadFileViewModel downloadFile) : base(appService)
    {
        _downloadFile = downloadFile;

        OpenFileCommand = ReactiveCommand.CreateFromTask<Window?>(OpenFileAsync);
        OpenFolderCommand = ReactiveCommand.CreateFromTask<Window?>(OpenFolderAsync);
        CloseCommand = ReactiveCommand.CreateFromTask<Window?>(CloseAsync);
    }

    private async Task OpenFileAsync(Window? owner)
    {
        try
        {
            if (DownloadFile.SaveLocation.IsNullOrEmpty() || DownloadFile.FileName.IsNullOrEmpty())
            {
                await DialogBoxManager.ShowDangerDialogAsync("Open file", "File not found.", DialogButtons.Ok);
                return;
            }

            var filePath = Path.Combine(DownloadFile.SaveLocation!, DownloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open file", "File not found.", DialogButtons.Ok);
                return;
            }

            PlatformSpecificManager.OpenFile(filePath);
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open the file. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task OpenFolderAsync(Window? owner)
    {
        try
        {
            if (DownloadFile.SaveLocation.IsNullOrEmpty()
                || !Directory.Exists(DownloadFile.SaveLocation!)
                || DownloadFile.FileName.IsNullOrEmpty())
            {
                await DialogBoxManager.ShowDangerDialogAsync("Open folder", "Folder not found.", DialogButtons.Ok);
                return;
            }

            var filePath = Path.Combine(DownloadFile.SaveLocation!, DownloadFile.FileName!);
            if (!File.Exists(filePath))
            {
                await DialogBoxManager.ShowDangerDialogAsync("Open folder", "File not found.", DialogButtons.Ok);
                return;
            }

            PlatformSpecificManager.OpenContainingFolderAndSelectFile(filePath);
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open the folder. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task CloseAsync(Window? owner)
    {
        try
        {
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task SaveDontShowThisDialogAgainOptionAsync()
    {
        try
        {
            AppService.SettingsService.Settings.ShowCompleteDownloadDialog = !DontShowThisDialogAgain;
            await AppService.SettingsService.SaveSettingsAsync(AppService.SettingsService.Settings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to save the dont show this dialog again option. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
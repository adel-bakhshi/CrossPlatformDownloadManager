using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class RefreshDownloadAddressWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly DownloadFileViewModel? _downloadFile;

    private string _currentAddress = string.Empty;
    private string _newAddress = string.Empty;

    #endregion

    #region Properties
    
    public string CurrentAddress
    {
        get => _currentAddress;
        set => this.RaiseAndSetIfChanged(ref _currentAddress, value);
    }

    public string NewAddress
    {
        get => _newAddress;
        set => this.RaiseAndSetIfChanged(ref _newAddress, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public RefreshDownloadAddressWindowViewModel(IAppService appService, DownloadFileViewModel? downloadFile) : base(appService)
    {
        _downloadFile = downloadFile;

        CurrentAddress = _downloadFile?.Url ?? string.Empty;

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            NewAddress = NewAddress.Replace("\\", "/").Trim();
            if (owner == null || NewAddress.IsNullOrEmpty() || !NewAddress.CheckUrlValidation())
                return;

            // Check if the new address is the same as the current address
            if (CurrentAddress.Equals(NewAddress))
            {
                owner.Close();
                return;
            }

            var downloadFile = AppService
                .DownloadFileService
                .DownloadFiles
                .FirstOrDefault(df => df.Id == _downloadFile?.Id);

            if (downloadFile == null)
                throw new InvalidOperationException("Download file not found.");

            if (downloadFile.IsCompleted)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Download completed",
                    "Download complete. To download with a new address, please delete the existing file and add a new download task.",
                    DialogButtons.Ok);

                return;
            }

            var restartDownloadFile = false;
            if (downloadFile.IsDownloading || downloadFile.IsPaused)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Download in progress",
                    "The selected download is currently in progress. Changing the address will stop the current download and start a new one. Are you sure you want to proceed?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                await AppService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile, ensureStopped: true);

                restartDownloadFile = true;
            }

            // Get url details
            var urlDetails = await AppService.DownloadFileService.GetUrlDetailsAsync(NewAddress, CancellationToken.None);
            if (urlDetails == null)
                throw new InvalidOperationException("Failed to retrieve URL details.");

            var urlDetailsValidation = AppService.DownloadFileService.ValidateUrlDetails(urlDetails);
            if (!urlDetailsValidation.IsValid)
            {
                await DialogBoxManager.ShowDangerDialogAsync(urlDetailsValidation.Title!, urlDetailsValidation.Message!, DialogButtons.Ok);
                return;
            }

            // Compare file size
            var newFileSize = (long)urlDetails.FileSize;
            var oldFileSize = (long)(downloadFile.Size ?? 0);
            if (newFileSize != oldFileSize)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("File size mismatch",
                    "The expected file size has changed. This might indicate a problem with the download source or the file itself. Do you want to continue?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                downloadFile.Size = newFileSize;
            }

            // Compare file name
            if (!urlDetails.FileName.Equals(downloadFile.FileName))
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("File name mismatch",
                    "The expected file name has changed. This might happen occasionally. Do you want to continue downloading with the new name?",
                    DialogButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    // Save new file name
                    var newFileName = urlDetails.FileName;
                    // Rename existing file
                    if (!downloadFile.SaveLocation.IsNullOrEmpty() && !downloadFile.FileName.IsNullOrEmpty())
                    {
                        // If the file exists, rename it
                        var filePath = Path.Combine(downloadFile.SaveLocation!, downloadFile.FileName!);
                        if (File.Exists(filePath))
                        {
                            // Select new name for file and check if it already exists or not
                            var newFilePath = Path.Combine(downloadFile.SaveLocation!, newFileName);
                            var overwriteExistingFile = false;
                            if (File.Exists(newFilePath))
                            {
                                result = await DialogBoxManager.ShowWarningDialogAsync("File already exists",
                                    $"A file named '{newFileName}' already exists in this location. Overwrite it?",
                                    DialogButtons.YesNo);

                                // When user chooses to overwrite the existing file, overwrite it
                                if (result == DialogResult.Yes)
                                {
                                    overwriteExistingFile = true;
                                }
                                // Otherwise, choose a new name for the file
                                else
                                {
                                    newFileName = AppService.DownloadFileService.GetNewFileName(newFileName, downloadFile.SaveLocation!);
                                    newFilePath = Path.Combine(downloadFile.SaveLocation!, newFileName);
                                }
                            }

                            File.Move(filePath, newFilePath, overwriteExistingFile);
                        }
                    }

                    // Save new download file name
                    downloadFile.FileName = newFileName;
                }
            }

            // Save new address
            downloadFile.Url = NewAddress;

            await AppService
                .DownloadFileService
                .UpdateDownloadFileAsync(downloadFile);

            if (restartDownloadFile)
            {
                _ = AppService
                    .DownloadFileService
                    .StartDownloadFileAsync(downloadFile);
            }

            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to refresh the download address. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (!NewAddress.IsNullOrEmpty() && NewAddress.CheckUrlValidation())
            {
                NewAddress = NewAddress.Replace("\\", "/").Trim();

                var downloadFile = AppService
                    .DownloadFileService
                    .DownloadFiles
                    .FirstOrDefault(df => df.Id == _downloadFile?.Id);

                if (downloadFile != null && !downloadFile.Url.IsNullOrEmpty() && !downloadFile.Url!.Equals(NewAddress))
                {
                    var result = await DialogBoxManager.ShowWarningDialogAsync(
                        "Refresh Download Address",
                        "Are you sure you want to cancel the refresh of the download address without saving the changes?",
                        DialogButtons.YesNoCancel);

                    switch (result)
                    {
                        case DialogResult.No:
                        {
                            await SaveAsync(owner);
                            return;
                        }

                        case DialogResult.Cancel:
                        {
                            return;
                        }
                    }
                }
            }

            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
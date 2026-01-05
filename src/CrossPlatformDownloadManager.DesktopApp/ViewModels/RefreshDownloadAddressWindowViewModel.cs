using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;
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
            // Validate new address and the owner window
            NewAddress = NewAddress.Replace("\\", "/").Trim();
            if (owner == null || NewAddress.IsStringNullOrEmpty() || !NewAddress.CheckUrlValidation())
                return;

            // Check if the new address is the same as the current address
            if (CurrentAddress.Equals(NewAddress))
            {
                owner.Close();
                return;
            }

            // Find the download file
            var downloadFile = AppService
                .DownloadFileService
                .DownloadFiles
                .FirstOrDefault(df => df.Id == _downloadFile?.Id);

            // Make sure the download file exists
            if (downloadFile == null)
                throw new InvalidOperationException("Download file not found.");

            // Check if the download file is completed
            if (downloadFile.IsCompleted)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Download completed",
                    "Download complete. To download with a new address, please delete the existing file and add a new download task.",
                    DialogButtons.Ok);

                return;
            }

            // Check if the download file is downloading or paused
            // Stop the download before changing the URL of it
            var restartDownloadFile = false;
            if (downloadFile.IsDownloading || downloadFile.IsPaused)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("Download in progress",
                    "The selected download is currently in progress. Changing the address will stop the current download and start a new one. Are you sure you want to proceed?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                // Stop download file
                await AppService
                    .DownloadFileService
                    .StopDownloadFileAsync(downloadFile, ensureStopped: true);

                restartDownloadFile = true;
            }

            // Create download options
            var options = new DownloadFileOptions
            {
                Referer = _downloadFile?.Referer,
                PageAddress = _downloadFile?.PageAddress,
                Description = _downloadFile?.Description,
                Username = _downloadFile?.Username,
                Password = _downloadFile?.Password
            };

            // Get url details
            var newDownloadFile = await AppService.DownloadFileService.GetDownloadFileFromUrlAsync(NewAddress, options);
            var isValid = await AppService.DownloadFileService.ValidateDownloadFileAsync(newDownloadFile);
            if (!isValid)
                return;

            // Compare file size
            var newFileSize = (long)(newDownloadFile.Size ?? 0);
            var oldFileSize = (long)(downloadFile.Size ?? 0);
            // Check if the new size is not equal to the old size.
            // If the size is not equal, ask the user if they want to continue.
            if (newFileSize != oldFileSize)
            {
                var result = await DialogBoxManager.ShowWarningDialogAsync("File size mismatch",
                    "The expected file size has changed. This might indicate a problem with the download source or the file itself. Do you want to continue?",
                    DialogButtons.YesNo);

                if (result != DialogResult.Yes)
                    return;

                downloadFile.Size = newDownloadFile.Size;
                downloadFile.IsSizeUnknown = newDownloadFile.IsSizeUnknown;
            }

            // Save new address
            downloadFile.Url = newDownloadFile.Url;
            // Update the download file with the new data
            await AppService
                .DownloadFileService
                .UpdateDownloadFileAsync(downloadFile);

            // If the download file was downloading or paused, restart it
            if (restartDownloadFile)
            {
                _ = AppService
                    .DownloadFileService
                    .StartDownloadFileAsync(downloadFile);
            }

            // Close the window
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

            if (!NewAddress.IsStringNullOrEmpty() && NewAddress.CheckUrlValidation())
            {
                NewAddress = NewAddress.Replace("\\", "/").Trim();

                var downloadFile = AppService
                    .DownloadFileService
                    .DownloadFiles
                    .FirstOrDefault(df => df.Id == _downloadFile?.Id);

                if (downloadFile != null && !downloadFile.Url.IsStringNullOrEmpty() && !downloadFile.Url!.Equals(NewAddress))
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
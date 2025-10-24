using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

/// <summary>
/// ViewModel for the CaptureUrlWindow, responsible for capturing and validating URLs from clipboard
/// and managing the download address input.
/// </summary>
public class CaptureUrlWindowViewModel : ViewModelBase
{
    #region Private Fields

    /// <summary>
    /// Backing field for the DownloadAddress property.
    /// </summary>
    private string? _downloadAddress;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the download URL address.
    /// </summary>
    public string? DownloadAddress
    {
        get => _downloadAddress;
        set => this.RaiseAndSetIfChanged(ref _downloadAddress, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to save the captured URL.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Command to cancel the URL capture operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the CaptureUrlWindowViewModel class.
    /// </summary>
    /// <param name="appService">Application service for accessing various services.</param>
    public CaptureUrlWindowViewModel(IAppService appService) : base(appService)
    {
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    /// <summary>
    /// Attempts to capture a URL from the clipboard and validates it.
    /// </summary>
    /// <param name="owner">The window that owns the clipboard.</param>
    public async Task CaptureUrlFromClipboardAsync(Window? owner)
    {
        try
        {
            if (owner?.Clipboard == null)
                return;

            var url = await owner.Clipboard.TryGetTextAsync();
            url = url?.Replace('\\', '/').Trim();
            if (!url.CheckUrlValidation())
                return;

            DownloadAddress = url;
        }
        catch
        {
            DownloadAddress = string.Empty;
        }
    }

    #region Command actions

    /// <summary>
    /// Handles the save operation for the captured URL.
    /// </summary>
    /// <param name="owner">The window to close after saving.</param>
    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            // Validate download address
            if (DownloadAddress.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Capture url",
                    "Please enter the download file address and save again.",
                    DialogButtons.Ok);

                return;
            }

            DownloadAddress = DownloadAddress!.Replace('\\', '/').Trim();
            var urlIsValid = DownloadAddress.CheckUrlValidation();
            if (!urlIsValid)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Capture url",
                    "The URL you entered is invalid. Please ensure it links directly to a file.",
                    DialogButtons.Ok);

                return;
            }

            var showStartDownloadDialog = AppService.SettingsService.Settings.ShowStartDownloadDialog;
            // Go to AddDownloadLinkWindow (Start download dialog) and let user choose what he/she want
            if (showStartDownloadDialog)
            {
                var vm = new AddDownloadLinkWindowViewModel(AppService)
                {
                    IsLoadingUrl = urlIsValid,
                    DownloadFile = { Url = DownloadAddress }
                };

                var window = new AddDownloadLinkWindow { DataContext = vm };
                window.Show();
            }
            // Otherwise, add link to database and start it
            else
            {
                var options = new DownloadFileOptions { StartDownloading = true };
                await AppService.DownloadFileService.AddDownloadFileAsync(url: DownloadAddress, options);
            }

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving captured url. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the cancel operation for the URL capture.
    /// </summary>
    /// <param name="owner">The window to close.</param>
    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while closing the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #endregion
}
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class CaptureUrlWindowViewModel : ViewModelBase
{
    #region Private Fields

    private string? _downloadAddress;

    #endregion

    #region Properties

    public string? DownloadAddress
    {
        get => _downloadAddress;
        set => this.RaiseAndSetIfChanged(ref _downloadAddress, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public CaptureUrlWindowViewModel(IAppService appService) : base(appService)
    {
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    public async Task CaptureUrlFromClipboardAsync(Window? owner)
    {
        try
        {
            if (owner?.Clipboard == null)
                return;

            var url = await owner.Clipboard.GetTextAsync();
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
                await AppService.DownloadFileService.AddDownloadFileAsync(DownloadAddress, startDownloading: true);
            }

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving captured url. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

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
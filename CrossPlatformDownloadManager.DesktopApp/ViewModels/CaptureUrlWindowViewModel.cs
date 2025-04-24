using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
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

    #region Command Actions

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
                await AddNewDownloadFileAndStartItAsync();
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

    #region Helpers

    private async Task AddNewDownloadFileAndStartItAsync()
    {
        // Get url details
        var urlDetails = await AppService.DownloadFileService.GetUrlDetailsAsync(DownloadAddress, CancellationToken.None);
        // Validate url details
        var validateResult = AppService.DownloadFileService.ValidateUrlDetails(urlDetails);
        if (!validateResult.IsValid)
        {
            if (validateResult.Title.IsStringNullOrEmpty() || validateResult.Message.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowDangerDialogAsync("Error downloading file",
                    "An error occurred while downloading the file.",
                    DialogButtons.Ok);
            }
            else
            {
                await DialogBoxManager.ShowDangerDialogAsync(validateResult.Title!, validateResult.Message!, DialogButtons.Ok);
            }

            return;
        }

        // Check duplicate download link
        DuplicateDownloadLinkAction? duplicateAction = null;
        if (urlDetails.IsUrlDuplicate)
        {
            var savedDuplicateAction = AppService.SettingsService.Settings.DuplicateDownloadLinkAction;
            if (savedDuplicateAction == DuplicateDownloadLinkAction.LetUserChoose)
            {
                duplicateAction = await AppService
                    .DownloadFileService
                    .GetUserDuplicateActionAsync(urlDetails.Url, urlDetails.FileName, urlDetails.Category!.CategorySaveDirectory!.SaveDirectory);
            }
            else
            {
                duplicateAction = savedDuplicateAction;
            }
        }

        // Create new download file
        var downloadFile = new DownloadFileViewModel
        {
            Url = urlDetails.Url,
            FileName = urlDetails.FileName,
            CategoryId = urlDetails.Category?.Id,
            Size = urlDetails.FileSize,
            IsSizeUnknown = urlDetails.IsFileSizeUnknown
        };

        // Add download file
        await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile,
            isUrlDuplicate: urlDetails.IsUrlDuplicate,
            duplicateAction: duplicateAction,
            isFileNameDuplicate: urlDetails.IsFileNameDuplicate,
            startDownloading: true);
    }

    #endregion
}
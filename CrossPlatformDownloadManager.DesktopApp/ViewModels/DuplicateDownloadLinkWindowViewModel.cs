using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DuplicateDownloadLinkWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly string _url;
    private readonly string _saveLocation;

    private bool _duplicateWithNumberedFile;
    private bool _overwriteExistingFile;
    private bool _showCompleteDialogOrResumeFile;
    private string _fileName;
    private string _newFileName = string.Empty;

    #endregion

    #region Properties

    public bool DuplicateWithNumberedFile
    {
        get => _duplicateWithNumberedFile;
        set => this.RaiseAndSetIfChanged(ref _duplicateWithNumberedFile, value);
    }

    public bool OverwriteExistingFile
    {
        get => _overwriteExistingFile;
        set => this.RaiseAndSetIfChanged(ref _overwriteExistingFile, value);
    }

    public bool ShowCompleteDialogOrResumeFile
    {
        get => _showCompleteDialogOrResumeFile;
        set => this.RaiseAndSetIfChanged(ref _showCompleteDialogOrResumeFile, value);
    }

    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    public string NewFileName
    {
        get => _newFileName;
        set => this.RaiseAndSetIfChanged(ref _newFileName, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public DuplicateDownloadLinkWindowViewModel(IAppService appService, string url, string saveLocation, string fileName) : base(appService)
    {
        _url = url;
        _saveLocation = saveLocation;
        _fileName = fileName;

        DuplicateWithNumberedFile = true;

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);

        CreateNewFileName();
    }

    private void CreateNewFileName()
    {
        NewFileName = AppService
            .DownloadFileService
            .GetNewFileName(_url, FileName, _saveLocation);

        if (NewFileName.IsNullOrEmpty())
            NewFileName = FileName;
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            DuplicateDownloadLinkAction dialogResult;
            switch (this)
            {
                case { DuplicateWithNumberedFile: true }:
                {
                    dialogResult = DuplicateDownloadLinkAction.DuplicateWithNumber;
                    break;
                }

                case { OverwriteExistingFile: true }:
                {
                    dialogResult = DuplicateDownloadLinkAction.OverwriteExisting;
                    break;
                }

                case { ShowCompleteDialogOrResumeFile: true }:
                {
                    dialogResult = DuplicateDownloadLinkAction.ShowCompleteDialogOrResume;
                    break;
                }

                default:
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Select an option",
                        "You haven't selected any options. Please make a selection and try again.",
                        DialogButtons.Ok);

                    return;
                }
            }

            CloseWindow(owner, dialogResult);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to save the duplicate download link option.");
        }
    }

    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            CloseWindow(owner);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to close the window.");
        }
    }

    #region Helpers

    private static void CloseWindow(Window window, DuplicateDownloadLinkAction? dialogResult = null)
    {
        window.Close(dialogResult);
    }

    #endregion
}
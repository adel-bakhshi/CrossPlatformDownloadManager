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

/// <summary>
/// ViewModel for the duplicate download link window, handling user choices for handling duplicate files.
/// </summary>
public class DuplicateDownloadLinkWindowViewModel : ViewModelBase
{
    #region Private Fields

    /// <summary>
    /// URL of the file to be downloaded.
    /// </summary>
    private readonly string _url;

    /// <summary>
    /// Location where the file will be saved.
    /// </summary>
    private readonly string _saveLocation;

    /// <summary>
    /// Flag indicating whether to duplicate the file with a numbered suffix.
    /// </summary>
    private bool _duplicateWithNumberedFile;

    /// <summary>
    /// Flag indicating whether to overwrite the existing file.
    /// </summary>
    private bool _overwriteExistingFile;

    /// <summary>
    /// Flag indicating whether to show completion dialog or resume the file.
    /// </summary>
    private bool _showCompleteDialogOrResumeFile;

    /// <summary>
    /// Original file name.
    /// </summary>
    private string _fileName;

    /// <summary>
    /// New file name (potentially with a number suffix).
    /// </summary>
    private string _newFileName = string.Empty;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether to duplicate the file with a numbered suffix.
    /// </summary>
    public bool DuplicateWithNumberedFile
    {
        get => _duplicateWithNumberedFile;
        set => this.RaiseAndSetIfChanged(ref _duplicateWithNumberedFile, value);
    }

    /// <summary>
    /// Gets or sets whether to overwrite the existing file.
    /// </summary>
    public bool OverwriteExistingFile
    {
        get => _overwriteExistingFile;
        set => this.RaiseAndSetIfChanged(ref _overwriteExistingFile, value);
    }

    /// <summary>
    /// Gets or sets whether to show completion dialog or resume the file.
    /// </summary>
    public bool ShowCompleteDialogOrResumeFile
    {
        get => _showCompleteDialogOrResumeFile;
        set => this.RaiseAndSetIfChanged(ref _showCompleteDialogOrResumeFile, value);
    }

    /// <summary>
    /// Gets or sets the original file name.
    /// </summary>
    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    /// <summary>
    /// Gets or sets the new file name (potentially with a number suffix).
    /// </summary>
    public string NewFileName
    {
        get => _newFileName;
        set => this.RaiseAndSetIfChanged(ref _newFileName, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Command to save the user's selection.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Command to cancel the operation.
    /// </summary>
    public ICommand CancelCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateDownloadLinkWindowViewModel"/> class.
    /// </summary>
    /// <param name="appService">Application service for accessing various services.</param>
    /// <param name="url">URL of the file to be downloaded.</param>
    /// <param name="saveLocation">Location where the file will be saved.</param>
    /// <param name="fileName">Original file name.</param>
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

    /// <summary>
    /// Gets the result based on the user's selection.
    /// </summary>
    /// <returns>The selected action or null if no action is selected.</returns>
    public DuplicateDownloadLinkAction? GetResult()
    {
        DuplicateDownloadLinkAction? dialogResult = this switch
        {
            { DuplicateWithNumberedFile: true } => DuplicateDownloadLinkAction.DuplicateWithNumber,
            { OverwriteExistingFile: true } => DuplicateDownloadLinkAction.OverwriteExisting,
            { ShowCompleteDialogOrResumeFile: true } => DuplicateDownloadLinkAction.ShowCompleteDialogOrResume,
            _ => null
        };

        return dialogResult;
    }

    /// <summary>
    /// Creates a new file name by adding a number suffix if the file already exists.
    /// </summary>
    private void CreateNewFileName()
    {
        NewFileName = AppService
            .DownloadFileService
            .GetNewFileName(_url, FileName, _saveLocation);

        if (NewFileName.IsStringNullOrEmpty())
            NewFileName = FileName;
    }

    /// <summary>
    /// Saves the user's selection and closes the window.
    /// </summary>
    /// <param name="owner">Window that owns this dialog.</param>
    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var dialogResult = GetResult();
            if (dialogResult == null)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Select an option",
                    "You haven't selected any options. Please make a selection and try again.",
                    DialogButtons.Ok);

                return;
            }

            CloseWindow(owner, dialogResult);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to save the duplicate download link option.");
        }
    }

    /// <summary>
    /// Cancels the operation and closes the window.
    /// </summary>
    /// <param name="owner">Window that owns this dialog.</param>
    private async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            // When user chooses cancel, we will set the action to null
            DuplicateWithNumberedFile = OverwriteExistingFile = ShowCompleteDialogOrResumeFile = false;
            // Close the window
            CloseWindow(owner);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occurred while trying to close the window.");
        }
    }

    #region Helpers

    /// <summary>
    /// Closes the window with the specified dialog result.
    /// </summary>
    /// <param name="window">Window to close.</param>
    /// <param name="dialogResult">Result to pass back to the window.</param>
    private static void CloseWindow(Window window, DuplicateDownloadLinkAction? dialogResult = null)
    {
        window.Close(dialogResult);
    }

    #endregion
}
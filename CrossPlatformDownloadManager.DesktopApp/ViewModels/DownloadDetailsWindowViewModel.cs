using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

/// <summary>
/// Represents the view model for the download details window.
/// </summary>
public class DownloadDetailsWindowViewModel : ViewModelBase
{
    #region Private fields

    // Backing fields for the properties.
    private DownloadFileViewModel? _downloadFile;
    private string? _saveLocation;
    private string? _username;
    private string? _password;
    private string? _description;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates the download file data.
    /// </summary>
    public DownloadFileViewModel? DownloadFile
    {
        get => _downloadFile;
        set => this.RaiseAndSetIfChanged(ref _downloadFile, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the save location for the download.
    /// </summary>
    public string? SaveLocation
    {
        get => _saveLocation;
        set => this.RaiseAndSetIfChanged(ref _saveLocation, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the username for the download.
    /// </summary>
    public string? Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the password for the download.
    /// </summary>
    public string? Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the description for the download.
    /// </summary>
    public string? Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets a command that is executed when the save button is clicked.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Gets a command that is executed when the cancel button is clicked.
    /// </summary>
    public ICommand CancelCommand { get; }

    /// <summary>
    /// Gets a command that is executed when the browse save location button is clicked.
    /// </summary>
    public ICommand BrowseSaveLocationCommand { get; }

    /// <summary>
    /// Gets a command that is executed when the open link button is clicked.
    /// </summary>
    public ICommand OpenLinkCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the DownloadDetailsWindowViewModel class.
    /// </summary>
    /// <param name="appService">The application service to use.</param>
    /// <param name="downloadFile">The download file view model to initialize with.</param>
    public DownloadDetailsWindowViewModel(IAppService appService, DownloadFileViewModel downloadFile) : base(appService)
    {
        // Check for null arguments
        ArgumentNullException.ThrowIfNull(downloadFile);

        // Initialize the properties
        DownloadFile = downloadFile;
        SaveLocation = DownloadFile.GetFilePath();
        Username = DownloadFile.Username;
        Password = DownloadFile.Password;
        Description = DownloadFile.Description;

        // Initialize the commands
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
        BrowseSaveLocationCommand = ReactiveCommand.CreateFromTask(BrowseSaveLocationAsync);
        OpenLinkCommand = ReactiveCommand.CreateFromTask<string?>(OpenLinkAsync);
    }

    /// <summary>
    /// Asynchronously saves the download details.
    /// </summary>
    /// <param name="owner">The window that owns the save operation.</param>
    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            // Get directory path of the save location
            var directoryPath = Path.GetDirectoryName(SaveLocation);
            // Check if the save location is null or empty
            if (directoryPath.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowDangerDialogAsync(
                    dialogMessage: "Save location is null or empty. \nMake sure choose a valid save location for your download file.",
                    dialogHeader: "Invalid save location",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Add a backslash to the end of the directory path if the original save location had one
            directoryPath = DownloadFile?.SaveLocation?.EndsWith('\\') == true ? directoryPath + "\\" : directoryPath;
            // Check if the user changed save location
            if (DownloadFile!.SaveLocation?.Equals(directoryPath) != true)
            {
                // Get current file path and move it if the file exists
                var filePath = DownloadFile.GetFilePath();
                if (File.Exists(filePath))
                {
                    var newFilePath = Path.Combine(directoryPath!, DownloadFile.FileName!);
                    if (!filePath.Equals(newFilePath))
                        await filePath.MoveFileAsync(newFilePath);
                }

                // Update the save location of the download file
                DownloadFile.SaveLocation = directoryPath;
            }

            // Update download file data
            DownloadFile.Username = Username;
            DownloadFile.Password = Password;
            DownloadFile.Description = Description;

            // Save download file data
            await AppService.DownloadFileService.UpdateDownloadFileAsync(DownloadFile);
            // Close window
            CloseWindow(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving the download details. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Asynchronously cancels the operation and closes the window.
    /// </summary>
    /// <param name="owner">The window to close.</param>
    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            CloseWindow(owner);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while closing the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Asynchronously browses for a save location.
    /// </summary>
    private async Task BrowseSaveLocationAsync()
    {
        try
        {
            // Get storage provider and check if it is available
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            if (storageProvider == null)
                throw new InvalidOperationException("The storage provider is not available.");

            // Get directory path of the save location
            var directoryPath = Path.GetDirectoryName(SaveLocation);
            // Create folder picker options
            var options = new FolderPickerOpenOptions
            {
                Title = "Select temporary file location",
                AllowMultiple = false,
                SuggestedStartLocation = directoryPath.IsStringNullOrEmpty() ? null : await storageProvider.TryGetFolderFromPathAsync(directoryPath!)
            };

            // Open folder picker and wait for user to select a folder
            var directories = await storageProvider.OpenFolderPickerAsync(options);
            // Check if the user selected a folder
            if (!directories.Any())
                return;

            // Set the save location
            directoryPath = directories[0].Path.IsAbsoluteUri ? directories[0].Path.LocalPath : directories[0].Path.OriginalString;
            SaveLocation = Path.Combine(directoryPath, DownloadFile!.FileName!);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while browsing the temporary file location. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Asynchronously opens a link in the default browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    private static async Task OpenLinkAsync(string? url)
    {
        try
        {
            if (url.IsStringNullOrEmpty() || !url.CheckUrlValidation())
                return;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while opening the link. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #region Helpers

    /// <summary>
    /// Closes the specified window.
    /// </summary>
    /// <param name="owner">The window to close.</param>
    private static void CloseWindow(Window? owner)
    {
        owner?.Close();
    }

    #endregion
}
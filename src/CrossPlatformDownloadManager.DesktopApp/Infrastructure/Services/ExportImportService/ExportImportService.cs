using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService;

/// <summary>
/// Represents the service for exporting and importing data.
/// </summary>
public class ExportImportService : IExportImportService
{
    #region Private fileds

    /// <summary>
    /// The download file service to access download files.
    /// </summary>
    private readonly IDownloadFileService _downloadFileService;

    /// <summary>
    /// The settings service to access settings.
    /// </summary>
    private readonly ISettingsService _settingsService;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ExportImportService"/> class.
    /// </summary>
    /// <param name="downloadFileService">The download file service.</param>
    /// <param name="settingsService">The settings service.</param>
    public ExportImportService(IDownloadFileService downloadFileService, ISettingsService settingsService)
    {
        _downloadFileService = downloadFileService;
        _settingsService = settingsService;

        Log.Debug("ExportImportService initialized successfully.");
    }

    public async Task ExportDataAsync(bool exportAsCdmFile)
    {
        Log.Information("Starting data export. Export type: {ExportType}", exportAsCdmFile ? "CDM" : "Text");

        try
        {
            // Get storage provider
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
                Log.Warning("Storage provider is not available for data export");
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Export data",
                    dialogMessage: "Can't access to storage provider. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Get download files
            var downloadFiles = await GetDownloadFilesForExportAsync();
            Log.Debug("Retrieved {DownloadFileCount} download files for export", downloadFiles.Count);

            // Create json file
            string downloadFilesContent;
            if (exportAsCdmFile)
            {
                Log.Debug("Creating CDM format export data");
                // Create export download files
                var exportDownloadFiles = downloadFiles
                    .Select(df => new DownloadFileData
                    {
                        Url = df.Url ?? string.Empty,
                        Referer = df.Referer ?? string.Empty,
                        PageAddress = df.PageAddress ?? string.Empty
                    })
                    .ToList();

                downloadFilesContent = exportDownloadFiles.ConvertToJson();
                Log.Debug("Created CDM export data with {FileCount} files", exportDownloadFiles.Count);
            }
            else
            {
                Log.Debug("Creating text format export data");
                var exportDownloadFiles = downloadFiles
                    .Select(df => df.Url)
                    .ToList();

                // Create string builder and append urls to it
                var builder = new StringBuilder();
                foreach (var fileData in exportDownloadFiles)
                    builder.AppendLine(fileData);

                downloadFilesContent = builder.ToString().Trim();
                Log.Debug("Created text export data with {FileCount} files", exportDownloadFiles.Count);
            }

            // Open save file picker and save export file
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // Create save file picker options
            var savePickerOpenOptions = new FilePickerSaveOptions
            {
                Title = "Export data",
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(desktopPath),
                SuggestedFileName = "cdm-data"
            };

            // Fill save options based on export type
            if (exportAsCdmFile)
            {
                savePickerOpenOptions.DefaultExtension = "cdm";
                savePickerOpenOptions.FileTypeChoices =
                [
                    new FilePickerFileType("CDM export file") { Patterns = ["*.cdm"] }
                ];

                Log.Debug("Configured file picker for CDM export");
            }
            else
            {
                savePickerOpenOptions.DefaultExtension = "txt";
                savePickerOpenOptions.FileTypeChoices =
                [
                    new FilePickerFileType("Text export file") { Patterns = ["*.txt"] }
                ];

                Log.Debug("Configured file picker for text export");
            }

            // Open save file picker
            var savePickerResult = await storageProvider.SaveFilePickerAsync(savePickerOpenOptions);
            // Check if save file picker result is not null
            if (savePickerResult == null)
            {
                Log.Information("Data export cancelled by user");
                return;
            }

            Log.Debug("Saving export file to: {FilePath}", savePickerResult.Path.LocalPath);

            // Save export file
            await SaveTextFileAsync(savePickerResult.Path.LocalPath, downloadFilesContent);

            // Show success message
            await DialogBoxManager.ShowSuccessDialogAsync(dialogHeader: "Export data",
                dialogMessage: "Your export file has been created successfully.",
                dialogButtons: DialogButtons.Ok);

            Log.Information("Data export completed successfully. File: {FilePath}", savePickerResult.Path.LocalPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to export CDM data. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task ImportDataAsync()
    {
        Log.Information("Starting data import");

        try
        {
            // Get storage provider
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
                Log.Warning("Storage provider is not available for data import");
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Export data",
                    dialogMessage: "Can't access to storage provider. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Create options
            var options = new FilePickerOpenOptions
            {
                Title = "Import data",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("CDM export file") { Patterns = ["*.cdm"] },
                    new FilePickerFileType("Text export file") { Patterns = ["*.txt"] }
                ]
            };

            Log.Debug("Opening file picker for data import");

            // Open file picker to pick files
            var exportFiles = await storageProvider.OpenFilePickerAsync(options);
            if (exportFiles.Count == 0)
            {
                Log.Information("Data import cancelled by user");
                return;
            }

            // Get the file path
            var exportFilePath = exportFiles[0].Path.LocalPath;
            Log.Debug("Selected import file: {FilePath}", exportFilePath);

            // Get the extension of the file
            var ext = Path.GetExtension(exportFilePath);
            List<DownloadFileViewModel> downloadFiles;
            // Load download files based on file extension
            if (ext.Equals(".cdm", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Processing CDM format import file");
                // Read the file content
                var json = await File.ReadAllTextAsync(exportFilePath);
                // Convert the json content to a list of DownloadFileData
                var downloadFileDataList = json.ConvertFromJson<List<DownloadFileData>?>();
                // Check if the list is empty
                if (downloadFileDataList == null)
                {
                    Log.Warning("No data found in CDM import file");
                    throw new InvalidOperationException("There is no data in the export file.");
                }

                Log.Debug("Found {FileCount} entries in CDM import file", downloadFileDataList.Count);

                // Select the urls from the list of DownloadFileData
                downloadFiles = downloadFileDataList
                    .Where(df => !df.Url.IsStringNullOrEmpty() && df.Url.CheckUrlValidation())
                    .Select(df => new DownloadFileViewModel
                    {
                        Url = df.Url,
                        Referer = df.Referer,
                        PageAddress = df.PageAddress
                    })
                    .ToList();

                Log.Debug("Filtered to {ValidFileCount} valid URLs from CDM file", downloadFiles.Count);
            }
            else
            {
                Log.Debug("Processing text format import file");
                // Read the file content
                var content = await File.ReadAllLinesAsync(exportFilePath);
                // Select the urls from the file content
                downloadFiles = content
                    .Where(url => !url.IsStringNullOrEmpty() && url.CheckUrlValidation())
                    .Select(url => new DownloadFileViewModel { Url = url })
                    .ToList();

                Log.Debug("Filtered to {ValidFileCount} valid URLs from text file", downloadFiles.Count);
            }

            // Check if the list of urls is empty
            if (downloadFiles.Count == 0)
            {
                Log.Warning("No valid data found in import file");
                await DialogBoxManager.ShowInfoDialogAsync(dialogHeader: "Import data",
                    dialogMessage: "There is no valid data in the export file.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Get the service provider
            var serviceProvider = Application.Current?.GetServiceProvider();
            // Get the app service
            var appService = serviceProvider?.GetService<IAppService>();
            if (appService == null)
            {
                Log.Warning("App service is not available for data import");
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Import data",
                    dialogMessage: "Can't access to app service. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            Log.Debug("Creating ManageLinksWindow with {FileCount} imported files", downloadFiles.Count);

            // Create a new ManageLinksWindowViewModel
            var viewModel = new ManageLinksWindowViewModel(appService, downloadFiles);
            // Create a new ManageLinksWindow
            var window = new ManageLinksWindow { DataContext = viewModel };
            // Show the window
            window.Show();

            Log.Information("Data import completed successfully. Imported {FileCount} files", downloadFiles.Count);
        }
        catch (Exception ex)
        {
            // Log the error
            Log.Error(ex, "An error occurred while trying to import CDM data. Error message: {ErrorMessage}", ex.Message);
            // Show an error dialog
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task ExportSettingsAsync()
    {
        Log.Information("Starting settings export");

        try
        {
            // Get main window
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
                Log.Warning("Storage provider is not available for settings export");
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Export settings",
                    dialogMessage: "Can't access to storage provider. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Open save file picker and save export file
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            // Create save file picker options
            var savePickerOpenOptions = new FilePickerSaveOptions
            {
                Title = "Export settings",
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(desktopPath),
                SuggestedFileName = "cdm-settings",
                DefaultExtension = "json",
                FileTypeChoices =
                [
                    new FilePickerFileType("Json file") { Patterns = ["*.json"] },
                    new FilePickerFileType("Text file") { Patterns = ["*.txt"] }
                ]
            };

            Log.Debug("Opening file picker for settings export");

            // Open save file picker
            var savePickerResult = await storageProvider.SaveFilePickerAsync(savePickerOpenOptions);
            // Check if save file picker result is not null
            if (savePickerResult == null)
            {
                Log.Information("Settings export cancelled by user");
                return;
            }

            Log.Debug("Preparing settings data for export");

            // Get settings and convert to export file
            var settings = _settingsService.Settings;
            var proxies = settings.Proxies.ToList();
            var exportSettings = SettingsData.CreateExportFile(settings, proxies);
            var json = exportSettings.ConvertToJson();

            Log.Debug("Exporting {ProxyCount} proxies with settings", proxies.Count);

            // Save export file
            await SaveTextFileAsync(savePickerResult.Path.LocalPath, json);

            // Show success message
            await DialogBoxManager.ShowSuccessDialogAsync(dialogHeader: "Export settings",
                dialogMessage: "Your export file has been created successfully.",
                dialogButtons: DialogButtons.Ok);

            Log.Information("Settings export completed successfully. File: {FilePath}", savePickerResult.Path.LocalPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to export settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task ImportSettingsAsync()
    {
        Log.Information("Starting settings import");

        try
        {
            // Get main window
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
                Log.Warning("Storage provider is not available for settings import");
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Import settings",
                    dialogMessage: "Can't access to storage provider. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Create open file picker options
            var filePickerOpenOptions = new FilePickerOpenOptions
            {
                Title = "Import settings",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Json file") { Patterns = ["*.json"] },
                    new FilePickerFileType("Text file") { Patterns = ["*.txt"] }
                ]
            };

            Log.Debug("Opening file picker for settings import");

            // Open file picker to pick settings data
            var filePickerResult = await storageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (!filePickerResult.Any() || filePickerResult[0].Path.LocalPath.IsStringNullOrEmpty())
            {
                Log.Information("Settings import cancelled by user");
                return;
            }

            // Read file and convert to settings data
            var filePath = filePickerResult[0].Path.LocalPath;
            Log.Debug("Reading settings from file: {FilePath}", filePath);

            var json = await File.ReadAllTextAsync(filePath);
            var exportSettings = json.ConvertFromJson<SettingsData?>();

            Log.Debug("Starting import of settings data");

            // Import settings data
            await ImportSettingsDataAsync(exportSettings);

            Log.Information("Settings import completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to import settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #region Helpers

    /// <summary>
    /// Gets download files for export.
    /// </summary>
    /// <returns>A list of download files.</returns>
    private async Task<List<DownloadFileViewModel>> GetDownloadFilesForExportAsync()
    {
        Log.Debug("Getting download files for export");

        // Get download files
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .ToList();

        Log.Debug("Found {TotalFileCount} download files", downloadFiles.Count);

        // If there is no download file that is completed, return the download files
        if (!downloadFiles.Exists(df => df.IsCompleted))
        {
            Log.Debug("No completed download files found, returning all files");
            return downloadFiles;
        }

        var completedCount = downloadFiles.Count(df => df.IsCompleted);
        Log.Debug("Found {CompletedFileCount} completed download files", completedCount);

        // Otherwise, Ask the users if they want to export the completed files
        var result = await DialogBoxManager.ShowInfoDialogAsync("Export data",
            "Some downloads are complete. Would you like to export the finished files?",
            DialogButtons.YesNo);

        // Remove completed download files from export
        if (result != DialogResult.Yes)
        {
            Log.Debug("User chose to exclude completed files from export");
            downloadFiles = downloadFiles
                .Where(df => !df.IsCompleted)
                .ToList();

            Log.Debug("Filtered to {FilteredFileCount} incomplete files", downloadFiles.Count);
        }
        else
        {
            Log.Debug("User chose to include completed files in export");
        }

        return downloadFiles;
    }

    /// <summary>
    /// Saves a text file to the specified path.
    /// </summary>
    /// <param name="filePath">The path to save the file to.</param>
    /// <param name="content">The content of the file.</param>
    /// <exception cref="InvalidOperationException">Thrown when the file path is null or empty.</exception>
    private static async Task SaveTextFileAsync(string filePath, string? content)
    {
        Log.Debug("Saving text file to: {FilePath}", filePath);

        // Make sure file path is not null
        if (filePath.IsStringNullOrEmpty())
        {
            Log.Warning("File path is null or empty, cannot save file");
            return;
        }

        // Get the directory of the file path
        var directory = Path.GetDirectoryName(filePath);
        if (directory.IsStringNullOrEmpty())
        {
            Log.Warning("Directory path is null or empty, cannot save file");
            return;
        }

        // Create the directory if it doesn't exist
        if (!Directory.Exists(directory!))
        {
            Log.Debug("Creating directory: {Directory}", directory);
            Directory.CreateDirectory(directory!);
        }

        // Write the content to the file
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        Log.Debug("File saved successfully: {FilePath}", filePath);
    }

    /// <summary>
    /// Imports settings data from the specified <see cref="SettingsData"/> object.
    /// </summary>
    /// <param name="exportSettings">The <see cref="SettingsData"/> object to import settings from.</param>
    private async Task ImportSettingsDataAsync(SettingsData? exportSettings)
    {
        Log.Debug("Starting settings data import");

        // Make sure export settings is not null
        if (exportSettings == null)
        {
            Log.Warning("Export settings is null, cannot import");
            return;
        }

        Log.Debug("Importing settings with {ProxyCount} proxies", exportSettings.Proxies.Count);

        // Get settings from settings service
        var settings = _settingsService.Settings;

        // Update settings with new values
        settings.StartOnSystemStartup = exportSettings.StartOnSystemStartup;
        settings.UseBrowserExtension = exportSettings.UseBrowserExtension;
        settings.UseManager = exportSettings.UseManager;
        settings.AlwaysKeepManagerOnTop = exportSettings.AlwaysKeepManagerOnTop;
        settings.ApplicationFont = exportSettings.ApplicationFont;
        settings.ShowStartDownloadDialog = exportSettings.ShowStartDownloadDialog;
        settings.ShowCompleteDownloadDialog = exportSettings.ShowCompleteDownloadDialog;
        settings.DuplicateDownloadLinkAction = exportSettings.DuplicateDownloadLinkAction;
        settings.MaximumConnectionsCount = exportSettings.MaximumConnectionsCount;
        settings.IsSpeedLimiterEnabled = exportSettings.IsSpeedLimiterEnabled;
        settings.LimitSpeed = exportSettings.LimitSpeed;
        settings.LimitUnit = exportSettings.LimitUnit;
        settings.IsMergeSpeedLimitEnabled = exportSettings.IsMergeSpeedLimitEnabled;
        settings.MergeLimitSpeed = exportSettings.MergeLimitSpeed;
        settings.MergeLimitUnit = exportSettings.MergeLimitUnit;
        settings.MaximumMemoryBufferBytes = exportSettings.MaximumMemoryBufferBytes;
        settings.MaximumMemoryBufferBytesUnit = exportSettings.MaximumMemoryBufferBytesUnit;
        settings.ProxyMode = exportSettings.ProxyMode;
        settings.ProxyType = exportSettings.ProxyType;
        settings.UseDownloadCompleteSound = exportSettings.UseDownloadCompleteSound;
        settings.UseDownloadStoppedSound = exportSettings.UseDownloadStoppedSound;
        settings.UseDownloadFailedSound = exportSettings.UseDownloadFailedSound;
        settings.UseQueueStartedSound = exportSettings.UseQueueStartedSound;
        settings.UseQueueStoppedSound = exportSettings.UseQueueStoppedSound;
        settings.UseQueueFinishedSound = exportSettings.UseQueueFinishedSound;
        settings.UseSystemNotifications = exportSettings.UseSystemNotifications;
        settings.ShowCategoriesPanel = exportSettings.ShowCategoriesPanel;
        settings.DataGridColumnSettings = exportSettings.DataGridColumnsSettings?.ConvertFromJson<MainGridColumnSettings?>() ?? settings.DataGridColumnSettings;

        Log.Debug("Updated application settings from import data");

        // Save settings
        await _settingsService.SaveSettingsAsync(settings);
        Log.Debug("Saved updated settings to database");

        // Get saved proxies in database
        var proxiesInDb = _settingsService.Settings.Proxies.ToList();
        Log.Debug("Found {ExistingProxyCount} existing proxies in database", proxiesInDb.Count);

        // Get proxies that are not in database
        var newProxies = exportSettings
            .Proxies
            .Where(proxy => proxiesInDb.Find(p => proxy.Type.Equals(p.Type) && proxy.Host.Equals(p.Host) && proxy.Port.Equals(p.Port)) == null)
            .Select(proxy => new ProxySettingsViewModel
            {
                Name = proxy.Name,
                Type = proxy.Type,
                Host = proxy.Host,
                Port = proxy.Port,
                Username = proxy.Username,
                Password = proxy.Password
            })
            .ToList();

        Log.Debug("Found {NewProxyCount} new proxies to add", newProxies.Count);

        // Add new proxies to database
        foreach (var proxy in newProxies)
        {
            await _settingsService.AddProxySettingsAsync(proxy);
            Log.Debug("Added new proxy: {ProxyName}", proxy.Name);
        }

        Log.Information("Settings data import completed successfully. Added {NewProxyCount} new proxies", newProxies.Count);
    }

    #endregion
}
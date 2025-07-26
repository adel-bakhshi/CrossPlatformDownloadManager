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

public class ExportImportService : IExportImportService
{
    #region Private fileds

    private readonly IDownloadFileService _downloadFileService;
    private readonly ISettingsService _settingsService;

    #endregion

    public ExportImportService(IDownloadFileService downloadFileService, ISettingsService settingsService)
    {
        _downloadFileService = downloadFileService;
        _settingsService = settingsService;
    }

    public async Task ExportDataAsync(bool exportAsCdmFile)
    {
        try
        {
            // Get storage provider
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Export data",
                    dialogMessage: "Can't access to storage provider. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Get download files
            var downloadFiles = await GetDownloadFilesForExportAsync();
            // Create json file
            string downloadFilesContent;
            if (exportAsCdmFile)
            {
                // Create export download files
                var exportDownloadFiles = downloadFiles
                    .Select(df => new DownloadFileData { Url = df.Url ?? string.Empty })
                    .ToList();

                downloadFilesContent = exportDownloadFiles.ConvertToJson();
            }
            else
            {
                var exportDownloadFiles = downloadFiles
                    .Select(df => df.Url)
                    .ToList();

                // Create string builder and append urls to it
                var builder = new StringBuilder();
                foreach (var fileData in exportDownloadFiles)
                    builder.AppendLine(fileData);

                downloadFilesContent = builder.ToString().Trim();
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
            }
            else
            {
                savePickerOpenOptions.DefaultExtension = "txt";
                savePickerOpenOptions.FileTypeChoices =
                [
                    new FilePickerFileType("Text export file") { Patterns = ["*.txt"] }
                ];
            }

            // Open save file picker
            var savePickerResult = await storageProvider.SaveFilePickerAsync(savePickerOpenOptions);
            // Check if save file picker result is not null
            if (savePickerResult == null)
                return;

            // Save export file
            await SaveTextFileAsync(savePickerResult.Path.LocalPath, downloadFilesContent);

            // Show success message
            await DialogBoxManager.ShowSuccessDialogAsync(dialogHeader: "Export data",
                dialogMessage: "Your export file has been created successfully.",
                dialogButtons: DialogButtons.Ok);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to export CDM data. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task ImportDataAsync()
    {
        try
        {
            // Get storage provider
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Export data",
                    dialogMessage: "Can't access to storage provider. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

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

            var exportFiles = await storageProvider.OpenFilePickerAsync(options);
            if (exportFiles.Count == 0)
                return;

            var exportFilePath = exportFiles[0].Path.LocalPath;
            var ext = Path.GetExtension(exportFilePath);
            List<DownloadFileViewModel> downloadFiles;
            if (ext.Equals(".cdm", StringComparison.OrdinalIgnoreCase))
            {
                var json = await File.ReadAllTextAsync(exportFilePath);
                var downloadFileDataList = json.ConvertFromJson<List<DownloadFileData>?>();
                if (downloadFileDataList == null)
                    throw new InvalidOperationException("There is no data in the export file.");

                downloadFiles = downloadFileDataList
                    .Where(df => !df.Url.IsStringNullOrEmpty() && df.Url.CheckUrlValidation())
                    .Select(df => new DownloadFileViewModel { Url = df.Url })
                    .ToList();
            }
            else
            {
                var content = await File.ReadAllLinesAsync(exportFilePath);
                downloadFiles = content
                    .Where(url => !url.IsStringNullOrEmpty() && url.CheckUrlValidation())
                    .Select(url => new DownloadFileViewModel { Url = url })
                    .ToList();
            }

            if (downloadFiles.Count == 0)
            {
                await DialogBoxManager.ShowInfoDialogAsync(dialogHeader: "Import data",
                    dialogMessage: "There is no valid data in the export file.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            var serviceProvider = Application.Current?.GetServiceProvider();
            var appService = serviceProvider?.GetService<IAppService>();
            if (appService == null)
            {
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Import data",
                    dialogMessage: "Can't access to app service. Please, try again later.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            var viewModel = new ManageLinksWindowViewModel(appService, downloadFiles);
            var window = new ManageLinksWindow { DataContext = viewModel };
            window.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to import CDM data. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task ExportSettingsAsync()
    {
        try
        {
            // Get main window
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
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

            // Open save file picker
            var savePickerResult = await storageProvider.SaveFilePickerAsync(savePickerOpenOptions);
            // Check if save file picker result is not null
            if (savePickerResult == null)
                return;

            // Get settings and convert to export file
            var settings = _settingsService.Settings;
            var proxies = settings.Proxies.ToList();
            var exportSettings = SettingsData.CreateExportFile(settings, proxies);
            var json = exportSettings.ConvertToJson();

            // Save export file
            await SaveTextFileAsync(savePickerResult.Path.LocalPath, json);

            // Show success message
            await DialogBoxManager.ShowSuccessDialogAsync(dialogHeader: "Export settings",
                dialogMessage: "Your export file has been created successfully.",
                dialogButtons: DialogButtons.Ok);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to export settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task ImportSettingsAsync()
    {
        try
        {
            // Get main window
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            // Check if storage provider is available
            if (storageProvider == null)
            {
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

            // Open file picker to pick settings data
            var filePickerResult = await storageProvider.OpenFilePickerAsync(filePickerOpenOptions);
            if (!filePickerResult.Any() || filePickerResult[0].Path.LocalPath.IsStringNullOrEmpty())
                return;

            // Read file and convert to settings data
            var filePath = filePickerResult[0].Path.LocalPath;
            var json = await File.ReadAllTextAsync(filePath);
            var exportSettings = json.ConvertFromJson<SettingsData?>();
            // Import settings data
            await ImportSettingsDataAsync(exportSettings);
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
        // Get download files
        var downloadFiles = _downloadFileService
            .DownloadFiles
            .ToList();

        // If there is no download file that is completed, return the download files
        if (!downloadFiles.Exists(df => df.IsCompleted))
            return downloadFiles;

        // Otherwise, Ask the users if they want to export the completed files
        var result = await DialogBoxManager.ShowInfoDialogAsync("Export data",
            "Some downloads are complete. Would you like to export the finished files?",
            DialogButtons.YesNo);

        // Remove completed download files from export
        if (result != DialogResult.Yes)
        {
            downloadFiles = downloadFiles
                .Where(df => !df.IsCompleted)
                .ToList();
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
        // Make sure file path is not null
        if (filePath.IsStringNullOrEmpty())
            return;

        // Get the directory of the file path
        var directory = Path.GetDirectoryName(filePath);
        if (directory.IsStringNullOrEmpty())
            return;

        // Create the directory if it doesn't exist
        if (!Directory.Exists(directory!))
            Directory.CreateDirectory(directory!);

        // Write the content to the file
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Imports settings data from the specified <see cref="SettingsData"/> object.
    /// </summary>
    /// <param name="exportSettings">The <see cref="SettingsData"/> object to import settings from.</param>
    private async Task ImportSettingsDataAsync(SettingsData? exportSettings)
    {
        // Make sure export settings is not null
        if (exportSettings == null)
            return;

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
        settings.DataGridColumnsSettings = exportSettings.DataGridColumnsSettings?.ConvertFromJson<MainDownloadFilesDataGridColumnsSettings?>()
                                           ?? settings.DataGridColumnsSettings;

        // Save settings
        await _settingsService.SaveSettingsAsync(settings);
        // Get saved proxies in database
        var proxiesInDb = _settingsService.Settings.Proxies.ToList();

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

        // Add new proxies to database
        foreach (var proxy in newProxies)
            await _settingsService.AddProxySettingsAsync(proxy);
    }

    #endregion
}
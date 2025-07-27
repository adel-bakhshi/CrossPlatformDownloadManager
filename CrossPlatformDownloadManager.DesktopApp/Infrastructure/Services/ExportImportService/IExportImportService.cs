using System.Threading.Tasks;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService;

/// <summary>
/// Service for exporting and importing data
/// </summary>
public interface IExportImportService
{
    /// <summary>
    /// Export data to file.
    /// </summary>
    /// <param name="exportAsCdmFile">If true, export as CDM file, otherwise export as Text file</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task ExportDataAsync(bool exportAsCdmFile);

    /// <summary>
    /// Import data from file.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task ImportDataAsync();

    /// <summary>
    /// Export settings to file.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task ExportSettingsAsync();

    /// <summary>
    /// Import setting from file.
    /// </summary>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    Task ImportSettingsAsync();
}
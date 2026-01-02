using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService.Models;

/// <summary>
/// Represents download file data for export/import operations.
/// </summary>
public class DownloadFileData
{
    #region Properties

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("referer")]
    public string Referer { get; set; } = string.Empty;

    [JsonProperty("pageAddress")]
    public string PageAddress { get; set; } = string.Empty;

    #endregion
}
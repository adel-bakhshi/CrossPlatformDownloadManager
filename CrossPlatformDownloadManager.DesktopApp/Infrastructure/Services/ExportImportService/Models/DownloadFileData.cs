using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService.Models;

public class DownloadFileData
{
    #region Properties

    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    #endregion
}
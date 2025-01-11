using CrossPlatformDownloadManager.Utils.Enums;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.Exports;

public class ExportDownloadFileViewModel
{
    #region Properties

    [JsonProperty("url")] public string Url { get; set; } = string.Empty;
    [JsonProperty("fileName")] public string FileName { get; set; } = string.Empty;
    [JsonProperty("downloadQueueId")] public int? DownloadQueueId { get; set; }
    [JsonProperty("size")] public double Size { get; set; }
    [JsonProperty("description")] public string? Description { get; set; }
    [JsonProperty("status")] public DownloadFileStatus? Status { get; set; }
    [JsonProperty("downloadQueuePriority")] public int? DownloadQueuePriority { get; set; }
    [JsonProperty("downloadProgress")] public float DownloadProgress { get; set; }
    [JsonProperty("downloadPackage")] public string? DownloadPackage { get; set; }
    [JsonProperty("addedToDefaultQueue")] public bool AddedToDefaultQueue { get; set; }

    #endregion

    public static ExportDownloadFileViewModel CreateExportFile(DownloadFileViewModel downloadFile)
    {
        return new ExportDownloadFileViewModel
        {
            Url = downloadFile.Url ?? string.Empty,
            FileName = downloadFile.FileName ?? string.Empty,
            DownloadQueueId = downloadFile.DownloadQueueId,
            Size = downloadFile.Size ?? 0,
            Description = downloadFile.Description,
            Status = downloadFile.Status,
            DownloadQueuePriority = downloadFile.DownloadQueuePriority,
            DownloadProgress = downloadFile.DownloadProgress ?? 0f,
            DownloadPackage = downloadFile.DownloadPackage
        };
    }
}
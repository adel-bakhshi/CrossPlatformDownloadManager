using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.Models;

/// <summary>
/// Represents a request from the browser extension.
/// </summary>
public class ExtensionRequest
{
    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates the URL of the download file.
    /// </summary>
    [JsonProperty("url")]
    public string? Url { get; set; }
    
    /// <summary>
    /// Gets or sets a value that indicates the referer of the download file.
    /// </summary>
    [JsonProperty("referer")]
    public string? Referer { get; set; }
    
    /// <summary>
    /// Gets or sets a value that indicates the page address that download started from it.
    /// </summary>
    [JsonProperty("pageAddress")]
    public string? PageAddress { get; set; }

    #endregion
}
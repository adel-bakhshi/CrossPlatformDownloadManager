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

    #endregion
}
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels;

/// <summary>
/// Represents the version of the application.
/// </summary>
public class AppVersion
{
    /// <summary>
    /// Gets or sets a value that indicates the version of the application.
    /// </summary>
    [JsonProperty("version")]
    public string Version { get; set; } = string.Empty;
}
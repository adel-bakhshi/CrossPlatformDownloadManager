using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.Models;

/// <summary>
/// Represents the response from the browser extension.
/// </summary>
public class ExtensionResponse
{
    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates whether the operation was successful.
    /// </summary>
    [JsonProperty("isSuccessful")]
    public bool IsSuccessful { get; set; }
    
    /// <summary>
    /// Gets or sets a value that indicates the response massage.
    /// </summary>
    [JsonProperty("message")]
    public string? Message { get; set; }

    #endregion
}

/// <summary>
/// Represents the response from the browser extension.
/// </summary>
/// <typeparam name="T">The type of the data that response contains.</typeparam>
public class ExtensionResponse<T> : ExtensionResponse where T : new()
{
    [JsonProperty("data")]
    public T? Data { get; set; }
}
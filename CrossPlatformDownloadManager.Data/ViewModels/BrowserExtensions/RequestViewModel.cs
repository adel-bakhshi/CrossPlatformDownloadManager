using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.BrowserExtensions;

public class RequestViewModel
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}
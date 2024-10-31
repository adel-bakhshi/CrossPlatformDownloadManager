using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.BrowserExtensions;

public class ResponseViewModel
{
    [JsonProperty("isSuccessful")]
    public bool IsSuccessful { get; set; }
    
    [JsonProperty("message")]
    public string? Message { get; set; }
}
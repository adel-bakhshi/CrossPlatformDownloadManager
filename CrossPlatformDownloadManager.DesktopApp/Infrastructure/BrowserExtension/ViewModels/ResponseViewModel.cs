using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.ViewModels;

public class ResponseViewModel
{
    #region Properties

    [JsonProperty("isSuccessful")]
    public bool IsSuccessful { get; set; }
    
    [JsonProperty("message")]
    public string? Message { get; set; }

    #endregion
}

public class ResponseViewModel<T> : ResponseViewModel where T : new()
{
    [JsonProperty("data")]
    public T? Data { get; set; }
}
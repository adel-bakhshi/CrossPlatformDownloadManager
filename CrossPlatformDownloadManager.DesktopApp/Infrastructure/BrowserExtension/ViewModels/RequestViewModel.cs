using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.BrowserExtension.ViewModels;

public class RequestViewModel
{
    #region Properties

    [JsonProperty("url")]
    public string? Url { get; set; }

    #endregion
}
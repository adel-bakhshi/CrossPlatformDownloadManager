using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService.Models;

public class ProxySettingsData
{
    #region Properties

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("host")]
    public string Host { get; set; } = string.Empty;

    [JsonProperty("port")]
    public string Port { get; set; } = string.Empty;

    [JsonProperty("userName")]
    public string? Username { get; set; }

    [JsonProperty("password")]
    public string? Password { get; set; }

    #endregion
}
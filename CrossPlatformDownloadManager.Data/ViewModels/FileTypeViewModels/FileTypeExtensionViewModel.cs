using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.FileTypeViewModels;

public class FileTypeExtensionViewModel
{
    [JsonProperty("extension")]
    [JsonPropertyName("extension")]
    public string? Extension { get; set; }

    [JsonProperty("alias")]
    [JsonPropertyName("alias")]
    public string? Alias { get; set; }
}
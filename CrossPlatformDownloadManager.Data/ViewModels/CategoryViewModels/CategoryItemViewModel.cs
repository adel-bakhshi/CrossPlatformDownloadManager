using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.CategoryViewModels;

public class CategoryItemViewModel
{
    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonProperty("icon")]
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
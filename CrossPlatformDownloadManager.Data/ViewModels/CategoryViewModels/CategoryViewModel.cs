using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.CategoryViewModels;

public class CategoryViewModel
{
    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonProperty("icon")]
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonProperty("items")]
    [JsonPropertyName("items")]
    public ObservableCollection<CategoryItemViewModel> Items { get; set; } = new ObservableCollection<CategoryItemViewModel>();
}
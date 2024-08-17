using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels.FileTypeViewModels;

public class FileTypeViewModel
{
    [JsonProperty("fileType")]
    [JsonPropertyName("fileType")]
    public string? FileType { get; set; }

    [JsonProperty("typeExtensions")]
    [JsonPropertyName("typeExtensions")]
    public ICollection<FileTypeExtensionViewModel> TypeExtensions { get; set; } =
        new List<FileTypeExtensionViewModel>();
}
using Newtonsoft.Json;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("CategoryItemFileExtensions")]
public class CategoryItemFileExtension
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] [JsonProperty("extension")] public string Extension { get; set; } = string.Empty;

    [NotNull] [JsonProperty("alias")] public string Alias { get; set; } = string.Empty;

    [Indexed] public int? CategoryItemId { get; set; }
}
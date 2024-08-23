using Newtonsoft.Json;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("CategoryFileExtensions")]
public class CategoryFileExtension
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] [JsonProperty("extension")] public string Extension { get; set; } = string.Empty;

    [NotNull] [JsonProperty("alias")] public string Alias { get; set; } = string.Empty;

    [Indexed] public int? CategoryId { get; set; }
    
    [Ignore] public Category? Category { get; set; }
}
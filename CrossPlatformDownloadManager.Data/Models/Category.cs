using Newtonsoft.Json;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("Categories")]
public class Category
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] [JsonProperty("title")] public string Title { get; set; } = "";

    [NotNull] [JsonProperty("icon")] public string Icon { get; set; } = "";

    public string? AutoAddLinkFromSites { get; set; }

    [Indexed] public int? CategoryItemSaveDirectoryId { get; set; }

    [Ignore]
    [JsonProperty("fileExtensions")]
    public ICollection<CategoryFileExtension> FileExtensions { get; set; } = new List<CategoryFileExtension>();

    [Ignore] public CategorySaveDirectory? CategorySaveDirectory { get; set; }
}
using Newtonsoft.Json;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("Categories")]
public class Category
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] [JsonProperty("title")] public string Title { get; set; } = "";

    [NotNull] [JsonProperty("icon")] public string Icon { get; set; } = "";

    [Ignore] public ICollection<CategoryItem> CategoryItems { get; set; } = new List<CategoryItem>();
}
using Newtonsoft.Json;
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("CategoryHeaders")]
public class CategoryHeader
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [NotNull] [JsonProperty("title")] public string Title { get; set; } = "";

    [NotNull] [JsonProperty("icon")] public string Icon { get; set; } = "";

    [Ignore] public ICollection<Category> Categories { get; set; } = new List<Category>();
}
using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("CategoryItemSaveDirectories")]
public class CategoryItemSaveDirectory
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int? CategoryItemId { get; set; }

    [NotNull] public string SaveDirectory { get; set; } = "";
    
    [Ignore]
    public CategoryItem? CategoryItem { get; set; }
}
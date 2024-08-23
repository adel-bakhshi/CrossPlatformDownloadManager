using SQLite;

namespace CrossPlatformDownloadManager.Data.Models;

[Table("CategorySaveDirectories")]
public class CategorySaveDirectory
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }

    [Indexed] public int? CategoryId { get; set; }

    [NotNull] public string SaveDirectory { get; set; } = "";
    
    [Ignore]
    public Category? Category { get; set; }
}
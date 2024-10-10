using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossPlatformDownloadManager.Data.Models;

public class CategorySaveDirectory : DbModelBase
{
    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))] public Category? Category { get; set; }

    [MaxLength(500)] public string SaveDirectory { get; set; } = string.Empty;

    public CategorySaveDirectory()
    {
    }

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not CategorySaveDirectory categorySaveDirectory)
            return;
        
        CategoryId = categorySaveDirectory.CategoryId;
        SaveDirectory = categorySaveDirectory.SaveDirectory;
    }
}
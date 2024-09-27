using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossPlatformDownloadManager.Data.Models;

public class CategorySaveDirectory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))] public Category? Category { get; set; }

    [MaxLength(500)] public string SaveDirectory { get; set; } = string.Empty;

    public CategorySaveDirectory()
    {
    }

    public void UpdateData(CategorySaveDirectory categorySaveDirectory)
    {
        CategoryId = categorySaveDirectory.CategoryId;
        SaveDirectory = categorySaveDirectory.SaveDirectory;
    }
}
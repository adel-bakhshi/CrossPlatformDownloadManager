using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class CategoryFileExtension : DbModelBase
{
    [Required]
    [MaxLength(100)]
    [JsonProperty("extension")]
    public string Extension { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [JsonProperty("alias")]
    public string Alias { get; set; } = string.Empty;

    public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))] public Category? Category { get; set; }

    public CategoryFileExtension()
    {
    }
    
    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not CategoryFileExtension categoryFileExtension)
            return;
        
        Extension = categoryFileExtension.Extension;
        Alias = categoryFileExtension.Alias;
        CategoryId = categoryFileExtension.CategoryId;
    }
}
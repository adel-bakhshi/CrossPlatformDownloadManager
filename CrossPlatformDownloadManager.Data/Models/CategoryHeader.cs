using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class CategoryHeader : DbModelBase
{
    [Required]
    [MaxLength(100)]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    [JsonProperty("icon")]
    public string Icon { get; set; } = string.Empty;

    [NotMapped] public ICollection<Category> Categories { get; set; } = [];

    public CategoryHeader()
    {
    }

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not CategoryHeader categoryHeader)
            return;
        
        Title = categoryHeader.Title;
        Icon = categoryHeader.Icon;
        
        Categories.Clear();
    }
}
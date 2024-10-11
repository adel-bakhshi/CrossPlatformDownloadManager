using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class Category : DbModelBase
{
    [Required]
    [MaxLength(100)]
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(300)]
    [JsonProperty("icon")]
    public string Icon { get; set; } = string.Empty;

    [Required] [JsonProperty("isDefault")] public bool IsDefault { get; set; }

    [MaxLength(1000)] public string? AutoAddLinkFromSites { get; set; }

    public int? CategorySaveDirectoryId { get; set; }

    [ForeignKey(nameof(CategorySaveDirectoryId))]
    public CategorySaveDirectory? CategorySaveDirectory { get; set; }

    [JsonProperty("fileExtensions")] public ICollection<CategoryFileExtension> FileExtensions { get; set; } = [];

    public ICollection<DownloadFile> DownloadFiles { get; } = [];
    
    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not Category category)
            return;
        
        Title = category.Title;
        Icon = category.Icon;
        IsDefault = category.IsDefault;
        AutoAddLinkFromSites = category.AutoAddLinkFromSites;
        CategorySaveDirectoryId = category.CategorySaveDirectoryId;
    }
}
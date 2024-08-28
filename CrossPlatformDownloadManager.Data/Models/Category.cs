using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] [MaxLength(100)] [JsonProperty("title")] public string Title { get; set; } = "";

    [Required] [MaxLength(300)] [JsonProperty("icon")] public string Icon { get; set; } = "";

    [Required] [JsonProperty("isDefault")] public bool IsDefault { get; set; }

    [MaxLength(500)] public string? AutoAddLinkFromSites { get; set; }

    public int? CategorySaveDirectoryId { get; set; }

    [ForeignKey(nameof(CategorySaveDirectoryId))]
    public CategorySaveDirectory? CategorySaveDirectory { get; set; }

    [JsonProperty("fileExtensions")]
    public ICollection<CategoryFileExtension> FileExtensions { get; set; } = new List<CategoryFileExtension>();

    public ICollection<DownloadFile> DownloadFiles { get; set; } = new List<DownloadFile>();
}
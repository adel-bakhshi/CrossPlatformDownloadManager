using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class CategoryFileExtension
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

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

    public void UpdateData(CategoryFileExtension categoryFileExtension)
    {
        Extension = categoryFileExtension.Extension;
        Alias = categoryFileExtension.Alias;
        CategoryId = categoryFileExtension.CategoryId;
    }
}
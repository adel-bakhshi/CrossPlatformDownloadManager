using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class CategoryHeader
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] [MaxLength(100)] [JsonProperty("title")] public string Title { get; set; } = "";

    [Required] [MaxLength(300)] [JsonProperty("icon")] public string Icon { get; set; } = "";

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
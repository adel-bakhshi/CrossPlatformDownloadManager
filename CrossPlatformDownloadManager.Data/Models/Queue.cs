using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossPlatformDownloadManager.Data.Models;

public class Queue
{
    [Key]
    [DatabaseGenerated((DatabaseGeneratedOption.Identity))]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = "";

    public ICollection<DownloadFile> DownloadFiles { get; set; } = new List<DownloadFile>();
}
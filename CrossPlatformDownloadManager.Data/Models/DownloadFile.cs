using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossPlatformDownloadManager.Data.Models;

public class DownloadFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public string FileName { get; set; } = "";

    [Required] public DownloadFileType FileType { get; set; }

    public int? QueueId { get; set; }

    [ForeignKey(nameof(QueueId))] public Queue? Queue { get; set; }

    [Required] public double Size { get; set; }

    public DownloadStatus? Status { get; set; }

    public DateTime? LastTryDate { get; set; }

    [Required] public DateTime DateAdded { get; set; }

    [NotMapped] public TimeSpan? TimeLeft { get; set; }

    [NotMapped] public double? TransferRate { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Data.Models;

public class DownloadFile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] [MaxLength(300)] public string Url { get; set; } = "";

    [Required] [MaxLength(300)] public string FileName { get; set; } = "";

    public int? DownloadQueueId { get; set; }

    [ForeignKey(nameof(DownloadQueueId))] public DownloadQueue? DownloadQueue { get; set; }

    [Required] public double Size { get; set; }

    [MaxLength(300)] public string? Description { get; set; }

    public DownloadStatus? Status { get; set; }

    public DateTime? LastTryDate { get; set; }

    [Required] public DateTime DateAdded { get; set; }

    public int? QueuePriority { get; set; }

    [Required] public int? CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))] public Category? Category { get; set; }

    [Required] public bool IsPaused { get; set; } = false;

    [Required] public bool IsError { get; set; } = false;

    [Required] public float DownloadProgress { get; set; } = 0.0f;

    public TimeSpan? ElapsedTime { get; set; }

    public TimeSpan? TimeLeft { get; set; }

    public float? TransferRate { get; set; }

    [Required] [MaxLength(500)] public string SaveLocation { get; set; } = "";
    
    [MaxLength(3000)]
    public string? DownloadPackage { get; set; }
}
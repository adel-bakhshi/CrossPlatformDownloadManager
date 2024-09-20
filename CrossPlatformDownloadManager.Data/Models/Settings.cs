using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Data.Models;

public class Settings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] public bool StartOnSystemStartup { get; set; }

    [Required] public bool UseBrowserExtension { get; set; }

    [Required] public bool DarkMode { get; set; }

    [Required] public bool ShowStartDownloadDialog { get; set; }

    [Required] public bool ShowCompleteDownloadDialog { get; set; }

    [Required] [MaxLength(500)] public string DuplicateDownloadLinkAction { get; set; } = string.Empty;

    [Required] public int MaximumConnectionsCount { get; set; }

    [Required] public ProxyMode ProxyMode { get; set; }

    [Required] public ProxyType ProxyType { get; set; }

    [Required] [MaxLength(1000)] public string CustomProxySettings { get; set; } = string.Empty;

    [Required] public bool UseDownloadCompleteSound { get; set; }

    [Required] public bool UseDownloadStoppedSound { get; set; }

    [Required] public bool UseDownloadFailedSound { get; set; }

    [Required] public bool UseQueueStartedSound { get; set; }

    [Required] public bool UseQueueStoppedSound { get; set; }

    [Required] public bool UseQueueFinishedSound { get; set; }

    [Required] public bool UseSystemNotifications { get; set; }
}
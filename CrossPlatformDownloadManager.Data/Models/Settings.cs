using System.ComponentModel.DataAnnotations;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.Data.Models;

public class Settings : DbModelBase
{
    [Required] public bool StartOnSystemStartup { get; set; }

    [Required] public bool UseBrowserExtension { get; set; }

    [Required] public bool DarkMode { get; set; }

    [Required] public bool ShowStartDownloadDialog { get; set; }

    [Required] public bool ShowCompleteDownloadDialog { get; set; }

    [Required] [MaxLength(500)] public string DuplicateDownloadLinkAction { get; set; } = string.Empty;

    [Required] public int MaximumConnectionsCount { get; set; }

    [Required] public ProxyMode ProxyMode { get; set; }

    [Required] public ProxyType ProxyType { get; set; }

    [Required] public bool UseDownloadCompleteSound { get; set; }

    [Required] public bool UseDownloadStoppedSound { get; set; }

    [Required] public bool UseDownloadFailedSound { get; set; }

    [Required] public bool UseQueueStartedSound { get; set; }

    [Required] public bool UseQueueStoppedSound { get; set; }

    [Required] public bool UseQueueFinishedSound { get; set; }

    [Required] public bool UseSystemNotifications { get; set; }

    public ICollection<ProxySettings> Proxies { get; } = [];

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not Settings settings)
            return;

        StartOnSystemStartup = settings.StartOnSystemStartup;
        UseBrowserExtension = settings.UseBrowserExtension;
        DarkMode = settings.DarkMode;
        ShowStartDownloadDialog = settings.ShowStartDownloadDialog;
        ShowCompleteDownloadDialog = settings.ShowCompleteDownloadDialog;
        DuplicateDownloadLinkAction = settings.DuplicateDownloadLinkAction;
        MaximumConnectionsCount = settings.MaximumConnectionsCount;
        ProxyMode = settings.ProxyMode;
        ProxyType = settings.ProxyType;
        UseDownloadCompleteSound = settings.UseDownloadCompleteSound;
        UseDownloadStoppedSound = settings.UseDownloadStoppedSound;
        UseDownloadFailedSound = settings.UseDownloadFailedSound;
        UseQueueStartedSound = settings.UseQueueStartedSound;
        UseQueueStoppedSound = settings.UseQueueStoppedSound;
        UseQueueFinishedSound = settings.UseQueueFinishedSound;
        UseSystemNotifications = settings.UseSystemNotifications;
    }
}
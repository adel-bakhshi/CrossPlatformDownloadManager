using CrossPlatformDownloadManager.Utils.Enums;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

[AddINotifyPropertyChangedInterface]
public class SettingsViewModel
{
    public int Id { get; set; }
    public bool StartOnSystemStartup { get; set; }
    public bool UseBrowserExtension { get; set; }
    public bool DarkMode { get; set; }
    public bool ShowStartDownloadDialog { get; set; }
    public bool ShowCompleteDownloadDialog { get; set; }
    public string DuplicateDownloadLinkAction { get; set; } = string.Empty;
    public int MaximumConnectionsCount { get; set; }
    public ProxyMode ProxyMode { get; set; }
    public ProxyType ProxyType { get; set; }
    public string CustomProxySettings { get; set; } = string.Empty;
    public bool UseDownloadCompleteSound { get; set; }
    public bool UseDownloadStoppedSound { get; set; }
    public bool UseDownloadFailedSound { get; set; }
    public bool UseQueueStartedSound { get; set; }
    public bool UseQueueStoppedSound { get; set; }
    public bool UseQueueFinishedSound { get; set; }
    public bool UseSystemNotifications { get; set; }
}
using System.ComponentModel.DataAnnotations;
using CrossPlatformDownloadManager.Utils.Enums;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.Models;

public class Settings : DbModelBase
{
    [Required]
    [JsonProperty("startOnSystemStartup")]
    public bool StartOnSystemStartup { get; set; }

    [Required]
    [JsonProperty("useBrowserExtension")]
    public bool UseBrowserExtension { get; set; }

    [Required]
    [MaxLength(150)]
    [JsonProperty("themeFilePath")]
    public string? ThemeFilePath { get; set; }

    [Required]
    [JsonProperty("useManager")]
    public bool UseManager { get; set; }

    [Required]
    [JsonProperty("alwaysKeepManagerOnTop")]
    public bool AlwaysKeepManagerOnTop { get; set; }

    [MaxLength(50)]
    [Required]
    [JsonProperty("applicationFont")]
    public string? ApplicationFont { get; set; }

    [JsonProperty("disableCategories")]
    public bool DisableCategories { get; set; }

    [MaxLength(500)]
    [JsonProperty("globalSaveLocation")]
    public string? GlobalSaveLocation { get; set; }

    [Required]
    [JsonProperty("showStartDownloadDialog")]
    public bool ShowStartDownloadDialog { get; set; }

    [Required]
    [JsonProperty("showCompleteDownloadDialog")]
    public bool ShowCompleteDownloadDialog { get; set; }

    [Required]
    [JsonProperty("duplicateDownloadLinkAction")]
    public DuplicateDownloadLinkAction DuplicateDownloadLinkAction { get; set; }

    [Required]
    [JsonProperty("maximumConnectionsCount")]
    public int MaximumConnectionsCount { get; set; }

    [Required]
    [JsonProperty("isSpeedLimiterEnabled")]
    public bool IsSpeedLimiterEnabled { get; set; }

    [JsonProperty("limitSpeed")]
    public double? LimitSpeed { get; set; }

    [MaxLength(50)]
    [JsonProperty("limitUnit")]
    public string? LimitUnit { get; set; }

    [Required]
    [JsonProperty("proxyMode")]
    public ProxyMode ProxyMode { get; set; }

    [Required]
    [JsonProperty("proxyType")]
    public ProxyType ProxyType { get; set; }

    [Required]
    [JsonProperty("useDownloadCompleteSound")]
    public bool UseDownloadCompleteSound { get; set; }

    [Required]
    [JsonProperty("useDownloadStoppedSound")]
    public bool UseDownloadStoppedSound { get; set; }

    [Required]
    [JsonProperty("useDownloadFailedSound")]
    public bool UseDownloadFailedSound { get; set; }

    [Required]
    [JsonProperty("useQueueStartedSound")]
    public bool UseQueueStartedSound { get; set; }

    [Required]
    [JsonProperty("useQueueStoppedSound")]
    public bool UseQueueStoppedSound { get; set; }

    [Required]
    [JsonProperty("useQueueFinishedSound")]
    public bool UseQueueFinishedSound { get; set; }

    [Required]
    [JsonProperty("useSystemNotifications")]
    public bool UseSystemNotifications { get; set; }

    [MaxLength(100)]
    public string? ManagerPoint { get; set; }

    [JsonProperty("showCategoriesPanel")]
    public bool ShowCategoriesPanel { get; set; } = true;

    [MaxLength(5000)]
    [JsonProperty("dataGridColumnsSettings")]
    public string? DataGridColumnsSettings { get; set; }

    public bool HasApplicationBeenRunYet { get; set; }

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<ProxySettings> Proxies { get; } = [];

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not Settings settings)
            return;

        StartOnSystemStartup = settings.StartOnSystemStartup;
        UseBrowserExtension = settings.UseBrowserExtension;
        ThemeFilePath = settings.ThemeFilePath;
        UseManager = settings.UseManager;
        AlwaysKeepManagerOnTop = settings.AlwaysKeepManagerOnTop;
        ApplicationFont = settings.ApplicationFont;
        DisableCategories = settings.DisableCategories;
        GlobalSaveLocation = settings.GlobalSaveLocation;
        ShowStartDownloadDialog = settings.ShowStartDownloadDialog;
        ShowCompleteDownloadDialog = settings.ShowCompleteDownloadDialog;
        DuplicateDownloadLinkAction = settings.DuplicateDownloadLinkAction;
        MaximumConnectionsCount = settings.MaximumConnectionsCount;
        IsSpeedLimiterEnabled = settings.IsSpeedLimiterEnabled;
        LimitSpeed = settings.LimitSpeed;
        LimitUnit = settings.LimitUnit;
        ProxyMode = settings.ProxyMode;
        ProxyType = settings.ProxyType;
        UseDownloadCompleteSound = settings.UseDownloadCompleteSound;
        UseDownloadStoppedSound = settings.UseDownloadStoppedSound;
        UseDownloadFailedSound = settings.UseDownloadFailedSound;
        UseQueueStartedSound = settings.UseQueueStartedSound;
        UseQueueStoppedSound = settings.UseQueueStoppedSound;
        UseQueueFinishedSound = settings.UseQueueFinishedSound;
        UseSystemNotifications = settings.UseSystemNotifications;
        ManagerPoint = settings.ManagerPoint;
        ShowCategoriesPanel = settings.ShowCategoriesPanel;
        DataGridColumnsSettings = settings.DataGridColumnsSettings;
        HasApplicationBeenRunYet = settings.HasApplicationBeenRunYet;
    }
}
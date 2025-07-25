using System.Collections.Generic;
using System.Linq;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.ExportImportService.Models;

public class SettingsData
{
    #region Properties

    [JsonProperty("startOnSystemStartup")]
    public bool StartOnSystemStartup { get; set; }

    [JsonProperty("useBrowserExtension")]
    public bool UseBrowserExtension { get; set; }

    [JsonProperty("useManager")]
    public bool UseManager { get; set; }

    [JsonProperty("alwaysKeepManagerOnTop")]
    public bool AlwaysKeepManagerOnTop { get; set; }

    [JsonProperty("applicationFont")]
    public string? ApplicationFont { get; set; }

    [JsonProperty("showStartDownloadDialog")]
    public bool ShowStartDownloadDialog { get; set; }

    [JsonProperty("showCompleteDownloadDialog")]
    public bool ShowCompleteDownloadDialog { get; set; }

    [JsonProperty("duplicateDownloadLinkAction")]
    public DuplicateDownloadLinkAction DuplicateDownloadLinkAction { get; set; }

    [JsonProperty("maximumConnectionsCount")]
    public int MaximumConnectionsCount { get; set; }

    [JsonProperty("isSpeedLimiterEnabled")]
    public bool IsSpeedLimiterEnabled { get; set; }

    [JsonProperty("limitSpeed")]
    public double? LimitSpeed { get; set; }

    [JsonProperty("limitUnit")]
    public string? LimitUnit { get; set; }

    [JsonProperty("isMergeSpeedLimitEnabled")]
    public bool IsMergeSpeedLimitEnabled { get; set; }

    [JsonProperty("mergeLimitSpeed")]
    public double? MergeLimitSpeed { get; set; }

    [JsonProperty("mergeLimitUnit")]
    public string? MergeLimitUnit { get; set; }

    [JsonProperty("maximumMemoryBufferBytes")]
    public long MaximumMemoryBufferBytes { get; set; }

    [JsonProperty("maximumMemoryBufferBytesUnit")]
    public string MaximumMemoryBufferBytesUnit { get; set; } = string.Empty;

    [JsonProperty("proxyMode")]
    public ProxyMode ProxyMode { get; set; }

    [JsonProperty("proxyType")]
    public ProxyType ProxyType { get; set; }

    [JsonProperty("useDownloadCompleteSound")]
    public bool UseDownloadCompleteSound { get; set; }

    [JsonProperty("useDownloadStoppedSound")]
    public bool UseDownloadStoppedSound { get; set; }

    [JsonProperty("useDownloadFailedSound")]
    public bool UseDownloadFailedSound { get; set; }

    [JsonProperty("useQueueStartedSound")]
    public bool UseQueueStartedSound { get; set; }

    [JsonProperty("useQueueStoppedSound")]
    public bool UseQueueStoppedSound { get; set; }

    [JsonProperty("useQueueFinishedSound")]
    public bool UseQueueFinishedSound { get; set; }

    [JsonProperty("useSystemNotifications")]
    public bool UseSystemNotifications { get; set; }

    [JsonProperty("showCategoriesPanel")]
    public bool ShowCategoriesPanel { get; set; }

    [JsonProperty("dataGridColumnsSettings")]
    public string? DataGridColumnsSettings { get; set; }

    [JsonProperty("proxies")]
    public List<ProxySettingsData> Proxies { get; set; } = [];

    #endregion

    public static SettingsData CreateExportFile(SettingsViewModel settings, List<ProxySettingsViewModel> proxies)
    {
        var exportProxies = proxies
            .Where(p => !p.Name.IsStringNullOrEmpty() && !p.Type.IsStringNullOrEmpty() && !p.Host.IsStringNullOrEmpty() && !p.Port.IsStringNullOrEmpty())
            .Select(p => new ProxySettingsData
            {
                Name = p.Name!,
                Type = p.Type!,
                Host = p.Host!,
                Port = p.Port!,
                Username = p.Username,
                Password = p.Password
            })
            .ToList();

        return new SettingsData
        {
            StartOnSystemStartup = settings.StartOnSystemStartup,
            UseBrowserExtension = settings.UseBrowserExtension,
            ApplicationFont = settings.ApplicationFont,
            UseManager = settings.UseManager,
            AlwaysKeepManagerOnTop = settings.AlwaysKeepManagerOnTop,
            ShowStartDownloadDialog = settings.ShowStartDownloadDialog,
            ShowCompleteDownloadDialog = settings.ShowCompleteDownloadDialog,
            DuplicateDownloadLinkAction = settings.DuplicateDownloadLinkAction,
            MaximumConnectionsCount = settings.MaximumConnectionsCount,
            IsSpeedLimiterEnabled = settings.IsSpeedLimiterEnabled,
            LimitSpeed = settings.LimitSpeed,
            LimitUnit = settings.LimitUnit,
            IsMergeSpeedLimitEnabled = settings.IsMergeSpeedLimitEnabled,
            MergeLimitSpeed = settings.MergeLimitSpeed,
            MergeLimitUnit = settings.MergeLimitUnit,
            MaximumMemoryBufferBytes = settings.MaximumMemoryBufferBytes,
            MaximumMemoryBufferBytesUnit = settings.MaximumMemoryBufferBytesUnit,
            ProxyMode = settings.ProxyMode,
            ProxyType = settings.ProxyType,
            UseDownloadCompleteSound = settings.UseDownloadCompleteSound,
            UseDownloadStoppedSound = settings.UseDownloadStoppedSound,
            UseDownloadFailedSound = settings.UseDownloadFailedSound,
            UseQueueStartedSound = settings.UseQueueStartedSound,
            UseQueueStoppedSound = settings.UseQueueStoppedSound,
            UseQueueFinishedSound = settings.UseQueueFinishedSound,
            UseSystemNotifications = settings.UseSystemNotifications,
            ShowCategoriesPanel = settings.ShowCategoriesPanel,
            DataGridColumnsSettings = settings.DataGridColumnsSettings.ConvertToJson(),
            Proxies = exportProxies
        };
    }
}
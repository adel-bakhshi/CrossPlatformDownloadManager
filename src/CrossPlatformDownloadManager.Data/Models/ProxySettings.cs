using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossPlatformDownloadManager.Data.Models;

public class ProxySettings : DbModelBase
{
    #region Properties

    [Required] [MaxLength(50)] public string Name { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Type { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Host { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Port { get; set; } = string.Empty;

    [MaxLength(200)] public string? Username { get; set; }

    [MaxLength(200)] public string? Password { get; set; }

    public int? SettingsId { get; set; }

    [ForeignKey(nameof(SettingsId))] public Settings? Settings { get; set; }

    #endregion

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not ProxySettings proxySettings)
            return;

        Name = proxySettings.Name;
        Type = proxySettings.Type;
        Host = proxySettings.Host;
        Port = proxySettings.Port;
        Username = proxySettings.Username;
        Password = proxySettings.Password;
        SettingsId = proxySettings.SettingsId;
    }
}
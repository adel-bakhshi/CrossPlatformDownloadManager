using System.ComponentModel.DataAnnotations;

namespace CrossPlatformDownloadManager.Data.Models;

public class ProxySettings : DbModelBase
{
    #region Properties

    [Required] [MaxLength(50)] public string Title { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Type { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Host { get; set; } = string.Empty;

    [Required] [MaxLength(50)] public string Port { get; set; } = string.Empty;

    [Required] [MaxLength(200)] public string Username { get; set; } = string.Empty;

    [Required] [MaxLength(200)] public string Password { get; set; } = string.Empty;

    #endregion

    public override void UpdateDbModel(DbModelBase? model)
    {
        if (model is not ProxySettings proxySettings)
            return;

        Title = proxySettings.Title;
        Type = proxySettings.Type;
        Host = proxySettings.Host;
        Port = proxySettings.Port;
        Username = proxySettings.Username;
        Password = proxySettings.Password;
    }
}
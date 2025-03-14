using System.Net;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ProxySettingsViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string? _name;
    private string? _type;
    private string? _host;
    private string? _port;
    private string? _username;
    private string? _password;
    private int? _settingsId;
    private bool _isActive;
    private bool _isResponsive;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string? Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string? Type
    {
        get => _type;
        set => SetField(ref _type, value);
    }

    public string? Host
    {
        get => _host;
        set => SetField(ref _host, value);
    }

    public string? Port
    {
        get => _port;
        set => SetField(ref _port, value);
    }

    public string? Username
    {
        get => _username;
        set => SetField(ref _username, value);
    }

    public string? Password
    {
        get => _password;
        set => SetField(ref _password, value);
    }

    public int? SettingsId
    {
        get => _settingsId;
        set => SetField(ref _settingsId, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetField(ref _isActive, value);
    }

    public bool IsResponsive
    {
        get => _isResponsive;
        set => SetField(ref _isResponsive, value);
    }

    #endregion

    public async Task CheckIsResponsiveAsync()
    {
        var proxyUri = GetProxyUri();
        IsResponsive = await CheckProxyAsync(proxyUri);
    }

    public string GetProxyUri()
    {
        var type = Type?.ToLower();
        return type switch
        {
            "http" => $"{type}://{Host}:{Port}",
            "https" => $"{type}://{Host}:{Port}",
            "socks 5" => $"{type.Replace(" ", "")}://{Host}:{Port}",
            _ => throw new InvalidOperationException("Invalid proxy type.")
        };
    }

    #region Helpers

    private static async Task<bool> CheckProxyAsync(string proxyUri)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                Proxy = new WebProxy(proxyUri),
                UseProxy = true
            };

            using var client = new HttpClient(handler);
            var response = await client.GetAsync("https://www.google.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
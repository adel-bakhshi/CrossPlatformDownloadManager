using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class ProxySettingsViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string? _title;
    private string? _type;
    private string? _host;
    private string? _port;
    private string? _username;
    private string? _password;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }
    
    public string? Title
    {
        get => _title;
        set => SetField(ref _title, value);
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

    #endregion
}
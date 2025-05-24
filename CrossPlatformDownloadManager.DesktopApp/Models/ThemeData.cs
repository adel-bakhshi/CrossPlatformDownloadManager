using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.DesktopApp.Models;

public class ThemeData : PropertyChangedBase
{
    #region Private fields

    private string? _themeName;
    private string? _themePath;

    #endregion

    #region Properties

    public string? ThemeName
    {
        get => _themeName;
        set => SetField(ref _themeName, value);
    }

    public string? ThemePath
    {
        get => _themePath;
        set => SetField(ref _themePath, value);
    }

    #endregion
}
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.DesktopApp.Models;

public class DownloadFilesDataGridShortcut : PropertyChangedBase
{
    #region Private Fields

    private string _title = string.Empty;
    private string _shortcut = string.Empty;
    private bool _isEven;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates the title of the shortcut.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the shortcut keys.
    /// </summary>
    public string Shortcut
    {
        get => _shortcut;
        set => SetField(ref _shortcut, value);
    }
    
    /// <summary>
    /// Gets or sets a value that indicates whether the row is even.
    /// </summary>
    public bool IsEven
    {
        get => _isEven;
        set => SetField(ref _isEven, value);
    }

    #endregion
}
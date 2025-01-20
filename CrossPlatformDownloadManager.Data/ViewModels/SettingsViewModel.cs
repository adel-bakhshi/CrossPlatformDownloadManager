using System.Collections.ObjectModel;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class SettingsViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private bool _startOnSystemStartup;
    private bool _useBrowserExtension;
    private bool _darkMode;
    private bool _alwaysKeepManagerOnTop;
    private bool _showStartDownloadDialog;
    private bool _showCompleteDownloadDialog;
    private DuplicateDownloadLinkAction _duplicateDownloadLinkAction;
    private int _maximumConnectionsCount;
    private bool _isSpeedLimiterEnabled;
    private double? _limitSpeed;
    private string? _limitUnit;
    private ProxyMode _proxyMode;
    private ProxyType _proxyType;
    private string _customProxySettings = string.Empty;
    private bool _useDownloadCompleteSound;
    private bool _useDownloadStoppedSound;
    private bool _useDownloadFailedSound;
    private bool _useQueueStartedSound;
    private bool _useQueueStoppedSound;
    private bool _useQueueFinishedSound;
    private bool _useSystemNotifications;
    private PointViewModel? _managerPoint;
    private bool _showCategoriesPanel = true;
    private MainDownloadFilesDataGridColumnsSettings _dataGridColumnsSettings = new();
    private ObservableCollection<ProxySettingsViewModel> _proxies = [];

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public bool StartOnSystemStartup
    {
        get => _startOnSystemStartup;
        set => SetField(ref _startOnSystemStartup, value);
    }

    public bool UseBrowserExtension
    {
        get => _useBrowserExtension;
        set => SetField(ref _useBrowserExtension, value);
    }

    public bool DarkMode
    {
        get => _darkMode;
        set => SetField(ref _darkMode, value);
    }

    public bool AlwaysKeepManagerOnTop
    {
        get => _alwaysKeepManagerOnTop;
        set => SetField(ref _alwaysKeepManagerOnTop, value);
    }

    public bool ShowStartDownloadDialog
    {
        get => _showStartDownloadDialog;
        set => SetField(ref _showStartDownloadDialog, value);
    }

    public bool ShowCompleteDownloadDialog
    {
        get => _showCompleteDownloadDialog;
        set => SetField(ref _showCompleteDownloadDialog, value);
    }

    public DuplicateDownloadLinkAction DuplicateDownloadLinkAction
    {
        get => _duplicateDownloadLinkAction;
        set => SetField(ref _duplicateDownloadLinkAction, value);
    }

    public int MaximumConnectionsCount
    {
        get => _maximumConnectionsCount;
        set => SetField(ref _maximumConnectionsCount, value);
    }

    public bool IsSpeedLimiterEnabled
    {
        get => _isSpeedLimiterEnabled;
        set => SetField(ref _isSpeedLimiterEnabled, value);
    }

    public double? LimitSpeed
    {
        get => _limitSpeed;
        set => SetField(ref _limitSpeed, value);
    }

    public string? LimitUnit
    {
        get => _limitUnit;
        set => SetField(ref _limitUnit, value);
    }

    public ProxyMode ProxyMode
    {
        get => _proxyMode;
        set => SetField(ref _proxyMode, value);
    }

    public ProxyType ProxyType
    {
        get => _proxyType;
        set => SetField(ref _proxyType, value);
    }

    public string CustomProxySettings
    {
        get => _customProxySettings;
        set => SetField(ref _customProxySettings, value);
    }

    public bool UseDownloadCompleteSound
    {
        get => _useDownloadCompleteSound;
        set => SetField(ref _useDownloadCompleteSound, value);
    }

    public bool UseDownloadStoppedSound
    {
        get => _useDownloadStoppedSound;
        set => SetField(ref _useDownloadStoppedSound, value);
    }

    public bool UseDownloadFailedSound
    {
        get => _useDownloadFailedSound;
        set => SetField(ref _useDownloadFailedSound, value);
    }

    public bool UseQueueStartedSound
    {
        get => _useQueueStartedSound;
        set => SetField(ref _useQueueStartedSound, value);
    }

    public bool UseQueueStoppedSound
    {
        get => _useQueueStoppedSound;
        set => SetField(ref _useQueueStoppedSound, value);
    }

    public bool UseQueueFinishedSound
    {
        get => _useQueueFinishedSound;
        set => SetField(ref _useQueueFinishedSound, value);
    }

    public bool UseSystemNotifications
    {
        get => _useSystemNotifications;
        set => SetField(ref _useSystemNotifications, value);
    }

    public PointViewModel? ManagerPoint
    {
        get => _managerPoint;
        set => SetField(ref _managerPoint, value);
    }

    public bool ShowCategoriesPanel
    {
        get => _showCategoriesPanel;
        set => SetField(ref _showCategoriesPanel, value);
    }
    
    public MainDownloadFilesDataGridColumnsSettings DataGridColumnsSettings
    {
        get => _dataGridColumnsSettings;
        set => SetField(ref _dataGridColumnsSettings, value);
    }

    public ObservableCollection<ProxySettingsViewModel> Proxies
    {
        get => _proxies;
        set => SetField(ref _proxies, value);
    }

    #endregion
}
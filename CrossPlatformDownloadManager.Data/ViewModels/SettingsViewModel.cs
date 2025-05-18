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
    private string? _themeFilePath;
    private bool _useManager;
    private bool _alwaysKeepManagerOnTop;
    private string? _applicationFont;
    private bool _disableCategories;
    private string? _globalSaveLocation;
    private bool _showStartDownloadDialog;
    private bool _showCompleteDownloadDialog;
    private DuplicateDownloadLinkAction _duplicateDownloadLinkAction;
    private int _maximumConnectionsCount;
    private bool _isSpeedLimiterEnabled;
    private double? _limitSpeed;
    private string? _limitUnit;
    private bool _isMergeSpeedLimitEnabled;
    private double? _mergeLimitSpeed;
    private string? _mergeLimitUnit;
    private long _maximumMemoryBufferBytes;
    private string _maximumMemoryBufferBytesUnit = string.Empty;
    private ProxyMode _proxyMode;
    private ProxyType _proxyType;
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
    private bool _hasApplicationBeenRunYet;
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

    public string? ThemeFilePath
    {
        get => _themeFilePath;
        set => SetField(ref _themeFilePath, value);
    }

    public bool UseManager
    {
        get => _useManager;
        set => SetField(ref _useManager, value);
    }

    public bool AlwaysKeepManagerOnTop
    {
        get => _alwaysKeepManagerOnTop;
        set => SetField(ref _alwaysKeepManagerOnTop, value);
    }

    public string? ApplicationFont
    {
        get => _applicationFont;
        set => SetField(ref _applicationFont, value);
    }

    public bool DisableCategories
    {
        get => _disableCategories;
        set => SetField(ref _disableCategories, value);
    }

    public string? GlobalSaveLocation
    {
        get => _globalSaveLocation;
        set => SetField(ref _globalSaveLocation, value);
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

    public bool IsMergeSpeedLimitEnabled
    {
        get => _isMergeSpeedLimitEnabled;
        set => SetField(ref _isMergeSpeedLimitEnabled, value);
    }

    public double? MergeLimitSpeed
    {
        get => _mergeLimitSpeed;
        set => SetField(ref _mergeLimitSpeed, value);
    }

    public string? MergeLimitUnit
    {
        get => _mergeLimitUnit;
        set => SetField(ref _mergeLimitUnit, value);
    }

    public long MaximumMemoryBufferBytes
    {
        get => _maximumMemoryBufferBytes;
        set => SetField(ref _maximumMemoryBufferBytes, value);
    }

    public string MaximumMemoryBufferBytesUnit
    {
        get => _maximumMemoryBufferBytesUnit;
        set => SetField(ref _maximumMemoryBufferBytesUnit, value);
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

    public bool HasApplicationBeenRunYet
    {
        get => _hasApplicationBeenRunYet;
        set => SetField(ref _hasApplicationBeenRunYet, value);
    }

    public ObservableCollection<ProxySettingsViewModel> Proxies
    {
        get => _proxies;
        set => SetField(ref _proxies, value);
    }

    #endregion
}
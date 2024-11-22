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
    private bool _showStartDownloadDialog;
    private bool _showCompleteDownloadDialog;
    private string _duplicateDownloadLinkAction = string.Empty;
    private int _maximumConnectionsCount;
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

    public string DuplicateDownloadLinkAction
    {
        get => _duplicateDownloadLinkAction;
        set => SetField(ref _duplicateDownloadLinkAction, value);
    }

    public int MaximumConnectionsCount
    {
        get => _maximumConnectionsCount;
        set => SetField(ref _maximumConnectionsCount, value);
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
    
    public ObservableCollection<ProxySettingsViewModel> Proxies
    {
        get => _proxies;
        set => SetField(ref _proxies, value);
    }

    #endregion

    public void UpdateData(SettingsViewModel? viewModel)
    {
        if (viewModel == null)
            return;
        
        Id = viewModel.Id;
        StartOnSystemStartup = viewModel.StartOnSystemStartup;
        UseBrowserExtension = viewModel.UseBrowserExtension;
        DarkMode = viewModel.DarkMode;
        ShowStartDownloadDialog = viewModel.ShowStartDownloadDialog;
        ShowCompleteDownloadDialog = viewModel.ShowCompleteDownloadDialog;
        DuplicateDownloadLinkAction = viewModel.DuplicateDownloadLinkAction;
        MaximumConnectionsCount = viewModel.MaximumConnectionsCount;
        ProxyMode = viewModel.ProxyMode;
        ProxyType = viewModel.ProxyType;
        CustomProxySettings = viewModel.CustomProxySettings;
        UseDownloadCompleteSound = viewModel.UseDownloadCompleteSound;
        UseDownloadStoppedSound = viewModel.UseDownloadStoppedSound;
        UseDownloadFailedSound = viewModel.UseDownloadFailedSound;
        UseQueueStartedSound = viewModel.UseQueueStartedSound;
        UseQueueStoppedSound = viewModel.UseQueueStoppedSound;
        UseQueueFinishedSound = viewModel.UseQueueFinishedSound;
        UseSystemNotifications = viewModel.UseSystemNotifications;
        
        // These proxies no longer exist so they should be removed.
        var removedProxies = Proxies
            .Where(p => viewModel.Proxies.FirstOrDefault(vp => vp.Id == p.Id) == null)
            .ToList();

        foreach (var proxy in removedProxies)
            Proxies.Remove(proxy);

        foreach (var proxy in viewModel.Proxies)
        {
            var proxyInList = Proxies.FirstOrDefault(p => p.Id == proxy.Id);
            
            // The proxy was not there before and was recently added, so it must be added to the list of proxies.
            if (proxyInList == null)
                Proxies.Add(proxy);
            // The proxy is in the list and just needs to be filled in with new information.
            else
                proxyInList.UpdateData(proxy);
        }
    }
}
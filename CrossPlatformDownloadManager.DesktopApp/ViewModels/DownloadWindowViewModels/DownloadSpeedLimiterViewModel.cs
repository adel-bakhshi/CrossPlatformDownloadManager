using System;
using System.Collections.ObjectModel;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.DownloadWindowViewModels;

public class DownloadSpeedLimiterViewModel : ViewModelBase
{
    #region Private Fields

    private readonly Debouncer _speedLimitDebouncer;

    private bool _isSpeedLimiterEnabled;
    private double? _speedLimit;
    private ObservableCollection<string> _speedUnits = [];
    private string? _selectedSpeedUnit;
    private string? _speedLimitInfo;

    #endregion

    #region Properties

    public bool IsSpeedLimiterEnabled
    {
        get => _isSpeedLimiterEnabled;
        set
        {
            this.RaiseAndSetIfChanged(ref _isSpeedLimiterEnabled, value);
            if (!value)
                SpeedLimit = null;

            ChangeSpeedLimitInfo();
            RaiseSpeedLimiterChanged();
        }
    }

    public double? SpeedLimit
    {
        get => _speedLimit;
        set
        {
            this.RaiseAndSetIfChanged(ref _speedLimit, value);
            ChangeSpeedLimitInfo();

            // Run speed limit debouncer
            _speedLimitDebouncer.Run(RaiseSpeedLimiterChanged);
        }
    }

    public ObservableCollection<string> SpeedUnits
    {
        get => _speedUnits;
        set => this.RaiseAndSetIfChanged(ref _speedUnits, value);
    }

    public string? SelectedSpeedUnit
    {
        get => _selectedSpeedUnit;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSpeedUnit, value);
            ChangeSpeedLimitInfo();

            // Run speed limit debouncer
            _speedLimitDebouncer.Run(RaiseSpeedLimiterChanged);
        }
    }

    public string? SpeedLimitInfo
    {
        get => _speedLimitInfo;
        set => this.RaiseAndSetIfChanged(ref _speedLimitInfo, value);
    }

    #endregion

    #region Events

    public event EventHandler<SpeedLimiterChangedEventArgs>? SpeedLimiterChanged;

    #endregion

    public DownloadSpeedLimiterViewModel(IAppService appService) : base(appService)
    {
        _speedLimitDebouncer = new Debouncer(TimeSpan.FromSeconds(2));

        SpeedUnits = Constants.SpeedLimiterUnits.ToObservableCollection();
        SelectedSpeedUnit = SpeedUnits.FirstOrDefault();
    }

    #region Helpers

    private void RaiseSpeedLimiterChanged()
    {
        SpeedLimiterChanged?.Invoke(this, new SpeedLimiterChangedEventArgs
        {
            Enabled = IsSpeedLimiterEnabled,
            Speed = SpeedLimit,
            Unit = SelectedSpeedUnit
        });
    }

    private void ChangeSpeedLimitInfo()
    {
        if (!IsSpeedLimiterEnabled || SpeedLimit == null || SpeedLimit <= 0)
        {
            SpeedLimitInfo = "Speed limiter is disabled";
            return;
        }

        SpeedLimitInfo = $"Your download speed is limited to a maximum of {SpeedLimit} {SelectedSpeedUnit}/s";
    }

    #endregion
}
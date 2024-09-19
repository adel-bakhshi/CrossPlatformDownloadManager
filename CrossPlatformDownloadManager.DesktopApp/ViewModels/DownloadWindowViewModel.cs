using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    // Speed Limiter
    private bool _isSpeedLimiterEnabled;
    private double? _limitSpeed;
    private string? _speedUnit;

    // Options
    private bool _openFolderAfterDownloadFinished;
    private bool _exitProgramAfterDownloadFinished;
    private bool _turnOffComputerAfterDownloadFinished;
    private string? _turnOffComputerMode;

    #endregion

    #region Properties

    private bool _showStatusView;

    public bool ShowStatusView
    {
        get => _showStatusView;
        set => this.RaiseAndSetIfChanged(ref _showStatusView, value);
    }

    private bool _showSpeedLimiterView;

    public bool ShowSpeedLimiterView
    {
        get => _showSpeedLimiterView;
        set => this.RaiseAndSetIfChanged(ref _showSpeedLimiterView, value);
    }

    private bool _showOptionsView;

    public bool ShowOptionsView
    {
        get => _showOptionsView;
        set => this.RaiseAndSetIfChanged(ref _showOptionsView, value);
    }

    private ObservableCollection<string> _speedLimiterUnits = [];

    public ObservableCollection<string> SpeedLimiterUnits
    {
        get => _speedLimiterUnits;
        set => this.RaiseAndSetIfChanged(ref _speedLimiterUnits, value);
    }

    private ObservableCollection<string> _optionsTurnOffModes = [];

    public ObservableCollection<string> OptionsTurnOffModes
    {
        get => _optionsTurnOffModes;
        set => this.RaiseAndSetIfChanged(ref _optionsTurnOffModes, value);
    }

    private DownloadFileViewModel _downloadFile = new();

    public DownloadFileViewModel DownloadFile
    {
        get => _downloadFile;
        set => this.RaiseAndSetIfChanged(ref _downloadFile, value);
    }

    private bool _isPaused;

    public bool IsPaused
    {
        get => _isPaused;
        set => this.RaiseAndSetIfChanged(ref _isPaused, value);
    }

    #endregion

    #region Commands

    public ICommand ChangeViewCommand { get; }

    public ICommand ResumePauseDownloadCommand { get; }

    public ICommand CancelDownloadCommand { get; }

    #endregion

    public DownloadWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper,
        DownloadFileViewModel downloadFile) : base(unitOfWork, downloadFileService, mapper)
    {
        DownloadFile = downloadFile;
        ShowStatusView = true;
        SpeedLimiterUnits = Constants.SpeedLimiterUnits.ToObservableCollection();
        OptionsTurnOffModes = Constants.TurnOffComputerModes.ToObservableCollection();

        ChangeViewCommand = ReactiveCommand.Create<object?>(ChangeView);
        ResumePauseDownloadCommand = ReactiveCommand.Create(ResumePauseDownload);
        CancelDownloadCommand = ReactiveCommand.Create<Window?>(CancelDownload);
    }

    private async void CancelDownload(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            await DownloadFileService.StopDownloadFileAsync(DownloadFile);
            owner.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void ResumePauseDownload()
    {
        // TODO: Show message box
        try
        {
            if (IsPaused)
            {
                DownloadFileService.ResumeDownloadFile(DownloadFile);
                IsPaused = false;
            }
            else
            {
                DownloadFileService.PauseDownloadFile(DownloadFile);
                IsPaused = true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task StartDownloadAsync(Window? window)
    {
        // TODO: Show message box
        try
        {
            await DownloadFileService.StartDownloadFileAsync(DownloadFile, window);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void ChangeView(object? obj)
    {
        var buttonName = (obj as ToggleButton)?.Name;
        switch (buttonName)
        {
            case "BtnStatus":
            {
                ChangeViewsVisibility(nameof(ShowStatusView));
                break;
            }

            case "BtnSpeedLimiter":
            {
                ChangeViewsVisibility(nameof(ShowSpeedLimiterView));
                break;
            }

            case "BtnOptions":
            {
                ChangeViewsVisibility(nameof(ShowOptionsView));
                break;
            }
        }
    }

    private void ChangeViewsVisibility(string propName)
    {
        ShowStatusView = ShowSpeedLimiterView = ShowOptionsView = false;
        GetType().GetProperty(propName)?.SetValue(this, true);
    }

    public void ChangeSpeedLimiterState(DownloadSpeedLimiterViewEventArgs eventArgs)
    {
        _isSpeedLimiterEnabled = eventArgs.Enabled;
        _limitSpeed = _isSpeedLimiterEnabled ? eventArgs.Speed : null;
        _speedUnit = _isSpeedLimiterEnabled ? eventArgs.Unit : null;

        if (_isSpeedLimiterEnabled)
        {
            var unit = _speedUnit.IsNullOrEmpty() ? 0 :
                _speedUnit!.Equals("KB", StringComparison.OrdinalIgnoreCase) ? Constants.KB : Constants.MB;
            var speed = (long)(_limitSpeed == null ? 0 : _limitSpeed.Value * unit);
            DownloadFileService.LimitDownloadFileSpeed(DownloadFile, speed);
        }
        else
        {
            DownloadFileService.LimitDownloadFileSpeed(DownloadFile, 0);
        }
    }

    public void ChangeOptions(DownloadOptionsViewEventArgs eventArgs)
    {
        _openFolderAfterDownloadFinished = eventArgs.OpenFolderAfterDownloadFinished;
        _exitProgramAfterDownloadFinished = eventArgs.ExitProgramAfterDownloadFinished;
        _turnOffComputerAfterDownloadFinished = eventArgs.TurnOffComputerAfterDownloadFinished;
        _turnOffComputerMode = eventArgs.TurnOffComputerMode;
    }
}
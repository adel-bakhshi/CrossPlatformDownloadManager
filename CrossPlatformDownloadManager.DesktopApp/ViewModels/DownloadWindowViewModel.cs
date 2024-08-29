using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using Downloader;
using ReactiveUI;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    // DownloadService
    private DownloadService? _downloadService;
    
    // ElapsedTime timer
    private DispatcherTimer? _elapsedTimeTimer;
    private TimeSpan? _elapsedTime;

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

    private ObservableCollection<ChunkDataViewModel> _chunksData = [];

    public ObservableCollection<ChunkDataViewModel> ChunksData
    {
        get => _chunksData;
        set => this.RaiseAndSetIfChanged(ref _chunksData, value);
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

    #endregion

    public DownloadWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService,
        DownloadFileViewModel downloadFile) : base(unitOfWork, downloadFileService)
    {
        DownloadFile = downloadFile;
        ShowStatusView = true;
        SpeedLimiterUnits = Constants.SpeedLimiterUnits.ToObservableCollection();
        OptionsTurnOffModes = Constants.TurnOffComputerModes.ToObservableCollection();

        ChangeViewCommand = ReactiveCommand.Create<object?>(ChangeView);
        ResumePauseDownloadCommand = ReactiveCommand.Create(ResumePauseDownload);
    }

    private void ResumePauseDownload()
    {
        if (_downloadService == null)
            return;

        if (IsPaused)
        {
            _downloadService.Resume();
            IsPaused = false;
            _elapsedTimeTimer?.Start();
        }
        else
        {
            _downloadService.Pause();
            IsPaused = true;
            _elapsedTimeTimer?.Stop();
        }
    }

    public async Task StartDownloadAsync()
    {
        // TODO: Show message box
        try
        {
            var downloadOptions = new DownloadConfiguration
            {
                ChunkCount = 8,
                // MaximumBytesPerSecond = 64 * 1000,
                ParallelDownload = true,
            };

            _downloadService = new DownloadService(downloadOptions);
            _downloadService.DownloadStarted += DownloadServiceOnDownloadStarted;
            _downloadService.DownloadFileCompleted += DownloadServiceOnDownloadFileCompleted;
            _downloadService.DownloadProgressChanged += DownloadServiceOnDownloadProgressChanged;
            _downloadService.ChunkDownloadProgressChanged += DownloadServiceOnChunkDownloadProgressChanged;

            var downloadPath = DownloadFile.SaveLocation;
            if (downloadPath.IsNullOrEmpty())
            {
                var saveDirectory = await UnitOfWork.CategorySaveDirectoryRepository
                    .GetAsync(where: sd => sd.CategoryId == null);

                downloadPath = saveDirectory?.SaveDirectory;
                if (downloadPath.IsNullOrEmpty())
                    return;
            }

            if (DownloadFile.FileName.IsNullOrEmpty() || DownloadFile.Url.IsNullOrEmpty())
                return;

            if (!Directory.Exists(downloadPath!))
                Directory.CreateDirectory(downloadPath!);

            CreateChunksData(downloadOptions.ChunkCount);
            CalculateElapsedTime();
            var fileName = Path.Join(downloadPath!, DownloadFile.FileName!);
            var url = DownloadFile.Url!;

            await _downloadService.DownloadFileTaskAsync(address: url, fileName: fileName).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void DownloadServiceOnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        Console.WriteLine("Download Started...");
    }

    private void DownloadServiceOnDownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        Console.WriteLine("Download Completed...");
    }

    private void DownloadServiceOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            DownloadFile.DownloadProgress = (float)e.ProgressPercentage;
            DownloadFile.TransferRate = e.BytesPerSecondSpeed.ToFileSize();

            TimeSpan timeLeft = TimeSpan.Zero;
            var remainSizeToReceive = (DownloadFile.Size ?? 0) - e.ReceivedBytesSize;
            var remainSeconds = remainSizeToReceive / e.BytesPerSecondSpeed;
            if (!double.IsInfinity(remainSeconds))
                timeLeft = TimeSpan.FromSeconds(remainSeconds);

            DownloadFile.TimeLeft = timeLeft.GetShortTime();
        });
    }

    private void DownloadServiceOnChunkDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!int.TryParse(e.ProgressId, out var progressId))
                return;

            var chunkData = ChunksData.FirstOrDefault(cd => cd.ChunkIndex == progressId);
            if (chunkData == null)
                return;

            chunkData.Info = chunkData.DownloadedSize == e.ReceivedBytesSize ? "Waiting..." : "Receiving...";
            chunkData.DownloadedSize = e.ReceivedBytesSize;
            chunkData.TotalSize = e.TotalBytesToReceive;
        });
    }

    private void CreateChunksData(int count)
    {
        var chunks = new List<ChunkDataViewModel>();
        for (int i = 0; i < count; i++)
            chunks.Add(new ChunkDataViewModel { ChunkIndex = i });

        ChunksData = chunks.ToObservableCollection();
    }

    private void CalculateElapsedTime()
    {
        _elapsedTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTimeTimer.Tick += ElapsedTimeTimerOnTick;
        _elapsedTimeTimer.Start();
    }

    private void ElapsedTimeTimerOnTick(object? sender, EventArgs e)
    {
        _elapsedTime ??= TimeSpan.Zero;
        _elapsedTime = TimeSpan.FromSeconds(_elapsedTime.Value.TotalSeconds + 1);
        DownloadFile.ElapsedTime = _elapsedTime.GetShortTime();
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
    }

    public void ChangeOptions(DownloadOptionsViewEventArgs eventArgs)
    {
        _openFolderAfterDownloadFinished = eventArgs.OpenFolderAfterDownloadFinished;
        _exitProgramAfterDownloadFinished = eventArgs.ExitProgramAfterDownloadFinished;
        _turnOffComputerAfterDownloadFinished = eventArgs.TurnOffComputerAfterDownloadFinished;
        _turnOffComputerMode = eventArgs.TurnOffComputerMode;
    }
}
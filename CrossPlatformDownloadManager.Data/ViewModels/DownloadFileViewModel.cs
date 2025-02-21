using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Downloader;
using MathNet.Numerics;
using MathNet.Numerics.Statistics;
using Serilog;
using DownloadProgressChangedEventArgs = Downloader.DownloadProgressChangedEventArgs;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public sealed class DownloadFileViewModel : PropertyChangedBase
{
    #region Private Fields

    private const int NumberOfSamples = 101;

    // ElapsedTime timer
    private DispatcherTimer? _elapsedTimeTimer;
    private TimeSpan? _elapsedTimeOfStartingDownload;

    // TimeLeft timer
    private DispatcherTimer? _timeLeftTimer;
    private long _receivedBytesSize;
    private readonly List<double> _downloadSpeeds = [];
    private readonly List<double> _medianDownloadSpeeds = [];

    // UpdateChunksData timer
    private DispatcherTimer? _updateChunksDataTimer;
    private List<ChunkProgressViewModel>? _chunkProgresses;

    private DownloadService? _downloadService;

    private int _id;
    private string? _url;
    private string? _fileName;
    private string? _fileType;
    private int? _downloadQueueId;
    private string? _downloadQueueName;
    private double? _size;
    private string? _description;
    private DownloadFileStatus? _status;
    private DateTime? _lastTryDate;
    private DateTime _dateAdded;
    private int? _downloadQueuePriority;
    private int? _categoryId;
    private float? _downloadProgress;
    private string? _downloadedSizeAsString;
    private TimeSpan? _elapsedTime;
    private TimeSpan? _timeLeft;
    private float? _transferRate;
    private string? _saveLocation;
    private string? _downloadPackage;
    private ObservableCollection<ChunkDataViewModel> _chunksData = [];
    private int _countOfError;
    private bool? _canResumeDownload;

    #endregion

    #region Properties

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string? Url
    {
        get => _url;
        set => SetField(ref _url, value);
    }

    public string? FileName
    {
        get => _fileName;
        set => SetField(ref _fileName, value);
    }

    public string? FileType
    {
        get => _fileType;
        set => SetField(ref _fileType, value);
    }

    public int? DownloadQueueId
    {
        get => _downloadQueueId;
        set => SetField(ref _downloadQueueId, value);
    }

    public string? DownloadQueueName
    {
        get => _downloadQueueName;
        set => SetField(ref _downloadQueueName, value);
    }

    public double? Size
    {
        get => _size;
        set
        {
            if (!SetField(ref _size, value))
                return;

            OnPropertyChanged(nameof(SizeAsString));
        }
    }

    public string SizeAsString => Size.ToFileSize();

    public string? Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public DownloadFileStatus? Status
    {
        get => _status;
        set
        {
            var oldStatus = Status;
            if (!SetField(ref _status, value))
                return;

            NotifyDownloadStatusChanged(oldStatus);
            NotifyDownloadStatusChanged(Status);
        }
    }

    public bool IsCompleted => Status == DownloadFileStatus.Completed;
    public bool IsDownloading => Status == DownloadFileStatus.Downloading;
    public bool IsStopping => Status == DownloadFileStatus.Stopping;
    public bool IsStopped => Status == DownloadFileStatus.Stopped;
    public bool IsPaused => Status == DownloadFileStatus.Paused;
    public bool IsError => Status == DownloadFileStatus.Error;

    public DateTime? LastTryDate
    {
        get => _lastTryDate;
        set
        {
            if (!SetField(ref _lastTryDate, value))
                return;

            OnPropertyChanged(nameof(LastTryDateAsString));
        }
    }

    public string LastTryDateAsString => LastTryDate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    public DateTime DateAdded
    {
        get => _dateAdded;
        set
        {
            if (!SetField(ref _dateAdded, value))
                return;

            OnPropertyChanged(nameof(DateAddedAsString));
        }
    }

    public string DateAddedAsString => DateAdded.ToString(CultureInfo.InvariantCulture);

    public int? DownloadQueuePriority
    {
        get => _downloadQueuePriority;
        set => SetField(ref _downloadQueuePriority, value);
    }

    public int? CategoryId
    {
        get => _categoryId;
        set => SetField(ref _categoryId, value);
    }

    public float? DownloadProgress
    {
        get => _downloadProgress;
        set
        {
            if (!SetField(ref _downloadProgress, value))
                return;

            OnPropertyChanged(nameof(DownloadProgressAsString));
            OnPropertyChanged(nameof(CeilingDownloadProgressAsString));
        }
    }

    public string DownloadProgressAsString => DownloadProgress == null ? "00.00%" : $"{DownloadProgress ?? 0:00.00}%";
    public string CeilingDownloadProgressAsString => DownloadProgress == null ? "00.00%" : $"{Math.Ceiling(DownloadProgress ?? 0):00}%";

    public string? DownloadedSizeAsString
    {
        get => _downloadedSizeAsString;
        set => SetField(ref _downloadedSizeAsString, value);
    }

    public TimeSpan? ElapsedTime
    {
        get => _elapsedTime;
        set
        {
            if (!SetField(ref _elapsedTime, value))
                return;

            OnPropertyChanged(nameof(ElapsedTimeAsString));
        }
    }

    public string ElapsedTimeAsString => ElapsedTime.GetShortTime();

    public TimeSpan? TimeLeft
    {
        get => _timeLeft;
        set
        {
            if (!SetField(ref _timeLeft, value))
                return;

            OnPropertyChanged(nameof(TimeLeftAsString));
        }
    }

    public string TimeLeftAsString => TimeLeft.GetShortTime();

    public float? TransferRate
    {
        get => _transferRate;
        set
        {
            if (!SetField(ref _transferRate, value))
                return;

            OnPropertyChanged(nameof(TransferRateAsString));
        }
    }

    public string TransferRateAsString => TransferRate.ToFileSize();

    public string? SaveLocation
    {
        get => _saveLocation;
        set => SetField(ref _saveLocation, value);
    }

    public string? DownloadPackage
    {
        get => _downloadPackage;
        set => SetField(ref _downloadPackage, value);
    }

    public ObservableCollection<ChunkDataViewModel> ChunksData
    {
        get => _chunksData;
        set => SetField(ref _chunksData, value);
    }

    public int CountOfError
    {
        get => _countOfError;
        set => SetField(ref _countOfError, value);
    }

    public bool? CanResumeDownload
    {
        get => _canResumeDownload;
        set => SetField(ref _canResumeDownload, value);
    }

    public bool PlayStopSound { get; set; } = true;
    public int? TempDownloadQueueId { get; set; }

    #endregion

    #region Events

    public event EventHandler<DownloadFileEventArgs>? DownloadFinished;
    public event EventHandler<DownloadFileEventArgs>? DownloadPaused;
    public event EventHandler<DownloadFileEventArgs>? DownloadResumed;
    public event EventHandler<DownloadFileEventArgs>? DownloadStopped;

    #endregion

    public async Task StartDownloadFileAsync(DownloadService? downloadService,
        DownloadConfiguration downloadConfiguration,
        IUnitOfWork? unitOfWork)
    {
        if (downloadService == null || unitOfWork == null)
            return;

        _downloadService = downloadService;

        downloadService.DownloadStarted += DownloadServiceOnDownloadStarted;
        downloadService.DownloadFileCompleted += DownloadServiceOnDownloadFileCompleted;
        downloadService.DownloadProgressChanged += DownloadServiceOnDownloadProgressChanged;
        downloadService.ChunkDownloadProgressChanged += DownloadServiceOnChunkDownloadProgressChanged;

        var downloadPath = SaveLocation;
        if (downloadPath.IsNullOrEmpty())
        {
            var saveDirectory = await unitOfWork
                .CategorySaveDirectoryRepository
                .GetAsync(where: sd => sd.CategoryId == null);

            downloadPath = saveDirectory?.SaveDirectory;
            if (downloadPath.IsNullOrEmpty())
                return;
        }

        if (FileName.IsNullOrEmpty() || Url.IsNullOrEmpty() || !Url.CheckUrlValidation())
            return;

        if (!Directory.Exists(downloadPath!))
            Directory.CreateDirectory(downloadPath!);

        CreateChunksData(downloadConfiguration.ChunkCount);
        CalculateElapsedTime();
        UpdateChunksData();

        // Check resume capability
        CanResumeDownload = null;
        _ = CheckResumeCapabilityAsync();

        var fileName = Path.Combine(downloadPath!, FileName!);
        var downloadPackage = DownloadPackage.ConvertFromJson<DownloadPackage>();
        if (downloadPackage == null)
        {
            await downloadService.DownloadFileTaskAsync(address: Url!, fileName: fileName);
        }
        else
        {
            // Load previous chunks data
            LoadChunksData(downloadPackage.Chunks);

            var urls = downloadPackage.Urls.ToList();
            var currentUrl = urls.FirstOrDefault(u => u.Equals(Url!));
            if (currentUrl.IsNullOrEmpty())
            {
                urls.Clear();
                urls.Add(Url!);

                downloadPackage.Urls = urls.ToArray();
            }

            await downloadService.DownloadFileTaskAsync(downloadPackage);
        }
    }

    public void StopDownloadFile(DownloadService? downloadService)
    {
        if (downloadService == null)
            return;

        _downloadService = downloadService;

        ResetDownload();

        Status = DownloadFileStatus.Stopping;
        _ = downloadService.CancelTaskAsync();
        DownloadStopped?.Invoke(this, new DownloadFileEventArgs { Id = Id });
    }

    public void ResumeDownloadFile(DownloadService? downloadService)
    {
        if (downloadService == null)
            return;

        _downloadService = downloadService;

        downloadService.Resume();
        _elapsedTimeTimer?.Start();
        _timeLeftTimer?.Start();
        _updateChunksDataTimer?.Start();
        Status = DownloadFileStatus.Downloading;

        DownloadResumed?.Invoke(this, new DownloadFileEventArgs { Id = Id });
    }

    public void PauseDownloadFile(DownloadService? downloadService)
    {
        if (downloadService == null)
            return;

        _downloadService = downloadService;

        downloadService.Pause();
        _elapsedTimeTimer?.Stop();
        _timeLeftTimer?.Stop();
        _updateChunksDataTimer?.Stop();
        Status = DownloadFileStatus.Paused;
        UpdateChunksDataTimerOnTick(null, EventArgs.Empty);
        SaveDownloadPackage(downloadService.Package);

        DownloadPaused?.Invoke(this, new DownloadFileEventArgs { Id = Id });
    }

    #region Helpers

    private void DownloadServiceOnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        Status = DownloadFileStatus.Downloading;
        LastTryDate = DateTime.Now;
    }

    private void DownloadServiceOnDownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        bool isSuccess;
        Exception? error = null;

        // Save download package
        SaveDownloadPackage(_downloadService!.Package);
        // Clear timers
        ResetDownload();

        // Change download status and if error occurred, store that
        if (e is { Error: not null, Cancelled: false })
        {
            Status = DownloadFileStatus.Error;
            isSuccess = false;
            error = e.Error;
        }
        else if (e.Cancelled)
        {
            Status = DownloadFileStatus.Stopped;
            isSuccess = true;
        }
        else
        {
            Status = DownloadFileStatus.Completed;
            isSuccess = true;
        }

        // Create an object of DownloadFileEventArgs
        var eventArgs = new DownloadFileEventArgs
        {
            Id = Id,
            IsSuccess = isSuccess,
            Error = error,
        };

        // Raise download finished event
        DownloadFinished?.Invoke(this, eventArgs);
    }

    private void ResetDownload()
    {
        // Clear elapsed time timer
        if (_elapsedTimeTimer != null)
        {
            _elapsedTimeTimer.Stop();
            _elapsedTimeTimer.Tick -= ElapsedTimeTimerOnTick;
            _elapsedTimeTimer = null;
        }

        // Clear time left timer
        if (_timeLeftTimer != null)
        {
            _timeLeftTimer.Stop();
            _timeLeftTimer.Tick -= TimeLeftTimerOnTick;
            _timeLeftTimer = null;
        }

        // Clear update chunks data timer
        if (_updateChunksDataTimer != null)
        {
            _updateChunksDataTimer.Stop();
            _updateChunksDataTimer.Tick -= UpdateChunksDataTimerOnTick;
            _updateChunksDataTimer = null;
        }

        // Reset elapsed time of starting download
        _elapsedTimeOfStartingDownload = null;
        // Reset chunk progresses
        _chunkProgresses = null;
        // Clear download speeds list
        _downloadSpeeds.Clear();
        // Clear median download speeds list
        _medianDownloadSpeeds.Clear();
        // Reset resume capability
        CanResumeDownload = null;
    }

    private void DownloadServiceOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        DownloadProgress = (float)e.ProgressPercentage;
        TransferRate = (float)e.BytesPerSecondSpeed;
        DownloadedSizeAsString = e.ReceivedBytesSize.ToFileSize();

        // Save required data to calculate time left
        _receivedBytesSize = e.ReceivedBytesSize;
        // Store average download speeds to find median
        _downloadSpeeds.Add(e.AverageBytesPerSecondSpeed);
        // Store specified number of samples
        // For calculating median it's better to have odd number of samples
        if (_downloadSpeeds.Count > NumberOfSamples)
            _downloadSpeeds.RemoveAt(0);

        // Initialize time left timer and start it to calculate time left
        if (_timeLeftTimer != null)
            return;

        _timeLeftTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timeLeftTimer.Tick += TimeLeftTimerOnTick;
        _timeLeftTimer.Start();
    }

    private void TimeLeftTimerOnTick(object? sender, EventArgs e)
    {
        var timeLeft = TimeSpan.Zero;

        // Calculate median download speed
        var median = _downloadSpeeds.Median();
        // Store median download speed to calculate average better
        _medianDownloadSpeeds.Add(median);
        // Store specified number of samples
        if (_medianDownloadSpeeds.Count > NumberOfSamples)
            _medianDownloadSpeeds.RemoveAt(0);

        // Calculate average download speed
        var averageDownloadSpeed = _medianDownloadSpeeds.Mean();
        // Make sure download speeds count is greater than 1
        if (averageDownloadSpeed <= 0)
        {
            TimeLeft = timeLeft;
            return;
        }

        // Calculate remain size
        var remainSizeToReceive = (Size ?? 0) - _receivedBytesSize;
        // Calculate remain seconds by average download speed
        var remainSeconds = (remainSizeToReceive / averageDownloadSpeed).Round(0);
        if (!double.IsInfinity(remainSeconds))
            timeLeft = TimeSpan.FromSeconds(remainSeconds);

        TimeLeft = timeLeft;
    }

    private void DownloadServiceOnChunkDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        if (_chunkProgresses == null || _chunkProgresses.Count == 0)
            return;

        var chunkProgress = _chunkProgresses.FirstOrDefault(cp => cp.ProgressId.Equals(e.ProgressId));
        if (chunkProgress == null)
            return;

        chunkProgress.ReceivedBytesSize = e.ReceivedBytesSize;
        chunkProgress.TotalBytesToReceive = e.TotalBytesToReceive;
        chunkProgress.IsCompleted = e.ReceivedBytesSize >= e.TotalBytesToReceive;
    }

    private void CreateChunksData(int count)
    {
        var chunks = new List<ChunkDataViewModel>();
        _chunkProgresses ??= [];

        for (var i = 0; i < count; i++)
        {
            chunks.Add(new ChunkDataViewModel { ChunkIndex = i });
            _chunkProgresses.Add(new ChunkProgressViewModel { ProgressId = i.ToString() });
        }

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
        _elapsedTimeOfStartingDownload ??= TimeSpan.Zero;
        _elapsedTimeOfStartingDownload = TimeSpan.FromSeconds(_elapsedTimeOfStartingDownload.Value.TotalSeconds + 1);
        ElapsedTime = _elapsedTimeOfStartingDownload;
    }

    private void UpdateChunksData()
    {
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _updateChunksDataTimer.Tick += UpdateChunksDataTimerOnTick;
        _updateChunksDataTimer.Start();
    }

    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs e)
    {
        if (_chunkProgresses == null || _chunkProgresses.Count == 0)
            return;

        var chunkProgresses = _chunkProgresses.Where(c => !c.IsCompletionChecked).ToList();
        foreach (var chunkProgress in chunkProgresses)
        {
            if (!int.TryParse(chunkProgress.ProgressId, out var progressId))
                return;

            var chunkData = ChunksData.FirstOrDefault(cd => cd.ChunkIndex == progressId);
            if (chunkData == null)
                return;

            if (chunkProgress.CheckCount % 10 == 0)
            {
                if (_updateChunksDataTimer!.IsEnabled && chunkData.DownloadedSize != chunkData.TotalSize)
                {
                    chunkData.Info = chunkData.DownloadedSize == chunkProgress.ReceivedBytesSize
                        ? "Connecting..."
                        : "Receiving...";
                }

                chunkProgress.CheckCount = 1;
            }
            else
            {
                chunkProgress.CheckCount++;
            }

            chunkData.DownloadedSize = chunkProgress.ReceivedBytesSize;
            chunkData.TotalSize = chunkProgress.TotalBytesToReceive;

            if (!_updateChunksDataTimer!.IsEnabled)
                chunkData.Info = "Paused";

            if (chunkProgress.IsCompleted)
            {
                chunkData.Info = "Completed";
                chunkProgress.IsCompletionChecked = true;
            }
        }
    }

    private void SaveDownloadPackage(DownloadPackage? downloadPackage)
    {
        DownloadPackage = downloadPackage?.ConvertToJson();
    }

    private async Task CheckResumeCapabilityAsync()
    {
        try
        {
            if (Url.IsNullOrEmpty() || !Url.CheckUrlValidation())
            {
                CanResumeDownload = false;
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Range = new RangeHeaderValue(0, 0);

            // Send HEAD request
            using var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, Url));
            response.EnsureSuccessStatusCode();

            // Check for Accept-Ranges header
            if (response.Headers.Contains("Accept-Ranges"))
            {
                var acceptRanges = response.Headers.GetValues("Accept-Ranges");
                if (acceptRanges.Contains("bytes"))
                {
                    CanResumeDownload = true;
                    return;
                }
            }

            // Some servers don't include Accept-Ranges but still support partial content.
            // If Range request succeeds with Partial Content status:
            if (response.StatusCode == HttpStatusCode.PartialContent)
            {
                CanResumeDownload = true;
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while checking resume capability. Error message: {ErrorMessage}", ex.Message);
        }

        CanResumeDownload = false;
    }

    private void LoadChunksData(Chunk[] chunks)
    {
        if (chunks.Length == 0)
            return;

        foreach (var chunk in chunks)
        {
            var chunkProgress = _chunkProgresses?.Find(c => c.ProgressId.Equals(chunk.Id));
            if (chunkProgress == null)
                continue;

            chunkProgress.ReceivedBytesSize = chunk.IsDownloadCompleted() ? chunk.Length : chunk.Position;
            chunkProgress.TotalBytesToReceive = chunk.Length;
        }
    }

    private void NotifyDownloadStatusChanged(DownloadFileStatus? status)
    {
        if (status == null)
            return;

        switch (status)
        {
            case DownloadFileStatus.Completed:
            {
                OnPropertyChanged(nameof(IsCompleted));
                break;
            }

            case DownloadFileStatus.Downloading:
            {
                OnPropertyChanged(nameof(IsDownloading));
                break;
            }

            case DownloadFileStatus.Stopping:
            {
                OnPropertyChanged(nameof(IsStopping));
                break;
            }

            case DownloadFileStatus.Stopped:
            {
                OnPropertyChanged(nameof(IsStopped));
                break;
            }

            case DownloadFileStatus.Paused:
            {
                OnPropertyChanged(nameof(IsPaused));
                break;
            }

            case DownloadFileStatus.Error:
            {
                OnPropertyChanged(nameof(IsError));
                break;
            }
        }
    }

    #endregion
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using Downloader;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

[AddINotifyPropertyChangedInterface]
public sealed class DownloadFileViewModel
{
    #region Private Fields

    // ElapsedTime timer
    private DispatcherTimer? _elapsedTimeTimer;
    private TimeSpan? _elapsedTime;

    // UpdateChunksData timer
    private DispatcherTimer? _updateChunksDataTimer;
    private List<ChunkProgressViewModel>? _chunkProgresses;

    #endregion

    #region Events

    public event EventHandler<DownloadFileEventArgs>? DownloadFinished;

    #endregion

    #region Properties

    public int Id { get; set; }
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public int? QueueId { get; set; }
    public string? QueueName { get; set; }
    public double? Size { get; set; }
    public string? SizeAsString => Size.ToFileSize();
    public bool IsCompleted { get; set; }
    public bool IsDownloading { get; set; }
    public bool IsPaused { get; set; }
    public bool IsError { get; set; }
    public float? DownloadProgress { get; set; }
    public string? DownloadProgressAsString => DownloadProgress == null ? string.Empty : $"{DownloadProgress:00.00}%";
    public double? DownloadSize { get; set; }
    public string? DownloadSizeAsString => DownloadSize.ToFileSize();
    public TimeSpan? ElapsedTime { get; set; }
    public string? ElapsedTimeAsString => ElapsedTime.GetShortTime();
    public TimeSpan? TimeLeft { get; set; }
    public string? TimeLeftAsString => TimeLeft.GetShortTime();
    public float? TransferRate { get; set; }
    public string? TransferRateAsString => TransferRate.ToFileSize();
    public DateTime? LastTryDate { get; set; }
    public string? LastTryDateAsString => LastTryDate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    public DateTime DateAdded { get; set; }
    public string DateAddedAsString => DateAdded.ToString(CultureInfo.InvariantCulture);
    public string? Url { get; set; }
    public string? SaveLocation { get; set; }
    public int? CategoryId { get; set; }
    public string? DownloadPackage { get; set; }
    public ObservableCollection<ChunkDataViewModel> ChunksData { get; set; } = [];

    #endregion

    public async Task StartDownloadFileAsync(DownloadService? downloadService,
        DownloadConfiguration downloadConfiguration, IUnitOfWork? unitOfWork)
    {
        if (downloadService == null || unitOfWork == null)
            return;

        downloadService.DownloadStarted += DownloadServiceOnDownloadStarted;
        downloadService.DownloadFileCompleted += DownloadServiceOnDownloadFileCompleted;
        downloadService.DownloadProgressChanged += DownloadServiceOnDownloadProgressChanged;
        downloadService.ChunkDownloadProgressChanged += DownloadServiceOnChunkDownloadProgressChanged;

        var downloadPath = SaveLocation;
        if (downloadPath.IsNullOrEmpty())
        {
            var saveDirectory = await unitOfWork.CategorySaveDirectoryRepository
                .GetAsync(where: sd => sd.CategoryId == null);

            downloadPath = saveDirectory?.SaveDirectory;
            if (downloadPath.IsNullOrEmpty())
                return;
        }

        if (FileName.IsNullOrEmpty() || Url.IsNullOrEmpty())
            return;

        if (!Directory.Exists(downloadPath!))
            Directory.CreateDirectory(downloadPath!);

        CreateChunksData(downloadConfiguration.ChunkCount);
        CalculateElapsedTime();
        UpdateChunksData();

        var fileName = Path.Combine(downloadPath!, FileName!);
        var downloadPackage = DownloadPackage.ConvertFromJson<DownloadPackage>();
        if (downloadPackage == null)
            await downloadService.DownloadFileTaskAsync(address: Url!, fileName: fileName);
        else
            await downloadService.DownloadFileTaskAsync(downloadPackage);
    }

    public async Task StopDownloadFileAsync(DownloadService? downloadService)
    {
        if (downloadService == null)
            return;

        _elapsedTimeTimer?.Stop();
        _updateChunksDataTimer?.Stop();

        _elapsedTimeTimer = null;
        _elapsedTime = null;
        _updateChunksDataTimer = null;
        _chunkProgresses = null;

        var pack = downloadService.Package;
        await downloadService.CancelTaskAsync();
        SaveDownloadPackage(pack);
    }

    public void ResumeDownloadFile(DownloadService? downloadService)
    {
        if (downloadService == null)
            return;

        downloadService.Resume();
        _elapsedTimeTimer?.Start();
        _updateChunksDataTimer?.Start();
        ChangeDownloadFileState(isDownloading: true);
    }

    public void PauseDownloadFile(DownloadService? downloadService)
    {
        if (downloadService == null)
            return;

        downloadService.Pause();
        _elapsedTimeTimer?.Stop();
        _updateChunksDataTimer?.Stop();
        ChangeDownloadFileState(isPaused: true);
        UpdateChunksDataTimerOnTick(null, null);
        SaveDownloadPackage(downloadService.Package);
    }

    #region Helpers

    private void DownloadServiceOnDownloadStarted(object? sender, DownloadStartedEventArgs e)
    {
        ChangeDownloadFileState(isDownloading: true);
        LastTryDate = DateTime.Now;
    }

    private void DownloadServiceOnDownloadFileCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        // TODO: Show error
        if (e.Error != null && !e.Cancelled)
        {
            ChangeDownloadFileState(isError: true);
            return;
        }

        if (e.Cancelled)
            ChangeDownloadFileState(isPaused: true);
        else
        {
            ChangeDownloadFileState(isCompleted: true);
            var eventArgs = new DownloadFileEventArgs { Id = Id };
            DownloadFinished?.Invoke(this, eventArgs);
        }
    }

    private void DownloadServiceOnDownloadProgressChanged(object? sender, DownloadProgressChangedEventArgs e)
    {
        DownloadProgress = (float)e.ProgressPercentage;
        TransferRate = (float)e.BytesPerSecondSpeed;
        DownloadSize = e.ReceivedBytesSize;

        TimeSpan timeLeft = TimeSpan.Zero;
        var remainSizeToReceive = (Size ?? 0) - e.ReceivedBytesSize;
        var remainSeconds = remainSizeToReceive / e.BytesPerSecondSpeed;
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
    }

    private void ChangeDownloadFileState(bool isCompleted = false, bool isDownloading = false, bool isPaused = false,
        bool isError = false)
    {
        IsCompleted = IsDownloading = IsPaused = IsError = false;
        if (!isCompleted && !isDownloading && !isPaused && !isError)
            return;

        if (isCompleted)
            IsCompleted = true;
        else if (isDownloading)
            IsDownloading = true;
        else if (isPaused)
            IsPaused = true;
        else if (isError)
            IsError = true;
    }

    private void CreateChunksData(int count)
    {
        var chunks = new List<ChunkDataViewModel>();
        _chunkProgresses ??= [];

        for (int i = 0; i < count; i++)
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
        _elapsedTime ??= TimeSpan.Zero;
        _elapsedTime = TimeSpan.FromSeconds(_elapsedTime.Value.TotalSeconds + 1);
        ElapsedTime = _elapsedTime;
    }

    private void UpdateChunksData()
    {
        _updateChunksDataTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _updateChunksDataTimer.Tick += UpdateChunksDataTimerOnTick;
        _updateChunksDataTimer.Start();
    }

    private void UpdateChunksDataTimerOnTick(object? sender, EventArgs? e)
    {
        if (_chunkProgresses == null || _chunkProgresses.Count == 0)
            return;

        foreach (var chunkProgress in _chunkProgresses)
        {
            if (!int.TryParse(chunkProgress.ProgressId, out var progressId))
                return;

            var chunkData = ChunksData.FirstOrDefault(cd => cd.ChunkIndex == progressId);
            if (chunkData == null)
                return;

            if (chunkProgress.CheckCount % 5 == 0)
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

            if (chunkData.DownloadedSize == chunkData.TotalSize)
                chunkData.Info = "Completed";
            else if (!_updateChunksDataTimer!.IsEnabled)
                chunkData.Info = "Paused";

            chunkData.DownloadedSize = chunkProgress.ReceivedBytesSize;
            chunkData.TotalSize = chunkProgress.TotalBytesToReceive;
        }
    }

    private void SaveDownloadPackage(DownloadPackage? downloadPackage)
    {
        DownloadPackage = downloadPackage?.ConvertToJson();
    }

    #endregion
}
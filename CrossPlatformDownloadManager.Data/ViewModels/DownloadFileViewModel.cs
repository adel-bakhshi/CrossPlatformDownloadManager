using System.Collections.ObjectModel;
using System.Globalization;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public sealed class DownloadFileViewModel : PropertyChangedBase
{
    #region Private Fields

    private int _id;
    private string? _url;
    private string? _fileName;
    private string? _fileType;
    private int? _downloadQueueId;
    private string? _downloadQueueName;
    private double? _size;
    private bool _isSizeUnknown;
    private string? _description;
    private DownloadFileStatus? _status;
    private DateTime? _lastTryDate;
    private DateTime _dateAdded;
    private int? _downloadQueuePriority;
    private int? _categoryId;
    private float? _downloadProgress;
    private double? _downloadedSize;
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

    public string SizeAsString => IsSizeUnknown ? "Unknown" : Size.ToFileSize();

    public bool IsSizeUnknown
    {
        get => _isSizeUnknown;
        set
        {
            SetField(ref _isSizeUnknown, value);
            OnPropertyChanged(nameof(SizeAsString));
            OnPropertyChanged(nameof(DownloadProgressAsString));
            OnPropertyChanged(nameof(CeilingDownloadProgressAsString));
            OnPropertyChanged(nameof(TimeLeftAsString));
        }
    }

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

    public string DownloadProgressAsString => IsSizeUnknown ? "Unknown" : DownloadProgress == null ? "00.00%" : $"{DownloadProgress ?? 0:00.00}%";
    public string CeilingDownloadProgressAsString => IsSizeUnknown ? "Unknown" : DownloadProgress == null ? "00.00%" : $"{Math.Ceiling(DownloadProgress ?? 0):00}%";

    public double? DownloadedSize
    {
        get => _downloadedSize;
        set
        {
            SetField(ref _downloadedSize, value);
            OnPropertyChanged(nameof(DownloadedSizeAsString));
        }
    }

    public string DownloadedSizeAsString => DownloadedSize.ToFileSize();

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

    public string TimeLeftAsString => IsSizeUnknown ? "Unknown" : TimeLeft.GetShortTime();

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

    #region Helpers

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
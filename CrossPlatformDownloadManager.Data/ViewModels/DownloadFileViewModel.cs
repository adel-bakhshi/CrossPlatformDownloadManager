using CrossPlatformDownloadManager.Utils;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public sealed class DownloadFileViewModel : NotifyProperty
{
    private int _id;

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    private string? _fileName;

    public string? FileName
    {
        get => _fileName;
        set => SetField(ref _fileName, value);
    }

    private string? _fileType;

    public string? FileType
    {
        get => _fileType;
        set => SetField(ref _fileType, value);
    }

    private int? _queueId;

    public int? QueueId
    {
        get => _queueId;
        set => SetField(ref _queueId, value);
    }

    private string? _queueName;

    public string? QueueName
    {
        get => _queueName;
        set => SetField(ref _queueName, value);
    }

    private double? _size;

    public double? Size
    {
        get => _size;
        set => SetField(ref _size, value);
    }

    public string? SizeAsString => Size.ToFileSize();

    private bool _isCompleted;

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetField(ref _isCompleted, value);
    }

    private bool _isDownloading;

    public bool IsDownloading
    {
        get => _isDownloading;
        set => SetField(ref _isDownloading, value);
    }

    private bool _isPaused;

    public bool IsPaused
    {
        get => _isPaused;
        set => SetField(ref _isPaused, value);
    }

    private bool _isError;

    public bool IsError
    {
        get => _isError;
        set => SetField(ref _isError, value);
    }

    private float? _downloadProgress;

    public float? DownloadProgress
    {
        get => _downloadProgress;
        set => SetField(ref _downloadProgress, value);
    }

    private string? _timeLeft;

    public string? TimeLeft
    {
        get => _timeLeft;
        set => SetField(ref _timeLeft, value);
    }

    private string? _transferRate;

    public string? TransferRate
    {
        get => _transferRate;
        set => SetField(ref _transferRate, value);
    }

    private string? _lastTryDate;

    public string? LastTryDate
    {
        get => _lastTryDate;
        set => SetField(ref _lastTryDate, value);
    }

    private string? _dateAdded;

    public string? DateAdded
    {
        get => _dateAdded;
        set => SetField(ref _dateAdded, value);
    }
}
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
        set
        {
            var result = SetField(ref _size, value);
            if (result)
                SizeAsString = value.ToFileSize();
        }
    }

    private string? _sizeAsString;

    public string? SizeAsString
    {
        get => _sizeAsString;
        set => SetField(ref _sizeAsString, value);
    }

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
        set
        {
            var result = SetField(ref _downloadProgress, value);
            if (result)
                DownloadProgressAsString = value == null ? string.Empty : $"{value:00.00}%";
        }
    }

    private string? _downloadProgressAsString;
    
    public string? DownloadProgressAsString
    {
        get => _downloadProgressAsString;
        set => SetField(ref _downloadProgressAsString, value);
    }

    private string? _elapsedTime;

    public string? ElapsedTime
    {
        get => _elapsedTime;
        set => SetField(ref _elapsedTime, value);
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

    private string? _url;

    public string? Url
    {
        get => _url;
        set => SetField(ref _url, value);
    }

    private string? _saveLocation;

    public string? SaveLocation
    {
        get => _saveLocation;
        set => SetField(ref _saveLocation, value);
    }

    private int? _categoryId;

    public int? CategoryId
    {
        get => _categoryId;
        set => SetField(ref _categoryId, value);
    }
}
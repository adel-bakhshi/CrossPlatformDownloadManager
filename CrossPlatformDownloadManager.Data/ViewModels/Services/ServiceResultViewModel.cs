namespace CrossPlatformDownloadManager.Data.ViewModels.Services;

public class ServiceResultViewModel
{
    public bool IsSuccess { get; set; }
    public string? Header { get; set; }
    public string? Message { get; set; }
}

public class ServiceResultViewModel<T> : ServiceResultViewModel
{
    public T? Result { get; set; }
}
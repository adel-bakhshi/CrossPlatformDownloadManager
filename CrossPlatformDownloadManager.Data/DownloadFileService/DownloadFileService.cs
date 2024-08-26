using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.Data.DownloadFileService;

public class DownloadFileService : IDownloadFileService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private List<DownloadFileViewModel> _downloadFiles;

    #endregion

    public DownloadFileService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        
        LoadFilesAsync().GetAwaiter().GetResult();
    }
    
    public async Task LoadFilesAsync()
    {
        try
        {
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    #region Notify Property Changed

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
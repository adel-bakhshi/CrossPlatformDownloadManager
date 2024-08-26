using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddFilesToQueueWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly int _downloadQueueId;

    #endregion
    
    #region Properties

    private ObservableCollection<DownloadFileViewModel> _downloadFiles;

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    #endregion
    
    public AddFilesToQueueWindowViewModel(IUnitOfWork unitOfWork, int downloadQueueId) : base(unitOfWork)
    {
        _downloadQueueId = downloadQueueId;
        
        DownloadFiles = GetDownloadFilesAsync().Result;

        SaveCommand = ReactiveCommand.Create(Save);
    }

    private void Save()
    {
        throw new NotImplementedException();
    }

    private async Task<ObservableCollection<DownloadFileViewModel>> GetDownloadFilesAsync()
    {
        try
        {
            var downloadFiles = await UnitOfWork.DownloadFileRepository
                .GetAllAsync(where: df => df.DownloadQueueId != _downloadQueueId, select: df =>
                    new DownloadFileViewModel
                    {
                        Id = df.Id,
                        FileName = df.FileName,
                        Size = df.Size.ToFileSize(),
                        DownloadProgress = Math.Floor(df.DownloadProgress) == 0 ? null : df.DownloadProgress,
                    });

            return downloadFiles.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObservableCollection<DownloadFileViewModel>();
        }
    }
}
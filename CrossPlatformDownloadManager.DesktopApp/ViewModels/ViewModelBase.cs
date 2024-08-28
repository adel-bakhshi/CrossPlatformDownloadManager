using System.Collections.Generic;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class ViewModelBase : ReactiveObject
{
    #region Properties

    protected IUnitOfWork UnitOfWork { get; }

    protected IDownloadFileService DownloadFileService { get; }

    #endregion

    public ViewModelBase(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService)
    {
        UnitOfWork = unitOfWork;
        DownloadFileService = downloadFileService;

        DownloadFileService.DataChanged += DownloadFileServiceOnDataChanged;
    }

    protected virtual void DownloadFileServiceDataChanged(List<DownloadFileViewModel> downloadFiles)
    {
    }

    private void DownloadFileServiceOnDataChanged(object? sender, List<DownloadFileViewModel> e)
    {
        DownloadFileServiceDataChanged(e);
    }
}
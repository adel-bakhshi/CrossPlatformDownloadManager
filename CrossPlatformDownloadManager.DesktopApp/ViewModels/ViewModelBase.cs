using System.Collections.Generic;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
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

    protected virtual void DownloadFileServiceDataChanged(DownloadFileServiceEventArgs eventArgs)
    {
    }

    private void DownloadFileServiceOnDataChanged(object? sender, DownloadFileServiceEventArgs e)
    {
        DownloadFileServiceDataChanged(e);
    }
}
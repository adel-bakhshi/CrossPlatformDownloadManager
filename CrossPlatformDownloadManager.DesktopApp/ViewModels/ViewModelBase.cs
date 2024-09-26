using System;
using CrossPlatformDownloadManager.Data.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    #region Properties

    protected IAppService AppService { get; }

    #endregion

    protected ViewModelBase(IAppService appService)
    {
        AppService = appService;
        AppService.DownloadFileService.DataChanged += DownloadFileServiceOnDataChanged;
        AppService.DownloadQueueService.DataChanged += DownloadQueueServiceOnDataChanged;
    }

    #region Virtual Methods

    protected virtual void OnDownloadFileServiceDataChanged()
    {
    }

    protected virtual void OnDownloadQueueServiceDataChanged()
    {
    }

    #endregion

    #region Helpers

    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e) =>
        OnDownloadFileServiceDataChanged();

    private void DownloadQueueServiceOnDataChanged(object? sender, EventArgs e) =>
        OnDownloadQueueServiceDataChanged();

    #endregion
}
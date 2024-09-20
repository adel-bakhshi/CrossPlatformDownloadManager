using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
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
    }

    protected virtual void OnDownloadFileServiceDataChanged(DownloadFileServiceEventArgs eventArgs)
    {
    }

    #region Helpers

    private void DownloadFileServiceOnDataChanged(object? sender, DownloadFileServiceEventArgs e) =>
        OnDownloadFileServiceDataChanged(e);

    #endregion
}
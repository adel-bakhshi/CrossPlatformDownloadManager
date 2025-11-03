using System;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

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
        AppService.SettingsService.DataChanged += SettingsServiceOnDataChanged;
        AppService.CategoryService.CategoriesChanged += CategoryServiceOnCategoriesChanged;
    }

    #region Virtual Methods

    protected virtual void OnDownloadFileServiceDataChanged()
    {
    }

    protected virtual void OnDownloadQueueServiceDataChanged()
    {
    }

    protected virtual void OnSettingsServiceDataChanged()
    {
    }

    protected virtual void OnCategoryServiceCategoriesChanged()
    {
    }

    #endregion

    #region Helpers

    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e) => OnDownloadFileServiceDataChanged();

    private void DownloadQueueServiceOnDataChanged(object? sender, EventArgs e) => OnDownloadQueueServiceDataChanged();

    private void SettingsServiceOnDataChanged(object? sender, EventArgs e) => OnSettingsServiceDataChanged();

    private void CategoryServiceOnCategoriesChanged(object? sender, EventArgs e) => OnCategoryServiceCategoriesChanged();

    #endregion
}
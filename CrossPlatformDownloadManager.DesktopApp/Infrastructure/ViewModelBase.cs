using System;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure;

/// <summary>
/// Base class for all view models in the application.
/// Provides common functionality and event handling for service data changes.
/// </summary>
public abstract class ViewModelBase : ReactiveObject
{
    #region Properties

    /// <summary>
    /// Gets the application service instance for accessing various services.
    /// </summary>
    protected IAppService AppService { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
    /// </summary>
    /// <param name="appService">The application service instance.</param>
    protected ViewModelBase(IAppService appService)
    {
        AppService = appService;

        // Subscribe to service events
        AppService.DownloadFileService.DataChanged += DownloadFileServiceOnDataChanged;
        AppService.DownloadQueueService.DataChanged += DownloadQueueServiceOnDataChanged;
        AppService.SettingsService.DataChanged += SettingsServiceOnDataChanged;
        AppService.CategoryService.CategoriesChanged += CategoryServiceOnCategoriesChanged;
    }

    #region Virtual Methods

    /// <summary>
    /// Called when the download file service data changes.
    /// Derived classes can override this method to handle the event.
    /// </summary>
    protected virtual void OnDownloadFileServiceDataChanged()
    {
    }

    /// <summary>
    /// Called when the download queue service data changes.
    /// Derived classes can override this method to handle the event.
    /// </summary>
    protected virtual void OnDownloadQueueServiceDataChanged()
    {
    }

    /// <summary>
    /// Called when the settings service data changes.
    /// Derived classes can override this method to handle the event.
    /// </summary>
    protected virtual void OnSettingsServiceDataChanged()
    {
    }

    /// <summary>
    /// Called when the category service categories change.
    /// Derived classes can override this method to handle the event.
    /// </summary>
    protected virtual void OnCategoryServiceCategoriesChanged()
    {
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Handles the DataChanged event from the download file service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e)
    {
        OnDownloadFileServiceDataChanged();
    }

    /// <summary>
    /// Handles the DataChanged event from the download queue service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void DownloadQueueServiceOnDataChanged(object? sender, EventArgs e)
    {
        OnDownloadQueueServiceDataChanged();
    }

    /// <summary>
    /// Handles the DataChanged event from the settings service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void SettingsServiceOnDataChanged(object? sender, EventArgs e)
    {
        OnSettingsServiceDataChanged();
    }

    /// <summary>
    /// Handles the CategoriesChanged event from the category service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void CategoryServiceOnCategoriesChanged(object? sender, EventArgs e)
    {
        OnCategoryServiceCategoriesChanged();
    }

    #endregion
}
using System;
using System.Net;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;

public interface ISettingsService
{
    #region Properties

    /// <summary>
    /// Gets a value that indicates the current settings of the application.
    /// </summary>
    SettingsViewModel Settings { get; }

    #endregion

    #region Events

    /// <summary>
    /// Event that is raised when the settings are changed.
    /// </summary>
    event EventHandler? DataChanged;

    #endregion

    /// <summary>
    /// Loads settings data from database.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LoadSettingsAsync();

    /// <summary>
    /// Saves settings data to database.
    /// </summary>
    /// <param name="viewModel">The settings view model containing new data to save.</param>
    /// <param name="reloadData">A value indicating whether to reload settings data after saving.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SaveSettingsAsync(SettingsViewModel viewModel, bool reloadData = true);

    /// <summary>
    /// Adds a new proxy settings to the database.
    /// </summary>
    /// <param name="proxySettings">The proxy settings to add.</param>
    /// <returns>The ID of the newly added proxy settings.</returns>
    Task<int> AddProxySettingsAsync(ProxySettings? proxySettings);

    /// <summary>
    /// Adds a new proxy settings to the database.
    /// </summary>
    /// <param name="viewModel">The proxy settings view model to add.</param>
    /// <returns>The ID of the newly added proxy settings.</returns>
    Task<int> AddProxySettingsAsync(ProxySettingsViewModel? viewModel);

    /// <summary>
    /// Updates an existing proxy settings in the database.
    /// </summary>
    /// <param name="proxySettings">The proxy settings to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateProxySettingsAsync(ProxySettings? proxySettings);

    /// <summary>
    /// Updates an existing proxy settings in the database.
    /// </summary>
    /// <param name="viewModel">The proxy settings view model to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UpdateProxySettingsAsync(ProxySettingsViewModel? viewModel);

    /// <summary>
    /// Removes an existing proxy settings from the database.
    /// </summary>
    /// <param name="proxySettings">The proxy settings to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteProxySettingsAsync(ProxySettings? proxySettings);

    /// <summary>
    /// Removes an existing proxy settings from the database.
    /// </summary>
    /// <param name="viewModel">The proxy settings view model to remove.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteProxySettingsAsync(ProxySettingsViewModel? viewModel);

    /// <summary>
    /// Changes the settings to don't use a proxy.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DisableProxyAsync();

    /// <summary>
    /// Changes the settings to use the system proxy settings.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UseSystemProxySettingsAsync();

    /// <summary>
    /// Changes the settings to use a custom proxy.
    /// </summary>
    /// <param name="viewModel">The proxy settings view model to use.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UseCustomProxyAsync(ProxySettingsViewModel? viewModel);

    /// <summary>
    /// Shows the manager window.
    /// </summary>
    void ShowManager();

    /// <summary>
    /// Hides the manager window.
    /// </summary>
    void HideManager();

    /// <summary>
    /// Gets the temporary file location for saving the temp download files.
    /// </summary>
    /// <returns>Returns the temporary file location for saving the temp download files.</returns>
    string GetTemporaryFileLocation();
}
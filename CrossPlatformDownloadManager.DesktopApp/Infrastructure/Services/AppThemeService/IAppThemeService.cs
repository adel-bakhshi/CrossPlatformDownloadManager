using System.Collections.Generic;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;

/// <summary>
/// Interface for the AppThemeService.
/// </summary>
public interface IAppThemeService
{
    /// <summary>
    /// Loads theme data based on the settings.
    /// </summary>
    void LoadThemeData();

    /// <summary>
    /// Validates a JSON content to be an AppTheme.
    /// </summary>
    /// <param name="json">The JSON content to validate.</param>
    /// <returns>True if the JSON content is valid, otherwise false.</returns>
    bool ValidateAppTheme(string? json);

    /// <summary>
    /// Gets all available themes from assets and storage.
    /// </summary>
    /// <returns>A list of AppTheme objects.</returns>
    Task<List<AppTheme>> GetAllThemesAsync();

    /// <summary>
    /// Gets the default theme for the specified theme variant.
    /// </summary>
    /// <param name="isDark">True if the theme is dark, false otherwise.</param>
    /// <returns>The default AppTheme for the specified theme variant.</returns>
    Task<AppTheme?> GetDefaultThemeAsync(bool isDark);
}
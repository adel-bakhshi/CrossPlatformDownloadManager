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
}
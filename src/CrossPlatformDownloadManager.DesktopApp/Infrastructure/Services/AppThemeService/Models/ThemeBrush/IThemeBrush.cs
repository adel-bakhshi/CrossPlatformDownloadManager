namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;

/// <summary>
/// Represents a theme brush that can be either solid or gradient.
/// </summary>
public interface IThemeBrush
{
    #region Properties

    /// <summary>
    /// Gets the brush mode (Solid or Gradient).
    /// </summary>
    ThemeBrushMode BrushMode { get; }

    #endregion

    /// <summary>
    /// Validates the brush data.
    /// </summary>
    /// <returns>Returns true if the brush is valid, otherwise false.</returns>
    bool Validate();

    /// <summary>
    /// Creates and returns the brush object.
    /// </summary>
    /// <returns>Returns the created brush object.</returns>
    object GetBrush();
}
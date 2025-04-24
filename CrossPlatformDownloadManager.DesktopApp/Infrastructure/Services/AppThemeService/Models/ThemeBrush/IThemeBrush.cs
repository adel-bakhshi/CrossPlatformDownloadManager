using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;

public interface IThemeBrush
{
    #region Properties

    ThemeBrushMode BrushMode { get; }

    #endregion

    bool Validate();

    object GetBrush();
}
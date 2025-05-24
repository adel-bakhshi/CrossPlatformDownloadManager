using Avalonia.Media;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;

public class ThemeGradientStop
{
    #region Properties

    [JsonProperty("offset")]
    public double Offset { get; set; }

    [JsonProperty("color")]
    public ThemeSolidBrush? Color { get; set; }

    #endregion

    public bool Validate()
    {
        if (Offset is < 0 or > 1 || Color == null)
            return false;

        return Color.Validate();
    }

    public GradientStop CreateGradientStop()
    {
        var color = Color!.GetBrush() as Color?;
        return new GradientStop(color!.Value, Offset);
    }
}
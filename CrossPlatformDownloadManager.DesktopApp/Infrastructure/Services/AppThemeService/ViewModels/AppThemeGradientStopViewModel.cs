using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.ViewModels;

public class AppThemeGradientStopViewModel
{
    #region Properties

    [JsonProperty("offset")]
    public double Offset { get; set; }

    [JsonProperty("color")]
    public AppThemeColorViewModel? Color { get; set; }

    #endregion

    public bool Validate()
    {
        if (Offset is < 0 or > 1 || Color == null)
            return false;

        return Color.Validate();
    }

    public GradientStop CreateGradientStop()
    {
        var color = Color!.GetColor();
        return new GradientStop(color, Offset);
    }
}
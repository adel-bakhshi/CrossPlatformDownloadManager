using System;
using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;

public class ThemeSolidBrush : IThemeBrush
{
    #region Properties

    [JsonProperty("brushMode")]
    public ThemeBrushMode BrushMode => ThemeBrushMode.Solid;

    [JsonProperty("color")]
    public string? Color { get; set; }

    [JsonProperty("opacity")]
    public double Opacity { get; set; }

    #endregion

    public bool Validate()
    {
        if (Opacity is < 0 or > 1)
            return false;

        return Color.ConvertFromHex() != null;
    }

    public object GetBrush()
    {
        var color = Color.ConvertFromHex()!.Value;
        var alpha = (byte)Math.Round(Opacity * 255);
        return new Color(alpha, color.R, color.G, color.B);
    }
}
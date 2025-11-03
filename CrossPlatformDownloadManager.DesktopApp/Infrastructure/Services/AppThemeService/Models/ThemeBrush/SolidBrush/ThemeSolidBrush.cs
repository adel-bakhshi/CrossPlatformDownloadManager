using System;
using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;

/// <summary>
/// Represents a solid color theme brush.
/// </summary>
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

    /// <summary>
    /// Validates the solid brush properties.
    /// </summary>
    /// <returns>Returns true if the brush is valid, otherwise false.</returns>
    public bool Validate()
    {
        Log.Debug("Validating solid theme brush. Color: {Color}, Opacity: {Opacity}", Color, Opacity);

        if (Opacity is < 0 or > 1)
        {
            Log.Warning("Solid brush opacity is out of range: {Opacity}", Opacity);
            return false;
        }

        var isValid = Color.ConvertFromHex() != null;
        Log.Debug("Solid brush validation result: {IsValid}", isValid);
        return isValid;
    }

    /// <summary>
    /// Creates and returns a solid color brush.
    /// </summary>
    /// <returns>Returns the created color object.</returns>
    public object GetBrush()
    {
        Log.Debug("Creating solid brush from color: {Color}, opacity: {Opacity}", Color, Opacity);

        var color = Color.ConvertFromHex()!.Value;
        var alpha = (byte)Math.Round(Opacity * 255);
        var result = new Color(alpha, color.R, color.G, color.B);

        Log.Debug("Solid brush created successfully with alpha: {Alpha}", alpha);
        return result;
    }
}
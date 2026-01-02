using Avalonia.Media;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;

/// <summary>
/// Represents a gradient stop in a gradient brush.
/// </summary>
public class ThemeGradientStop
{
    #region Properties

    [JsonProperty("offset")]
    public double Offset { get; set; }

    [JsonProperty("color")]
    public ThemeSolidBrush? Color { get; set; }

    #endregion

    /// <summary>
    /// Validates the gradient stop properties.
    /// </summary>
    /// <returns>Returns true if the gradient stop is valid, otherwise false.</returns>
    public bool Validate()
    {
        if (Offset is < 0 or > 1 || Color == null)
        {
            Log.Warning("Gradient stop validation failed - invalid offset or null color. Offset: {Offset}, Color: {Color}", Offset, Color);
            return false;
        }

        var colorValid = Color.Validate();
        if (!colorValid)
            Log.Debug("Gradient stop color validation failed. Color: {Color}, Offset: {Offset}", Color, Offset);

        return colorValid;
    }

    /// <summary>
    /// Creates and returns a gradient stop object.
    /// </summary>
    /// <returns>Returns the created gradient stop object.</returns>
    public GradientStop CreateGradientStop()
    {
        var color = Color!.GetBrush() as Color?;
        return new GradientStop(color!.Value, Offset);
    }
}
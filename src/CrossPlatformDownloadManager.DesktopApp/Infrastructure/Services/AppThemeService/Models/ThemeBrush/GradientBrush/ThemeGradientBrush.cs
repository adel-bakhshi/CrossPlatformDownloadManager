using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;

/// <summary>
/// Represents a gradient theme brush.
/// </summary>
public class ThemeGradientBrush : IThemeBrush
{
    #region Properties

    [JsonProperty("brushMode")]
    public ThemeBrushMode BrushMode => ThemeBrushMode.Gradient;

    [JsonProperty("startPoint")]
    public string? StartPoint { get; set; }

    [JsonProperty("endPoint")]
    public string? EndPoint { get; set; }

    [JsonProperty("gradientStops")]
    public List<ThemeGradientStop> GradientStops { get; set; } = [];

    #endregion

    /// <summary>
    /// Validates the gradient brush properties.
    /// </summary>
    /// <returns>Returns true if the brush is valid, otherwise false.</returns>
    public bool Validate()
    {
        if (!ValidatePoint(StartPoint) || !ValidatePoint(EndPoint) || GradientStops.Count == 0)
        {
            Log.Warning("Gradient brush validation failed - invalid points or empty gradient stops.");
            return false;
        }

        var allStopsValid = GradientStops.TrueForAll(gs => gs.Validate());
        if (!allStopsValid)
            Log.Debug("Gradient stop validation failed.");

        return allStopsValid;
    }

    /// <summary>
    /// Creates and returns a linear gradient brush.
    /// </summary>
    /// <returns>Returns the created gradient brush object.</returns>
    public object GetBrush()
    {
        var startPoint = StartPoint!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture))
            .ToList();

        var endPoint = EndPoint!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture))
            .ToList();

        var gradientBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(startPoint[0], startPoint[1], RelativeUnit.Relative),
            EndPoint = new RelativePoint(endPoint[0], endPoint[1], RelativeUnit.Relative)
        };

        var stops = GradientStops.ConvertAll(gs => gs.CreateGradientStop());
        gradientBrush.GradientStops.AddRange(stops);

        return gradientBrush;
    }

    #region Helpers

    /// <summary>
    /// Validates a point string format.
    /// </summary>
    /// <param name="pointString">The point string to validate.</param>
    /// <returns>Returns true if the point string is valid, otherwise false.</returns>
    private static bool ValidatePoint(string? pointString)
    {
        if (pointString.IsStringNullOrEmpty())
        {
            Log.Debug("Point string is null or empty.");
            return false;
        }

        var points = pointString!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
        if (points.Count != 2)
        {
            Log.Debug("Point string does not contain exactly 2 values: {PointString}", pointString);
            return false;
        }

        foreach (var point in points)
        {
            if (!double.TryParse(point, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                Log.Debug("Point value is not a valid number: {Point}", point);
                return false;
            }

            if (value is < 0 or > 1)
            {
                Log.Debug("Point value is out of range [0,1]: {Value}", value);
                return false;
            }
        }

        return true;
    }

    #endregion
}
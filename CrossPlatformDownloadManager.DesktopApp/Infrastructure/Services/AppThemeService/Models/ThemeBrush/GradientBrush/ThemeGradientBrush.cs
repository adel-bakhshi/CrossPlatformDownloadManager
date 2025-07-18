using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;

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

    public bool Validate()
    {
        if (!ValidatePoint(StartPoint) || !ValidatePoint(EndPoint) || GradientStops.Count == 0)
            return false;

        return GradientStops.TrueForAll(gs => gs.Validate());
    }

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

    private static bool ValidatePoint(string? pointString)
    {
        if (pointString.IsStringNullOrEmpty())
            return false;

        var points = pointString!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
        if (points.Count != 2)
            return false;

        foreach (var point in points)
        {
            if (!double.TryParse(point, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return false;

            if (value is < 0 or > 1)
                return false;
        }

        return true;
    }

    #endregion
}
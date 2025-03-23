using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.ViewModels;

public class AppThemeGradientViewModel
{
    #region Properties

    [JsonProperty("startPoint")]
    public string? StartPoint { get; set; }

    [JsonProperty("endPoint")]
    public string? EndPoint { get; set; }

    [JsonProperty("gradientStops")]
    public List<AppThemeGradientStopViewModel> GradientStops { get; set; } = [];

    #endregion

    public bool Validate()
    {
        if (!ValidatePoint(StartPoint) || !ValidatePoint(EndPoint) || GradientStops.Count == 0)
            return false;

        return GradientStops.TrueForAll(gs => gs.Validate());
    }

    public LinearGradientBrush CreateGradientBrush()
    {
        var startPoint = StartPoint!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => double.Parse(p.Trim()))
            .ToList();

        var endPoint = EndPoint!
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => double.Parse(p.Trim()))
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
        if (pointString.IsNullOrEmpty())
            return false;

        var points = pointString!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
        if (points.Count != 2)
            return false;

        foreach (var point in points)
        {
            if (!double.TryParse(point, out var value))
                return false;

            if (value is < 0 or > 1)
                return false;
        }

        return true;
    }

    #endregion
}
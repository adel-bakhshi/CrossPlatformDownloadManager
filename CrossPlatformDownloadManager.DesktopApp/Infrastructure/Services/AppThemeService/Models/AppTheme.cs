using System;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;

public class AppTheme
{
    #region Properties

    [JsonProperty("themeName")]
    public string ThemeName { get; set; } = string.Empty;

    [JsonProperty("isDarkTheme")]
    public bool IsDarkTheme { get; set; }

    [JsonProperty("mainBackgroundColor")]
    public IThemeBrush? MainBackgroundColor { get; set; }

    [JsonProperty("secondaryBackgroundColor")]
    public IThemeBrush? SecondaryBackgroundColor { get; set; }

    [JsonProperty("accentColor")]
    public IThemeBrush? AccentColor { get; set; }

    [JsonProperty("mainTextColor")]
    public IThemeBrush? MainTextColor { get; set; }

    [JsonProperty("buttonTextColor")]
    public IThemeBrush? ButtonTextColor { get; set; }

    [JsonProperty("categoryHoverColor")]
    public IThemeBrush? CategoryHoverColor { get; set; }

    [JsonProperty("menuBackgroundColor")]
    public IThemeBrush? MenuBackgroundColor { get; set; }

    [JsonProperty("menuItemHoverColor")]
    public IThemeBrush? MenuItemHoverColor { get; set; }

    [JsonProperty("iconColor")]
    public IThemeBrush? IconColor { get; set; }

    [JsonProperty("selectedProxyColor")]
    public IThemeBrush? SelectedProxyColor { get; set; }

    [JsonProperty("toggleCircleColor")]
    public IThemeBrush? ToggleCircleColor { get; set; }

    [JsonProperty("loadingIndicatorColor")]
    public IThemeBrush? LoadingIndicatorColor { get; set; }

    [JsonProperty("dialogTextColor")]
    public IThemeBrush? DialogTextColor { get; set; }

    [JsonProperty("dialogOkBackgroundColor")]
    public IThemeBrush? DialogOkBackgroundColor { get; set; }

    [JsonProperty("dialogYesBackgroundColor")]
    public IThemeBrush? DialogYesBackgroundColor { get; set; }

    [JsonProperty("dialogNoBackgroundColor")]
    public IThemeBrush? DialogNoBackgroundColor { get; set; }

    [JsonProperty("dialogCancelBackgroundColor")]
    public IThemeBrush? DialogCancelBackgroundColor { get; set; }

    [JsonProperty("managerTextColor")]
    public IThemeBrush? ManagerTextColor { get; set; }

    [JsonProperty("chunkProgressBackgroundColor")]
    public IThemeBrush? ChunkProgressBackgroundColor { get; set; }

    [JsonProperty("mainColor")]
    public IThemeBrush? MainColor { get; set; }

    [JsonProperty("successColor")]
    public IThemeBrush? SuccessColor { get; set; }

    [JsonProperty("infoColor")]
    public IThemeBrush? InfoColor { get; set; }

    [JsonProperty("dangerColor")]
    public IThemeBrush? DangerColor { get; set; }

    [JsonProperty("warningColor")]
    public IThemeBrush? WarningColor { get; set; }

    [JsonProperty("chunkProgressColor")]
    public IThemeBrush? ChunkProgressColor { get; set; }

    [JsonProperty("gridRowColor")]
    public IThemeBrush? GridRowColor { get; set; }

    #endregion

    public bool Validate()
    {
        // Get all properties that are of type IThemeBrush
        var properties = GetType().GetProperties().Where(p => p.PropertyType == typeof(IThemeBrush)).ToList();
        // Validate properties
        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            switch (value)
            {
                case null:
                    return false;

                case IThemeBrush themeBrush:
                {
                    if (!themeBrush.Validate())
                        return false;

                    break;
                }

                default:
                    throw new InvalidOperationException($"The type of {property.Name} is invalid.");
            }
        }

        return true;
    }
}
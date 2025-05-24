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

    [JsonProperty("primaryColor")]
    public IThemeBrush? PrimaryColor { get; set; }

    [JsonProperty("secondaryColor")]
    public IThemeBrush? SecondaryColor { get; set; }

    [JsonProperty("tertiaryColor")]
    public IThemeBrush? TertiaryColor { get; set; }

    [JsonProperty("textColor")]
    public IThemeBrush? TextColor { get; set; }

    [JsonProperty("buttonTextColor")]
    public IThemeBrush? ButtonTextColor { get; set; }

    [JsonProperty("categoryItemOnHoverColor")]
    public IThemeBrush? CategoryItemOnHoverColor { get; set; }

    [JsonProperty("menuBackgroundColor")]
    public IThemeBrush? MenuBackgroundColor { get; set; }

    [JsonProperty("menuItemOnHoverBackgroundColor")]
    public IThemeBrush? MenuItemOnHoverBackgroundColor { get; set; }

    [JsonProperty("iconColor")]
    public IThemeBrush? IconColor { get; set; }

    [JsonProperty("selectedAvailableProxyTypeColor")]
    public IThemeBrush? SelectedAvailableProxyTypeColor { get; set; }

    [JsonProperty("toggleSwitchCircleColor")]
    public IThemeBrush? ToggleSwitchCircleColor { get; set; }

    [JsonProperty("loadingColor")]
    public IThemeBrush? LoadingColor { get; set; }

    [JsonProperty("dialogTextColor")]
    public IThemeBrush? DialogTextColor { get; set; }

    [JsonProperty("dialogOkButtonBackgroundColor")]
    public IThemeBrush? DialogOkButtonBackgroundColor { get; set; }

    [JsonProperty("dialogYesButtonBackgroundColor")]
    public IThemeBrush? DialogYesButtonBackgroundColor { get; set; }

    [JsonProperty("dialogNoButtonBackgroundColor")]
    public IThemeBrush? DialogNoButtonBackgroundColor { get; set; }

    [JsonProperty("dialogCancelButtonBackgroundColor")]
    public IThemeBrush? DialogCancelButtonBackgroundColor { get; set; }

    [JsonProperty("managerTextColor")]
    public IThemeBrush? ManagerTextColor { get; set; }

    [JsonProperty("primaryGradientBrush")]
    public IThemeBrush? PrimaryGradientBrush { get; set; }

    [JsonProperty("successGradientBrush")]
    public IThemeBrush? SuccessGradientBrush { get; set; }

    [JsonProperty("infoGradientBrush")]
    public IThemeBrush? InfoGradientBrush { get; set; }

    [JsonProperty("dangerGradientBrush")]
    public IThemeBrush? DangerGradientBrush { get; set; }

    [JsonProperty("warningGradientBrush")]
    public IThemeBrush? WarningGradientBrush { get; set; }

    [JsonProperty("chunkProgressGradientBrush")]
    public IThemeBrush? ChunkProgressGradientBrush { get; set; }

    [JsonProperty("dataGridRowGradientBrush")]
    public IThemeBrush? DataGridRowGradientBrush { get; set; }

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
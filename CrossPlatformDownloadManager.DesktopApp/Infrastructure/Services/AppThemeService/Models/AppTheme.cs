using System;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;

/// <summary>
/// Represents an application theme with various color properties.
/// </summary>
public class AppTheme
{
    #region Properties

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("author")]
    public string Author { get; set; } = string.Empty;

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

    /// <summary>
    /// Gets or sets a value indicating whether the theme is the default app theme.
    /// </summary>
    [JsonIgnore]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the path to the theme file.
    /// </summary>
    [JsonIgnore]
    public string? Path { get; set; }

    #endregion

    /// <summary>
    /// Validates the theme data to ensure all required properties are set and valid.
    /// </summary>
    /// <returns>Returns true if the theme is valid, otherwise false.</returns>
    public bool Validate()
    {
        Log.Debug("Validating app theme: {Name}", Name);

        // Get all properties that are of type IThemeBrush
        var properties = GetType().GetProperties().Where(p => p.PropertyType == typeof(IThemeBrush)).ToList();
        Log.Debug("Found {PropertyCount} theme brush properties to validate.", properties.Count);

        // Validate properties
        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            switch (value)
            {
                case null:
                {
                    Log.Warning("Theme brush property {PropertyName} is null.", property.Name);
                    return false;
                }

                case IThemeBrush themeBrush:
                {
                    if (!themeBrush.Validate())
                    {
                        Log.Warning("Theme brush property {PropertyName} failed validation.", property.Name);
                        return false;
                    }

                    break;
                }

                default:
                {
                    Log.Error("Invalid type for property {PropertyName}. Expected IThemeBrush.", property.Name);
                    throw new InvalidOperationException($"The type of {property.Name} is invalid.");
                }
            }
        }

        Log.Debug("App theme validation successful: {Name}", Name);
        return true;
    }
}
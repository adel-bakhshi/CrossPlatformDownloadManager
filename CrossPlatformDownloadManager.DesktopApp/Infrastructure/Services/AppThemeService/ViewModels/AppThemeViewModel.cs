using System;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.ViewModels;

public class AppThemeViewModel
{
    #region Properties

    [JsonProperty("primaryColor")]
    public AppThemeColorViewModel? PrimaryColor { get; set; }

    [JsonProperty("secondaryColor")]
    public AppThemeColorViewModel? SecondaryColor { get; set; }

    [JsonProperty("tertiaryColor")]
    public AppThemeColorViewModel? TertiaryColor { get; set; }

    [JsonProperty("textColor")]
    public AppThemeColorViewModel? TextColor { get; set; }

    [JsonProperty("buttonTextColor")]
    public AppThemeColorViewModel? ButtonTextColor { get; set; }

    [JsonProperty("categoryItemOnHoverColor")]
    public AppThemeColorViewModel? CategoryItemOnHoverColor { get; set; }

    [JsonProperty("menuBackgroundColor")]
    public AppThemeColorViewModel? MenuBackgroundColor { get; set; }

    [JsonProperty("menuItemOnHoverBackgroundColor")]
    public AppThemeColorViewModel? MenuItemOnHoverBackgroundColor { get; set; }

    [JsonProperty("iconColor")]
    public AppThemeColorViewModel? IconColor { get; set; }

    [JsonProperty("selectedAvailableProxyTypeColor")]
    public AppThemeColorViewModel? SelectedAvailableProxyTypeColor { get; set; }

    [JsonProperty("toggleSwitchCircleColor")]
    public AppThemeColorViewModel? ToggleSwitchCircleColor { get; set; }

    [JsonProperty("loadingColor")]
    public AppThemeColorViewModel? LoadingColor { get; set; }

    [JsonProperty("dialogTextColor")]
    public AppThemeColorViewModel? DialogTextColor { get; set; }

    [JsonProperty("dialogOkButtonBackgroundColor")]
    public AppThemeColorViewModel? DialogOkButtonBackgroundColor { get; set; }

    [JsonProperty("dialogYesButtonBackgroundColor")]
    public AppThemeColorViewModel? DialogYesButtonBackgroundColor { get; set; }

    [JsonProperty("dialogNoButtonBackgroundColor")]
    public AppThemeColorViewModel? DialogNoButtonBackgroundColor { get; set; }

    [JsonProperty("dialogCancelButtonBackgroundColor")]
    public AppThemeColorViewModel? DialogCancelButtonBackgroundColor { get; set; }

    [JsonProperty("managerTextColor")]
    public AppThemeColorViewModel? ManagerTextColor { get; set; }

    [JsonProperty("primaryGradientBrush")]
    public AppThemeGradientViewModel? PrimaryGradientBrush { get; set; }

    [JsonProperty("successGradientBrush")]
    public AppThemeGradientViewModel? SuccessGradientBrush { get; set; }

    [JsonProperty("infoGradientBrush")]
    public AppThemeGradientViewModel? InfoGradientBrush { get; set; }

    [JsonProperty("dangerGradientBrush")]
    public AppThemeGradientViewModel? DangerGradientBrush { get; set; }

    [JsonProperty("warningGradientBrush")]
    public AppThemeGradientViewModel? WarningGradientBrush { get; set; }

    [JsonProperty("chunkProgressGradientBrush")]
    public AppThemeGradientViewModel? ChunkProgressGradientBrush { get; set; }

    [JsonProperty("dataGridRowGradientBrush")]
    public AppThemeGradientViewModel? DataGridRowGradientBrush { get; set; }

    #endregion

    public bool Validate()
    {
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            switch (value)
            {
                case null:
                    return false;

                case AppThemeColorViewModel viewModel:
                {
                    if (!viewModel.Validate())
                        return false;

                    break;
                }

                case AppThemeGradientViewModel viewModel:
                {
                    var isValid = viewModel.Validate();
                    if (!isValid)
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
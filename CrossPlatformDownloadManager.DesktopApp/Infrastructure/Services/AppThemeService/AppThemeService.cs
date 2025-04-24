using System;
using Avalonia;
using Avalonia.Styling;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.JsonConverters;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;

public class AppThemeService : IAppThemeService
{
    #region Private Fields

    private readonly ISettingsService _settingsService;
    private string? _lastThemeName;

    #endregion

    public AppThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void LoadThemeDataFromAssets()
    {
        // Check last theme. If same, return
        if (!_lastThemeName.IsStringNullOrEmpty())
            return;

        // Get and validate resources
        var resources = Application.Current?.Resources;
        if (resources == null)
            return;

        // Get theme data from assets
        var assetPath = $"avares://CrossPlatformDownloadManager.DesktopApp/Assets/Themes/{(_settingsService.Settings.DarkMode ? "dark-theme.json" : "light-theme.json")}";
        var assetUri = new Uri(assetPath);
        // Json serializer settings
        var settings = new JsonSerializerSettings { Converters = [new ThemeBrushJsonConverter()] };
        // Convert json to app theme
        var appTheme = assetUri.OpenJsonAsset<AppTheme>(settings);
        if (appTheme == null)
            throw new InvalidOperationException("Unable to load app theme.");

        // Validate app theme
        if (!appTheme.Validate())
        {
            throw new InvalidOperationException(
                "The theme data appears to be corrupted or invalid. Please try again. If the problem continues, please reach out to the developers for assistance.");
        }

        // Load theme data
        resources["PrimaryColor"] = appTheme.PrimaryColor!.GetBrush();
        resources["SecondaryColor"] = appTheme.SecondaryColor!.GetBrush();
        resources["TertiaryColor"] = appTheme.TertiaryColor!.GetBrush();
        resources["TextColor"] = appTheme.TextColor!.GetBrush();
        resources["ButtonTextColor"] = appTheme.ButtonTextColor!.GetBrush();
        resources["CategoryItemOnHoverColor"] = appTheme.CategoryItemOnHoverColor!.GetBrush();
        resources["MenuBackgroundColor"] = appTheme.MenuBackgroundColor!.GetBrush();
        resources["MenuItemOnHoverBackgroundColor"] = appTheme.MenuItemOnHoverBackgroundColor!.GetBrush();
        resources["IconColor"] = appTheme.IconColor!.GetBrush();
        resources["SelectedAvailableProxyTypeColor"] = appTheme.SelectedAvailableProxyTypeColor!.GetBrush();
        resources["ToggleSwitchCircleColor"] = appTheme.ToggleSwitchCircleColor!.GetBrush();
        resources["LoadingColor"] = appTheme.LoadingColor!.GetBrush();
        resources["DialogTextColor"] = appTheme.DialogTextColor!.GetBrush();
        resources["DialogOkButtonBackgroundColor"] = appTheme.DialogOkButtonBackgroundColor!.GetBrush();
        resources["DialogYesButtonBackgroundColor"] = appTheme.DialogYesButtonBackgroundColor!.GetBrush();
        resources["DialogNoButtonBackgroundColor"] = appTheme.DialogNoButtonBackgroundColor!.GetBrush();
        resources["DialogCancelButtonBackgroundColor"] = appTheme.DialogCancelButtonBackgroundColor!.GetBrush();
        resources["ManagerTextColor"] = appTheme.ManagerTextColor!.GetBrush();
        resources["PrimaryGradientBrush"] = appTheme.PrimaryGradientBrush!.GetBrush();
        resources["SuccessGradientBrush"] = appTheme.SuccessGradientBrush!.GetBrush();
        resources["InfoGradientBrush"] = appTheme.InfoGradientBrush!.GetBrush();
        resources["DangerGradientBrush"] = appTheme.DangerGradientBrush!.GetBrush();
        resources["WarningGradientBrush"] = appTheme.WarningGradientBrush!.GetBrush();
        resources["ChunkProgressGradientBrush"] = appTheme.ChunkProgressGradientBrush!.GetBrush();
        resources["DataGridRowGradientBrush"] = appTheme.DataGridRowGradientBrush!.GetBrush();

        if (Application.Current?.RequestedThemeVariant != null)
            Application.Current.RequestedThemeVariant = appTheme.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

        // Set last theme flag
        _lastThemeName = appTheme.ThemeName;
        // Log information
        Log.Information($"App theme ({(_settingsService.Settings.DarkMode ? "Dark" : "Light")}) loaded successfully.");
    }
}
using System;
using Avalonia;
using Avalonia.Styling;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;

public class AppThemeService : IAppThemeService
{
    #region Private Fields

    private readonly ISettingsService _settingsService;
    private bool? _isLastThemeDarkMode;

    #endregion

    public AppThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void LoadThemeDataFromAssets()
    {
        // Check last theme. If same, return
        if (_isLastThemeDarkMode != null && _isLastThemeDarkMode == _settingsService.Settings.DarkMode)
            return;
        
        // Get and validate resources
        var resources = Application.Current?.Resources;
        if (resources == null)
            return;

        // Get theme data from assets
        var assetPath = $"avares://CrossPlatformDownloadManager.DesktopApp/Assets/Themes/{(_settingsService.Settings.DarkMode ? "dark-theme.json" : "light-theme.json")}";
        var assetUri = new Uri(assetPath);
        // Convert json to app theme
        var appTheme = assetUri.OpenJsonAsset<AppThemeViewModel>();
        if (appTheme == null)
            throw new InvalidOperationException("Unable to load app theme.");

        // Validate app theme
        if (!appTheme.Validate())
        {
            throw new InvalidOperationException(
                "The theme data appears to be corrupted or invalid. Please try again. If the problem continues, please reach out to the developers for assistance.");
        }

        // Load theme data
        resources["PrimaryColor"] = appTheme.PrimaryColor!.GetColor();
        resources["SecondaryColor"] = appTheme.SecondaryColor!.GetColor();
        resources["TertiaryColor"] = appTheme.TertiaryColor!.GetColor();
        resources["TextColor"] = appTheme.TextColor!.GetColor();
        resources["ButtonTextColor"] = appTheme.ButtonTextColor!.GetColor();
        resources["CategoryItemOnHoverColor"] = appTheme.CategoryItemOnHoverColor!.GetColor();
        resources["MenuBackgroundColor"] = appTheme.MenuBackgroundColor!.GetColor();
        resources["MenuItemOnHoverBackgroundColor"] = appTheme.MenuItemOnHoverBackgroundColor!.GetColor();
        resources["IconColor"] = appTheme.IconColor!.GetColor();
        resources["SelectedAvailableProxyTypeColor"] = appTheme.SelectedAvailableProxyTypeColor!.GetColor();
        resources["ToggleSwitchCircleColor"] = appTheme.ToggleSwitchCircleColor!.GetColor();
        resources["LoadingColor"] = appTheme.LoadingColor!.GetColor();
        resources["DialogTextColor"] = appTheme.DialogTextColor!.GetColor();
        resources["DialogOkButtonBackgroundColor"] = appTheme.DialogOkButtonBackgroundColor!.GetColor();
        resources["DialogYesButtonBackgroundColor"] = appTheme.DialogYesButtonBackgroundColor!.GetColor();
        resources["DialogNoButtonBackgroundColor"] = appTheme.DialogNoButtonBackgroundColor!.GetColor();
        resources["DialogCancelButtonBackgroundColor"] = appTheme.DialogCancelButtonBackgroundColor!.GetColor();
        resources["ManagerTextColor"] = appTheme.ManagerTextColor!.GetColor();
        resources["PrimaryGradientBrush"] = appTheme.PrimaryGradientBrush!.CreateGradientBrush();
        resources["SuccessGradientBrush"] = appTheme.SuccessGradientBrush!.CreateGradientBrush();
        resources["InfoGradientBrush"] = appTheme.InfoGradientBrush!.CreateGradientBrush();
        resources["DangerGradientBrush"] = appTheme.DangerGradientBrush!.CreateGradientBrush();
        resources["WarningGradientBrush"] = appTheme.WarningGradientBrush!.CreateGradientBrush();
        resources["ChunkProgressGradientBrush"] = appTheme.ChunkProgressGradientBrush!.CreateGradientBrush();
        resources["DataGridRowGradientBrush"] = appTheme.DataGridRowGradientBrush!.CreateGradientBrush();

        if (Application.Current?.RequestedThemeVariant != null)
            Application.Current.RequestedThemeVariant = _settingsService.Settings.DarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        
        // Set last theme flag
        _isLastThemeDarkMode = _settingsService.Settings.DarkMode;
        // Log information
        Log.Information($"App theme ({(_settingsService.Settings.DarkMode ? "Dark" : "Light")}) loaded successfully.");
    }
}
using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.JsonConverters;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.SettingsService;
using CrossPlatformDownloadManager.Utils;
using Newtonsoft.Json;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService;

/// <summary>
/// Service for managing application themes.
/// </summary>
public class AppThemeService : IAppThemeService
{
    #region Private Fields

    private readonly ISettingsService _settingsService;
    private string? _lastThemePath;

    #endregion

    public AppThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void LoadThemeData()
    {
        // Get the theme file path and set default light theme file path if it's empty or theme file not found
        var themeFilePath = _settingsService.Settings.ThemeFilePath;
        if (themeFilePath.IsStringNullOrEmpty()
            || (themeFilePath?.Contains("avares://", StringComparison.OrdinalIgnoreCase) == false && !File.Exists(themeFilePath)))
        {
            themeFilePath = Constants.LightThemeFilePath;
        }

        // Check if current theme is equal to last theme
        if (themeFilePath!.Equals(_lastThemePath))
            return;

        // Read JSON data
        string? jsonData;
        try
        {
            jsonData = themeFilePath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase)
                ? LoadThemeDataFromAssets(themeFilePath)
                : LoadThemeDataFromStorage(themeFilePath);
        }
        catch
        {
            // Log error
            Log.Error("Failed to load theme data from {ThemeFilePath}.", themeFilePath);
            // Load light theme
            jsonData = LoadThemeDataFromAssets(Constants.LightThemeFilePath);
        }

        // Convert JSON data to AppTheme object
        var appTheme = ConvertJsonToAppTheme(jsonData);
        if (appTheme == null)
            throw new InvalidCastException("App theme data is null or undefined.");

        // Apply theme data
        ApplyAppTheme(appTheme);
        // Set the last theme path
        _lastThemePath = themeFilePath;
        // Log information
        Log.Information("{ThemeName} theme loaded successfully.", appTheme.ThemeName);
    }

    public bool ValidateAppTheme(string? json)
    {
        if (json.IsStringNullOrEmpty())
            return false;

        var appTheme = ConvertJsonToAppTheme(json);
        return appTheme != null && appTheme.Validate();
    }

    #region Helpers

    /// <summary>
    /// Loads theme data from Avalonia assets.
    /// </summary>
    /// <param name="themeFilePath">Theme file path based on Avalonia path system.</param>
    /// <returns>Returns the theme data as JSON string.</returns>
    private static string? LoadThemeDataFromAssets(string themeFilePath)
    {
        return themeFilePath.OpenTextAsset();
    }

    /// <summary>
    /// Loads theme data from storage.
    /// </summary>
    /// <param name="themeFilePath">Theme file path.</param>
    /// <returns>Returns the theme data as JSON string.</returns>
    private static string? LoadThemeDataFromStorage(string themeFilePath)
    {
        return !File.Exists(themeFilePath) ? null : File.ReadAllText(themeFilePath);
    }

    /// <summary>
    /// Converts JSON data to AppTheme object.
    /// </summary>
    /// <param name="jsonData">JSON data as string.</param>
    /// <returns>Returns the AppTheme object.</returns>
    private static AppTheme? ConvertJsonToAppTheme(string? jsonData)
    {
        if (jsonData.IsStringNullOrEmpty())
            return null;

        // Json serializer settings for converting IThemeBrush
        var settings = new JsonSerializerSettings
        {
            Converters = [new ThemeBrushJsonConverter()],
            Culture = CultureInfo.InvariantCulture
        };

        return jsonData.ConvertFromJson<AppTheme?>(settings);
    }

    /// <summary>
    /// Applies the theme data to the application.
    /// </summary>
    /// <param name="appTheme">AppTheme object.</param>
    /// <exception cref="InvalidOperationException">Thrown when the theme data is corrupted or invalid.</exception>
    private static void ApplyAppTheme(AppTheme appTheme)
    {
        // Get and validate resources
        var resources = Application.Current?.Resources;
        if (resources == null)
            return;

        // Validate app theme
        if (!appTheme.Validate())
        {
            const string message = "The theme data appears to be corrupted or invalid. Please try again. " +
                                   "If the problem continues, please reach out to the developers for assistance.";

            throw new InvalidOperationException(message);
        }

        // Load theme data
        resources["MainBackgroundColor"] = appTheme.MainBackgroundColor!.GetBrush();
        resources["SecondaryBackgroundColor"] = appTheme.SecondaryBackgroundColor!.GetBrush();
        resources["AccentColor"] = appTheme.AccentColor!.GetBrush();
        resources["MainTextColor"] = appTheme.MainTextColor!.GetBrush();
        resources["ButtonTextColor"] = appTheme.ButtonTextColor!.GetBrush();
        resources["CategoryHoverColor"] = appTheme.CategoryHoverColor!.GetBrush();
        resources["MenuBackgroundColor"] = appTheme.MenuBackgroundColor!.GetBrush();
        resources["MenuItemHoverColor"] = appTheme.MenuItemHoverColor!.GetBrush();
        resources["IconColor"] = appTheme.IconColor!.GetBrush();
        resources["SelectedProxyColor"] = appTheme.SelectedProxyColor!.GetBrush();
        resources["ToggleCircleColor"] = appTheme.ToggleCircleColor!.GetBrush();
        resources["LoadingIndicatorColor"] = appTheme.LoadingIndicatorColor!.GetBrush();
        resources["DialogTextColor"] = appTheme.DialogTextColor!.GetBrush();
        resources["DialogOkBackgroundColor"] = appTheme.DialogOkBackgroundColor!.GetBrush();
        resources["DialogYesBackgroundColor"] = appTheme.DialogYesBackgroundColor!.GetBrush();
        resources["DialogNoBackgroundColor"] = appTheme.DialogNoBackgroundColor!.GetBrush();
        resources["DialogCancelBackgroundColor"] = appTheme.DialogCancelBackgroundColor!.GetBrush();
        resources["ManagerTextColor"] = appTheme.ManagerTextColor!.GetBrush();
        resources["MainColor"] = appTheme.MainColor!.GetBrush();
        resources["SuccessColor"] = appTheme.SuccessColor!.GetBrush();
        resources["InfoColor"] = appTheme.InfoColor!.GetBrush();
        resources["DangerColor"] = appTheme.DangerColor!.GetBrush();
        resources["WarningColor"] = appTheme.WarningColor!.GetBrush();
        resources["ProgressColor"] = appTheme.ProgressColor!.GetBrush();
        resources["GridRowColor"] = appTheme.GridRowColor!.GetBrush();

        // Set application theme variant
        if (Application.Current?.RequestedThemeVariant != null)
            Application.Current.RequestedThemeVariant = appTheme.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    #endregion
}
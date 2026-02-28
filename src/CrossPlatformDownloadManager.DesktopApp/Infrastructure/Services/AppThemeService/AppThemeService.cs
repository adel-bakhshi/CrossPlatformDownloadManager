using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
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

    /// <summary>
    /// The settings service instance to access application settings.
    /// </summary>
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// The path of the last theme that loaded.
    /// </summary>
    private string? _lastThemePath;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AppThemeService"/> class.
    /// </summary>
    /// <param name="settingsService">The settings service instance.</param>
    public AppThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Log.Debug("AppThemeService initialized successfully.");
    }

    public async Task LoadThemeDataAsync()
    {
        Log.Information("Loading theme data...");

        // Get the theme file path and set default light theme file path if it's empty or theme file not found
        var themeFilePath = _settingsService.Settings.ThemeFilePath;
        Log.Debug("Current theme file path from settings: {ThemeFilePath}", themeFilePath);

        if (themeFilePath.IsStringNullOrEmpty()
            || (themeFilePath?.Contains("avares://", StringComparison.OrdinalIgnoreCase) == false && !File.Exists(themeFilePath)))
        {
            Log.Debug("Theme file path is invalid or file not found. Using default light theme.");
            themeFilePath = Constants.LightThemeFilePath;
        }

        // Check if current theme is equal to last theme
        if (themeFilePath!.Equals(_lastThemePath))
        {
            Log.Debug("Theme is already loaded. Skipping reload.");
            return;
        }

        // Read JSON data
        string? jsonData;
        try
        {
            jsonData = themeFilePath.StartsWith("avares://", StringComparison.OrdinalIgnoreCase)
                ? await LoadThemeDataFromAssetsAsync(themeFilePath)
                : await LoadThemeDataFromStorageAsync(themeFilePath);

            Log.Debug("Theme data loaded successfully from: {ThemeFilePath}", themeFilePath);
        }
        catch (Exception ex)
        {
            // Log error
            Log.Error(ex, "Failed to load theme data from {ThemeFilePath}.", themeFilePath);
            // Load light theme
            Log.Debug("Loading default light theme as fallback.");
            jsonData = await LoadThemeDataFromAssetsAsync(Constants.LightThemeFilePath);
        }

        // Convert JSON data to AppTheme object
        var appTheme = ConvertJsonToAppTheme(jsonData);
        if (appTheme == null)
        {
            Log.Error("Failed to convert JSON data to AppTheme object.");
            throw new InvalidCastException("App theme data is null or undefined.");
        }

        Log.Debug("AppTheme object created successfully. Theme name: {Name}", appTheme.Name);

        // Apply theme data
        Dispatcher.UIThread.Invoke(() => ApplyAppTheme(appTheme));
        // Set the last theme path
        _lastThemePath = themeFilePath;
        // Log information
        Log.Information("{Name} theme loaded successfully.", appTheme.Name);
    }

    public bool ValidateAppTheme(string? json)
    {
        Log.Debug("Validating app theme JSON data...");

        if (json.IsStringNullOrEmpty())
        {
            Log.Debug("JSON data is null or empty. Validation failed.");
            return false;
        }

        var appTheme = ConvertJsonToAppTheme(json);
        var isValid = appTheme != null && appTheme.Validate();

        Log.Debug("App theme validation result: {IsValid}", isValid);
        return isValid;
    }

    public async Task<List<AppTheme>> GetAllThemesAsync()
    {
        Log.Debug("Getting all available themes...");
        var result = new List<AppTheme>();

        // Get all themes from assets
        var assetsUri = new Uri("avares://CrossPlatformDownloadManager.DesktopApp/Assets/Themes/");
        var assets = assetsUri.GetAllAssets();
        Log.Debug("Found {Count} themes in assets.", assets.Count);

        // Check if there are any themes
        if (assets.Count > 0)
        {
            Log.Debug("Converting assets to AppTheme objects...");

            // Convert assets to AppTheme objects
            var appThemes = new List<AppTheme>();
            foreach (var asset in assets)
            {
                try
                {
                    var json = await asset.OpenTextAssetAsync();
                    var appTheme = ConvertJsonToAppTheme(json);
                    if (appTheme?.Validate() != true)
                        continue;

                    appTheme.IsDefault = true;
                    appTheme.Path = asset;
                    appThemes.Add(appTheme);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to convert asset to AppTheme object.");
                }
            }

            Log.Debug("Converted {Count} assets to AppTheme objects.", appThemes.Count);
            result.AddRange(appThemes);
        }

        var themeFiles = Directory.GetFiles(Constants.ThemesDirectory, "*.json");
        Log.Debug("Found {Count} themes in storage.", themeFiles.Length);

        if (themeFiles.Length > 0)
        {
            Log.Debug("Loading themes from storage...");

            // Convert theme files to AppTheme objects
            var appThemes = new List<AppTheme>();
            foreach (var file in themeFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var appTheme = ConvertJsonToAppTheme(json);
                    if (appTheme?.Validate() != true)
                        continue;

                    appTheme.Path = file;
                    appThemes.Add(appTheme);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to convert theme file to AppTheme object.");
                }
            }

            Log.Debug("Loaded {Count} themes from storage.", appThemes.Count);
            result.AddRange(appThemes);
        }

        Log.Information("loaded {Count} themes successfully.", result.Count);
        return result;
    }

    public async Task<AppTheme?> GetDefaultThemeAsync(bool isDark)
    {
        // Get default theme path and load theme data
        var defaultThemeUri = new Uri(isDark ? Constants.DarkThemeFilePath : Constants.LightThemeFilePath);
        var json = await defaultThemeUri.OpenTextAssetAsync();

        // Convert JSON data to AppTheme object and set IsDefault property to true if successful
        var appTheme = ConvertJsonToAppTheme(json);
        if (appTheme != null)
        {
            appTheme.IsDefault = true;
            appTheme.Path = defaultThemeUri.OriginalString;
        }

        return appTheme;
    }

    #region Helpers

    /// <summary>
    /// Loads theme data from Avalonia assets.
    /// </summary>
    /// <param name="themeFilePath">Theme file path based on Avalonia path system.</param>
    /// <returns>Returns the theme data as JSON string.</returns>
    private static async Task<string?> LoadThemeDataFromAssetsAsync(string themeFilePath)
    {
        Log.Debug("Loading theme data from assets: {ThemeFilePath}", themeFilePath);

        var data = await themeFilePath.OpenTextAssetAsync();
        Log.Debug("Theme data loaded from assets successfully. Length: {Length}", data?.Length ?? 0);

        return data;
    }

    /// <summary>
    /// Loads theme data from storage.
    /// </summary>
    /// <param name="themeFilePath">Theme file path.</param>
    /// <returns>Returns the theme data as JSON string.</returns>
    private static async Task<string?> LoadThemeDataFromStorageAsync(string themeFilePath)
    {
        Log.Debug("Loading theme data from storage: {ThemeFilePath}", themeFilePath);

        if (!File.Exists(themeFilePath))
        {
            Log.Debug("Theme file not found in storage: {ThemeFilePath}", themeFilePath);
            return null;
        }

        var data = await File.ReadAllTextAsync(themeFilePath);
        Log.Debug("Theme data loaded from storage successfully. Length: {Length}", data.Length);

        return data;
    }

    /// <summary>
    /// Converts JSON data to AppTheme object.
    /// </summary>
    /// <param name="jsonData">JSON data as string.</param>
    /// <returns>Returns the AppTheme object.</returns>
    private static AppTheme? ConvertJsonToAppTheme(string? jsonData)
    {
        Log.Debug("Converting JSON data to AppTheme object...");

        if (jsonData.IsStringNullOrEmpty())
        {
            Log.Debug("JSON data is null or empty. Conversion failed.");
            return null;
        }

        // JSON serializer settings for converting IThemeBrush
        var settings = new JsonSerializerSettings
        {
            Converters = [new ThemeBrushJsonConverter()],
            Culture = CultureInfo.InvariantCulture
        };

        var appTheme = jsonData.ConvertFromJson<AppTheme?>(settings);

        if (appTheme != null)
        {
            Log.Debug("JSON data converted to AppTheme successfully. Theme name: {Name}", appTheme.Name);
        }
        else
        {
            Log.Debug("Failed to convert JSON data to AppTheme.");
        }

        return appTheme;
    }

    /// <summary>
    /// Applies the theme data to the application.
    /// </summary>
    /// <param name="appTheme">AppTheme object.</param>
    /// <exception cref="InvalidOperationException">Thrown when the theme data is corrupted or invalid.</exception>
    private static void ApplyAppTheme(AppTheme appTheme)
    {
        Log.Debug("Applying app theme: {Name}", appTheme.Name);

        // Get and validate resources
        var resources = Application.Current?.Resources;
        if (resources == null)
        {
            Log.Warning("Application resources are not available. Cannot apply theme.");
            return;
        }

        // Validate app theme
        if (!appTheme.Validate())
        {
            const string message = "The theme data appears to be corrupted or invalid. Please try again. " +
                                   "If the problem continues, please reach out to the developers for assistance.";

            Log.Error("Theme validation failed: {Message}", message);
            throw new InvalidOperationException(message);
        }

        Log.Debug("Theme validation successful. Applying theme resources...");

        // Load theme data
        resources["MainBackgroundColor"] = appTheme.MainBackgroundColor!.GetBrush();
        resources["SecondaryBackgroundColor"] = appTheme.SecondaryBackgroundColor!.GetBrush();
        resources["AccentColor"] = appTheme.AccentColor!.GetBrush();
        resources["MainTextColor"] = appTheme.MainTextColor!.GetBrush();
        resources["ButtonTextColor"] = appTheme.ButtonTextColor!.GetBrush();
        resources["CategoryHeaderHoverColor"] = appTheme.CategoryHeaderHoverColor!.GetBrush();
        resources["CategoryHeaderSelectedColor"] = appTheme.CategoryHeaderSelectedColor!.GetBrush();
        resources["MenuBackgroundColor"] = appTheme.MenuBackgroundColor!.GetBrush();
        resources["MenuItemHoverColor"] = appTheme.MenuItemHoverColor!.GetBrush();
        resources["IconColor"] = appTheme.IconColor!.GetBrush();
        resources["SelectedProxyColor"] = appTheme.SelectedProxyColor!.GetBrush();
        resources["ToggleCircleColor"] = appTheme.ToggleCircleColor!.GetBrush();
        resources["ToggleColor"] = appTheme.ToggleColor!.GetBrush();
        resources["ToggleCheckedColor"] = appTheme.ToggleCheckedColor!.GetBrush();
        resources["LoadingIndicatorColor"] = appTheme.LoadingIndicatorColor!.GetBrush();
        resources["DialogTextColor"] = appTheme.DialogTextColor!.GetBrush();
        resources["DialogOkBackgroundColor"] = appTheme.DialogOkBackgroundColor!.GetBrush();
        resources["DialogYesBackgroundColor"] = appTheme.DialogYesBackgroundColor!.GetBrush();
        resources["DialogNoBackgroundColor"] = appTheme.DialogNoBackgroundColor!.GetBrush();
        resources["DialogCancelBackgroundColor"] = appTheme.DialogCancelBackgroundColor!.GetBrush();
        resources["ManagerTextColor"] = appTheme.ManagerTextColor!.GetBrush();
        resources["ChunkProgressBackgroundColor"] = appTheme.ChunkProgressBackgroundColor!.GetBrush();
        resources["MainColor"] = appTheme.MainColor!.GetBrush();
        resources["SuccessColor"] = appTheme.SuccessColor!.GetBrush();
        resources["InfoColor"] = appTheme.InfoColor!.GetBrush();
        resources["DangerColor"] = appTheme.DangerColor!.GetBrush();
        resources["WarningColor"] = appTheme.WarningColor!.GetBrush();
        resources["ChunkProgressColor"] = appTheme.ChunkProgressColor!.GetBrush();
        resources["GridRowColor"] = appTheme.GridRowColor!.GetBrush();
        resources["LinkColor"] = appTheme.LinkColor!.GetBrush();

        Log.Debug("All theme resources applied successfully.");

        // Check if the RequestedThemeVariant exists
        if (Application.Current?.RequestedThemeVariant == null)
        {
            Log.Warning("RequestedThemeVariant is null. Cannot apply theme variant.");
            return;
        }

        // Set application theme variant
        Application.Current.RequestedThemeVariant = appTheme.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        Log.Debug("Application theme variant set to: {ThemeVariant}", appTheme.IsDarkTheme ? "Dark" : "Light");
    }

    #endregion
}
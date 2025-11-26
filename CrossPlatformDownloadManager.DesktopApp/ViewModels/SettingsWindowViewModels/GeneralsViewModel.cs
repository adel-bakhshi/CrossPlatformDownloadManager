using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Models;
using CrossPlatformDownloadManager.Utils;
using Emik;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class GeneralsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _startOnSystemStartup;
    private bool _useBrowserExtension;
    private ObservableCollection<ThemeData> _themes = [];
    private ThemeData? _selectedTheme;
    private bool _useManager;
    private bool _alwaysKeepManagerOnTop;
    private ObservableCollection<string> _fonts = [];
    private string? _selectedFont;

    #endregion

    #region Properties

    public bool StartOnSystemStartup
    {
        get => _startOnSystemStartup;
        set => this.RaiseAndSetIfChanged(ref _startOnSystemStartup, value);
    }

    public bool UseBrowserExtension
    {
        get => _useBrowserExtension;
        set => this.RaiseAndSetIfChanged(ref _useBrowserExtension, value);
    }

    public ObservableCollection<ThemeData> Themes
    {
        get => _themes;
        set => this.RaiseAndSetIfChanged(ref _themes, value);
    }

    public ThemeData? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTheme, value);
            this.RaisePropertyChanged(nameof(CanRemoveTheme));
        }
    }

    public bool CanRemoveTheme => SelectedTheme != null && SelectedTheme.ThemePath?.Contains("avares://") == false;

    public bool UseManager
    {
        get => _useManager;
        set => this.RaiseAndSetIfChanged(ref _useManager, value);
    }

    public bool AlwaysKeepManagerOnTop
    {
        get => _alwaysKeepManagerOnTop;
        set => this.RaiseAndSetIfChanged(ref _alwaysKeepManagerOnTop, value);
    }

    public ObservableCollection<string> Fonts
    {
        get => _fonts;
        set => this.RaiseAndSetIfChanged(ref _fonts, value);
    }

    public string? SelectedFont
    {
        get => _selectedFont;
        set => this.RaiseAndSetIfChanged(ref _selectedFont, value);
    }

    #endregion

    #region Commands

    public ICommand AddNewThemeCommand { get; }

    public ICommand RemoveThemeCommand { get; }

    #endregion

    public GeneralsViewModel(IAppService appService) : base(appService)
    {
        LoadViewData();

        AddNewThemeCommand = ReactiveCommand.CreateFromTask(AddNewThemeAsync);
        RemoveThemeCommand = ReactiveCommand.CreateFromTask(RemoveThemeAsync);
    }

    #region Command actions

    private async Task AddNewThemeAsync()
    {
        try
        {
            // Get storage provider
            var storageProvider = App.Desktop?.MainWindow?.StorageProvider;
            if (storageProvider == null)
                throw new InvalidOperationException("Failed to access to storage. Storage provider is null or undefined.");

            // Create file picker options
            var options = new FilePickerOpenOptions
            {
                Title = "Select Theme",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("CDM theme file") { Patterns = ["*.json"] }]
            };

            // Open file picker
            var selectedFiles = await storageProvider.OpenFilePickerAsync(options);
            if (selectedFiles.Count == 0)
                return;

            // Get selected file as stream
            await using var stream = await selectedFiles[0].OpenReadAsync();
            using var reader = new StreamReader(stream);
            // Read file content
            var json = await reader.ReadToEndAsync();
            // Try to convert file to AppTheme object
            if (!AppService.AppThemeService.ValidateAppTheme(json))
            {
                await DialogBoxManager.ShowDangerDialogAsync("Invalid theme", "The selected file is not a valid theme file.", DialogButtons.Ok);
                return;
            }

            // Copy theme file to themes directory
            var themeFileName = $"{Guid.NewGuid().ToString()}.json";
            var themeFilePath = Path.Combine(Constants.ThemesDirectory, themeFileName);
            await File.WriteAllTextAsync(themeFilePath, json);

            // Load themes
            LoadThemes();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add new theme to the application. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task RemoveThemeAsync()
    {
        try
        {
            // Make sure we can remove the selected theme
            if (!CanRemoveTheme)
                return;

            // Remove theme file from storage
            if (File.Exists(SelectedTheme!.ThemePath))
                await Rubbish.MoveAsync(SelectedTheme.ThemePath);

            // Load themes
            LoadThemes();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to remove a theme from the application. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Loads the data that needed for the view.
    /// </summary>
    private void LoadViewData()
    {
        var settings = AppService.SettingsService.Settings;
        StartOnSystemStartup = settings.StartOnSystemStartup;
        UseBrowserExtension = settings.UseBrowserExtension;
        UseManager = settings.UseManager;
        AlwaysKeepManagerOnTop = settings.AlwaysKeepManagerOnTop;

        // Load themes
        LoadThemes();

        // Load fonts
        Fonts = Constants.AvailableFonts.ToObservableCollection();
        SelectedFont = Constants.AvailableFonts.Find(f => f.Equals(settings.ApplicationFont)) ?? Fonts.FirstOrDefault();
    }

    /// <summary>
    /// Loads themes.
    /// </summary>
    private void LoadThemes()
    {
        Themes = GetAvailableThemes();
        SelectedTheme = Themes.FirstOrDefault(th => th.ThemePath?.Equals(AppService.SettingsService.Settings.ThemeFilePath) == true) ?? Themes.FirstOrDefault();
    }

    /// <summary>
    /// Gets the available themes for the application.
    /// </summary>
    /// <returns>An observable collection of theme data.</returns>
    private static ObservableCollection<ThemeData> GetAvailableThemes()
    {
        var results = new List<ThemeData>();
        // Get default theme names
        List<string> defaultThemeNames =
        [
            "dark-theme",
            "light-theme",
            "gold-theme",
            "ocean-blue-theme",
            "galactic-purple-theme",
            "forest-green-theme",
            "sunset-orange-theme",
            "dreamy-pink-theme",
            "modern-gray-theme",
            "gruvbox-dark-theme",
            "gruvbox-light-theme"
        ];

        // Convert theme names to ThemeData
        var defaultThemes = defaultThemeNames
            .ConvertAll(theme =>
            {
                var themeData = new ThemeData { ThemePath = "avares://CrossPlatformDownloadManager.DesktopApp/Assets/Themes/" + theme + ".json" };
                themeData.ThemeName = theme switch
                {
                    "dark-theme" => "Dark",
                    "light-theme" => "Light",
                    "gold-theme" => "Gold",
                    "ocean-blue-theme" => "Ocean Blue",
                    "galactic-purple-theme" => "Galactic Purple",
                    "forest-green-theme" => "Forest Green",
                    "sunset-orange-theme" => "Sunset Orange",
                    "dreamy-pink-theme" => "Dreamy Pink",
                    "modern-gray-theme" => "Modern Gray",
                    "gruvbox-dark-theme" => "Gruvbox Dark",
                    "gruvbox-light-theme" => "Gruvbox Light",
                    _ => themeData.ThemeName
                };

                return themeData;
            });

        // Add default themes to results
        results.AddRange(defaultThemes);
        // Get custom themes
        var themeFiles = Directory.GetFiles(Constants.ThemesDirectory, "*.json")
            .Select(path =>
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var jsonToken = JToken.Parse(json);
                    var themeName = jsonToken.SelectToken("themeName")?.ToString();
                    return themeName.IsStringNullOrEmpty() ? null : new ThemeData { ThemeName = themeName, ThemePath = path };
                }
                catch
                {
                    return null;
                }
            })
            .OfType<ThemeData>()
            .ToList();

        // Add custom themes to results
        results.AddRange(themeFiles);
        return results
            .OrderBy(td => td.ThemeName)
            .ToObservableCollection();
    }

    #endregion
}
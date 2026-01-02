using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;
using CrossPlatformDownloadManager.DesktopApp.Views.Settings.Views.Appearance;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings.Views.Appearance;

public class AppearanceViewModel : ViewModelBase
{
    #region Private Fields

    // Backing fields for properties
    private ObservableCollection<ThemeCardView> _darkThemes = [];
    private ObservableCollection<ThemeCardView> _lightThemes = [];
    private ThemeCardView? _selectedDarkTheme;
    private ThemeCardView? _selectedLightTheme;
    private ObservableCollection<string> _fonts = [];
    private string? _selectedFont;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the dark theme views.
    /// </summary>
    public ObservableCollection<ThemeCardView> DarkThemes
    {
        get => _darkThemes;
        set => this.RaiseAndSetIfChanged(ref _darkThemes, value);
    }

    /// <summary>
    /// Gets or sets the light theme views.
    /// </summary>
    public ObservableCollection<ThemeCardView> LightThemes
    {
        get => _lightThemes;
        set => this.RaiseAndSetIfChanged(ref _lightThemes, value);
    }

    /// <summary>
    /// Gets or sets the selected dark theme.
    /// </summary>
    public ThemeCardView? SelectedDarkTheme
    {
        get => _selectedDarkTheme;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedDarkTheme, value);

            // If a dark theme is selected, unselect the light theme
            if (SelectedDarkTheme != null)
                SelectedLightTheme = null;
        }
    }

    /// <summary>
    /// Gets or sets the selected light theme.
    /// </summary>
    public ThemeCardView? SelectedLightTheme
    {
        get => _selectedLightTheme;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLightTheme, value);

            // If a light theme is selected, unselect the dark theme
            if (SelectedLightTheme != null)
                SelectedDarkTheme = null;
        }
    }

    /// <summary>
    /// Gets or sets the fonts.
    /// </summary>
    public ObservableCollection<string> Fonts
    {
        get => _fonts;
        set => this.RaiseAndSetIfChanged(ref _fonts, value);
    }

    /// <summary>
    /// Gets or sets the selected font.
    /// </summary>
    public string? SelectedFont
    {
        get => _selectedFont;
        set => this.RaiseAndSetIfChanged(ref _selectedFont, value);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Gets the create theme command.
    /// </summary>
    public ICommand CreateThemeCommand { get; }

    /// <summary>
    /// Gets the add new theme command.
    /// </summary>
    public ICommand AddNewThemeCommand { get; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="AppearanceViewModel"/> class.
    /// </summary>
    /// <param name="appService">The app service.</param>
    public AppearanceViewModel(IAppService appService) : base(appService)
    {
        LoadViewData();

        // Initialize commands
        CreateThemeCommand = ReactiveCommand.CreateFromTask(CreateThemeAsync);
        AddNewThemeCommand = ReactiveCommand.CreateFromTask(AddNewThemeAsync);
    }

    #region Command Actions

    /// <summary>
    /// Creates a new theme.
    /// </summary>
    private static async Task CreateThemeAsync()
    {
        try
        {
            // Open the guide link for creating themes
            var processStartInfo = new ProcessStartInfo
            {
                FileName = Constants.CreateThemeGuideLink,
                UseShellExecute = true
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to create a new theme for the application. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Adds a new theme to the application.
    /// </summary>
    /// <exception cref="InvalidOperationException">Failed to access to storage. Storage provider is null or undefined.</exception>
    private async Task AddNewThemeAsync()
    {
        try
        {
            Log.Information("Starting theme import");

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
            {
                Log.Debug("Theme import cancelled by user");
                return;
            }

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

            Log.Debug("Valid theme file found. Importing...");

            // Copy theme file to themes directory
            var themeFileName = $"{Guid.NewGuid().ToString()}.json";
            var themeFilePath = Path.Combine(Constants.ThemesDirectory, themeFileName);
            await File.WriteAllTextAsync(themeFilePath, json);

            Log.Debug("Theme file copied to themes directory.");

            // Load themes
            await LoadThemesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to add new theme to the application. Error message: {ErrorMessage}", ex.Message);
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
        // Load themes
        _ = LoadThemesAsync();

        // Load fonts
        LoadFonts();
    }

    /// <summary>
    /// Loads the themes.
    /// </summary>
    private async Task LoadThemesAsync()
    {
        try
        {
            // Create theme card view models
            var viewModels = await CreateThemeCardViewModelsAsync();

            // Remove event handlers from old theme views
            RemoveEventHandlers();

            // Generate theme views
            DarkThemes = GenerateThemeViews(viewModels, isDark: true);
            LightThemes = GenerateThemeViews(viewModels, isDark: false);

            // Add event handlers to new theme views
            AddEventHandlers();

            // Select the default theme
            SelectDefaultTheme();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while loading themes. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Adds event handlers to the theme views.
    /// </summary>
    private void AddEventHandlers()
    {
        foreach (var view in DarkThemes)
            view.ThemeRemoved += ThemeCardViewOnThemeRemoved;

        foreach (var view in LightThemes)
            view.ThemeRemoved += ThemeCardViewOnThemeRemoved;
    }

    /// <summary>
    /// Removes event handlers from the theme views.
    /// </summary>
    private void RemoveEventHandlers()
    {
        foreach (var view in DarkThemes)
            view.ThemeRemoved -= ThemeCardViewOnThemeRemoved;

        foreach (var view in LightThemes)
            view.ThemeRemoved -= ThemeCardViewOnThemeRemoved;
    }

    /// <summary>
    /// Selects the default theme.
    /// </summary>
    private void SelectDefaultTheme()
    {
        // Get the selected theme from settings
        var selectedTheme = AppService.SettingsService.Settings.ThemeFilePath;

        // Try to find the selected theme in dark themes
        var selectedDarkTheme = DarkThemes
            .FirstOrDefault(v => v.DataContext is ThemeCardViewModel viewModel && viewModel.AppTheme!.Path?.Equals(selectedTheme) == true);

        // If the selected theme is found, select it
        if (selectedDarkTheme != null)
        {
            SelectedDarkTheme = selectedDarkTheme;
            return;
        }

        // Otherwise, Try to find the selected theme in light themes
        var selectedLightTheme = LightThemes
            .FirstOrDefault(v => v.DataContext is ThemeCardViewModel viewModel && viewModel.AppTheme!.Path?.Equals(selectedTheme) == true);

        // If the selected theme is found, select it
        if (selectedLightTheme != null)
            SelectedLightTheme = selectedLightTheme;
    }

    /// <summary>
    /// Creates the theme card view models.
    /// </summary>
    /// <returns>The theme card view models.</returns>
    /// <exception cref="InvalidOperationException">Default themes can't be null.</exception>
    private async Task<List<ThemeCardViewModel>> CreateThemeCardViewModelsAsync()
    {
        // Get default themes
        var defaultDarkTheme = await AppService.AppThemeService.GetDefaultThemeAsync(isDark: true);
        var defaultLightTheme = await AppService.AppThemeService.GetDefaultThemeAsync(isDark: false);

        // Check if default themes are null
        if (defaultDarkTheme == null || defaultLightTheme == null)
            throw new InvalidOperationException("Default themes can't be null");

        // Get all themes
        var appThemes = await AppService.AppThemeService.GetAllThemesAsync();

        // Create theme card view models
        return appThemes
            .Select(t => new ThemeCardViewModel(AppService, t, t.IsDarkTheme ? defaultDarkTheme : defaultLightTheme))
            .ToList();
    }

    /// <summary>
    /// Generates the theme views for the given themes.
    /// </summary>
    /// <param name="viewModels">The theme card view models.</param>
    /// <param name="isDark">Whether the themes are dark themes or not.</param>
    /// <returns>The theme views.</returns>
    private static ObservableCollection<ThemeCardView> GenerateThemeViews(List<ThemeCardViewModel> viewModels, bool isDark)
    {
        // Generate theme views for the given theme and order them by name
        var themeCardViewModels = viewModels
            .Where(vm => vm.AppTheme!.IsDarkTheme == isDark)
            .OrderBy(vm => vm.AppTheme!.Name)
            .ToList();

        // Find CDM theme based on the given theme
        var cdmTheme = themeCardViewModels.Find(vm => vm.AppTheme!.Name.Equals(isDark ? "CDM Dark" : "CDM Light"));
        if (cdmTheme == null)
            throw new InvalidOperationException("CDM theme can't be null");

        // Move CDM theme to the first position
        themeCardViewModels.Remove(cdmTheme);
        themeCardViewModels.Insert(0, cdmTheme);

        return themeCardViewModels
            .Select(vm => new ThemeCardView { DataContext = vm })
            .ToObservableCollection();
    }

    /// <summary>
    /// Loads the fonts.
    /// </summary>
    private void LoadFonts()
    {
        Fonts = Constants.AvailableFonts.ToObservableCollection();
        SelectedFont = Constants.AvailableFonts.Find(f => f.Equals(AppService.SettingsService.Settings.ApplicationFont)) ?? Fonts.FirstOrDefault();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles the ThemeRemoved event of the theme views.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void ThemeCardViewOnThemeRemoved(object? sender, AppTheme? e)
    {
        try
        {
            // Check if theme is null
            if (e == null)
                return;

            // Check if theme file exists
            if (!File.Exists(e.Path))
            {
                await DialogBoxManager.ShowDangerDialogAsync("Invalid theme", "The selected theme does not exist.", DialogButtons.Ok);
                return;
            }

            // Show warning dialog
            var result = await DialogBoxManager.ShowWarningDialogAsync("Remove theme",
                $"Are you sure you want to remove the theme \"{e.Name}\"?",
                DialogButtons.YesNo);

            if (result != DialogResult.Yes)
                return;

            // Delete theme file
            File.Delete(e.Path);
            Log.Debug("Removed theme \"{ThemeName}\" from the application.", e.Name);

            // Load themes
            await LoadThemesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to remove a theme from the application. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #endregion
}
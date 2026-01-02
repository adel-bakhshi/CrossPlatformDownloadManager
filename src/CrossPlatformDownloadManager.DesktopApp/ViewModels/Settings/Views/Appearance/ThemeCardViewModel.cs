using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Media;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings.Views.Appearance;

public class ThemeCardViewModel : ViewModelBase
{
    #region Private Fields

    // Backing fields for properties
    private AppTheme? _appTheme;
    private IBrush? _mainBackgroundColor;
    private IBrush? _mainTextColor;
    private IBrush? _accentColor;
    private IBrush? _successColor;
    private IBrush? _dangerColor;
    private ObservableCollection<IBrush> _colorPaletteBrushes = [];
    private bool _isDefaultTheme;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the app theme containing the theme data.
    /// </summary>
    public AppTheme? AppTheme
    {
        get => _appTheme;
        set => this.RaiseAndSetIfChanged(ref _appTheme, value);
    }

    /// <summary>
    /// Gets or sets the main background color of the app theme.
    /// </summary>
    public IBrush? MainBackgroundColor
    {
        get => _mainBackgroundColor;
        set => this.RaiseAndSetIfChanged(ref _mainBackgroundColor, value);
    }

    /// <summary>
    /// Gets or sets the main text color of the app theme.
    /// </summary>
    public IBrush? MainTextColor
    {
        get => _mainTextColor;
        set => this.RaiseAndSetIfChanged(ref _mainTextColor, value);
    }

    /// <summary>
    /// Gets or sets the accent color of the app theme.
    /// </summary>
    public IBrush? AccentColor
    {
        get => _accentColor;
        set => this.RaiseAndSetIfChanged(ref _accentColor, value);
    }

    /// <summary>
    /// Gets or sets the success color of the app theme.
    /// </summary>
    public IBrush? SuccessColor
    {
        get => _successColor;
        set => this.RaiseAndSetIfChanged(ref _successColor, value);
    }

    /// <summary>
    /// Gets or sets the danger color of the app theme.
    /// </summary>
    public IBrush? DangerColor
    {
        get => _dangerColor;
        set => this.RaiseAndSetIfChanged(ref _dangerColor, value);
    }

    /// <summary>
    /// Gets or sets the color palette brushes containing the main colors of the app theme.
    /// </summary>
    public ObservableCollection<IBrush> ColorPaletteBrushes
    {
        get => _colorPaletteBrushes;
        set => this.RaiseAndSetIfChanged(ref _colorPaletteBrushes, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the app theme is the default theme.
    /// </summary>
    public bool IsDefaultTheme
    {
        get => _isDefaultTheme;
        set => this.RaiseAndSetIfChanged(ref _isDefaultTheme, value);
    }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeCardViewModel"/> class.
    /// </summary>
    /// <param name="appService">The app service.</param>
    /// <param name="appTheme">The app theme.</param>
    /// <param name="defaultTheme">The default theme.</param>
    public ThemeCardViewModel(IAppService appService, AppTheme? appTheme, AppTheme? defaultTheme) : base(appService)
    {
        ArgumentNullException.ThrowIfNull(appTheme);
        ArgumentNullException.ThrowIfNull(defaultTheme);

        AppTheme = appTheme;
        IsDefaultTheme = AppTheme.IsDefault;

        LoadRequiredColors(defaultTheme);

        LoadColorPaletteBrushes(defaultTheme);
    }

    /// <summary>
    /// Loads the required colors from the app theme.
    /// If a color is null or not valid in the app theme, the default theme color is used instead.
    /// </summary>
    /// <param name="defaultTheme">The default theme.</param>
    /// <exception cref="InvalidOperationException">An error occurred while loading required colors.</exception>
    private void LoadRequiredColors(AppTheme defaultTheme)
    {
        try
        {
            Log.Debug("Loading required colors...");

            MainBackgroundColor = GetBrush(AppTheme!.MainBackgroundColor, defaultTheme.MainBackgroundColor)
                                  ?? throw new InvalidOperationException("Background color can't be null");

            MainTextColor = GetBrush(AppTheme!.MainTextColor, defaultTheme.MainTextColor)
                            ?? throw new InvalidOperationException("Text color can't be null");

            AccentColor = GetBrush(AppTheme!.AccentColor, defaultTheme.AccentColor)
                          ?? throw new InvalidOperationException("Accent color can't be null");

            SuccessColor = GetBrush(AppTheme!.SuccessColor, defaultTheme.SuccessColor)
                           ?? throw new InvalidOperationException("Success color can't be null");

            DangerColor = GetBrush(AppTheme!.DangerColor, defaultTheme.DangerColor)
                          ?? throw new InvalidOperationException("Danger color can't be null");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading required colors. Error message: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Loads the color palette brushes from the app theme.
    /// Null colors will be replaced with the default theme color.
    /// </summary>
    /// <param name="defaultTheme">The default theme.</param>
    private void LoadColorPaletteBrushes(AppTheme defaultTheme)
    {
        try
        {
            var brushes = new List<IBrush>();

            Log.Debug("Loading color palette brushes...");

            AddBrushToPalette(brushes, AppTheme!.MainBackgroundColor, defaultTheme.MainBackgroundColor);
            AddBrushToPalette(brushes, AppTheme!.SecondaryBackgroundColor, defaultTheme.SecondaryBackgroundColor);
            AddBrushToPalette(brushes, AppTheme!.AccentColor, defaultTheme.AccentColor);
            AddBrushToPalette(brushes, AppTheme!.MainTextColor, defaultTheme.MainTextColor);
            AddBrushToPalette(brushes, AppTheme!.IconColor, defaultTheme.IconColor);
            AddBrushToPalette(brushes, AppTheme!.SuccessColor, defaultTheme.SuccessColor);
            AddBrushToPalette(brushes, AppTheme!.InfoColor, defaultTheme.InfoColor);
            AddBrushToPalette(brushes, AppTheme!.DangerColor, defaultTheme.DangerColor);
            AddBrushToPalette(brushes, AppTheme!.WarningColor, defaultTheme.WarningColor);
            AddBrushToPalette(brushes, AppTheme!.MainColor, defaultTheme.MainColor);

            ColorPaletteBrushes = brushes.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while loading color palette brushes. Error message: {ErrorMessage}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Adds a brush to the color palette brushes.
    /// </summary>
    /// <param name="brushes">The color palette brushes to add to.</param>
    /// <param name="themeBrush">The theme brush to add.</param>
    /// <param name="defaultThemeBrush">The default theme brush to use if the theme brush is null.</param>
    /// <exception cref="InvalidOperationException">Brush can't be null.</exception>
    private static void AddBrushToPalette(List<IBrush> brushes, IThemeBrush? themeBrush, IThemeBrush? defaultThemeBrush)
    {
        var brush = GetBrush(themeBrush, defaultThemeBrush);
        if (brush == null)
            throw new InvalidOperationException("Brush can't be null");

        brushes.Add(brush);
    }

    /// <summary>
    /// Gets a brush from a theme brush.
    /// </summary>
    /// <param name="themeBrush">The theme brush.</param>
    /// <param name="defaultThemeBrush">The default theme brush.</param>
    /// <returns>The brush.</returns>
    private static IBrush? GetBrush(IThemeBrush? themeBrush, IThemeBrush? defaultThemeBrush)
    {
        if (themeBrush?.Validate() != true)
            return defaultThemeBrush == null ? null : GetBrush(defaultThemeBrush, null);

        var brush = themeBrush.GetBrush();
        return themeBrush.BrushMode switch
        {
            ThemeBrushMode.Solid => GetSolidColorBrush((Color)brush),
            ThemeBrushMode.Gradient => (LinearGradientBrush)brush,
            _ => null
        };
    }

    /// <summary>
    /// Gets a solid color brush from a color.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns>The solid color brush.</returns>
    private static SolidColorBrush GetSolidColorBrush(Color color)
    {
        return new SolidColorBrush(new Color(255, color.R, color.G, color.B))
        {
            Opacity = color.A / 255.0
        };
    }
}
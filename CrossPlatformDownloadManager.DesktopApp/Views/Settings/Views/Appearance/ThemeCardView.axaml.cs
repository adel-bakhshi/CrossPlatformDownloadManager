using System;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.Settings.Views.Appearance;

namespace CrossPlatformDownloadManager.DesktopApp.Views.Settings.Views.Appearance;

public partial class ThemeCardView : MyUserControlBase<ThemeCardViewModel>
{
    #region Events

    /// <summary>
    /// Event that is raised when the user clicks the remove theme button.
    /// </summary>
    public event EventHandler<AppTheme?>? ThemeRemoved;

    #endregion

    public ThemeCardView()
    {
        InitializeComponent();
    }

    private void RemoveThemeButtonOnClick(object? sender, RoutedEventArgs e)
    {
        ThemeRemoved?.Invoke(this, ViewModel?.AppTheme);
    }
}
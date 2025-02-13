using CrossPlatformDownloadManager.Utils;
using System;
using System.IO;
using System.Runtime.Versioning;
using WindowsShortcutFactory;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.StartupManager;

[SupportedOSPlatform("windows")]
public class WindowsStartupManager : IStartupManager
{
    #region Private Fields

    private readonly string _appName;

    #endregion

    public WindowsStartupManager(string appName)
    {
        _appName = appName;
    }

    public bool IsRegistered()
    {
#if DEBUG

        if (!ValidateShortcutInDebugMode())
            return true;

#endif

        var shortcutPath = GetShortcutPath();
        return File.Exists(shortcutPath);
    }

    public void Register()
    {
#if DEBUG

        if (!ValidateShortcutInDebugMode())
            return;

#endif

        var shortcutPath = GetShortcutPath();
        var exePath = GetExePath();

        using var shortcut = new WindowsShortcut();
        shortcut.Path = exePath;
        shortcut.Description = "Cross platform Download Manager (CDM)";

        shortcut.Save(shortcutPath);
    }

    public void Delete()
    {
#if DEBUG

        if (!ValidateShortcutInDebugMode())
            return;

#endif

        var shortcutPath = GetShortcutPath();
        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
    }

    #region Helpers

    private string GetShortcutPath()
    {
        var startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        return Path.Combine(startUpFolderPath, $"{_appName}.lnk");
    }

    private string GetExePath()
    {
        return Path.Combine(Constants.MainDirectory, $"{_appName}.exe");
    }

    private bool ValidateShortcutInDebugMode()
    {
        // When in debug mode, if the shortcut path of the application does not match the current path,
        // we assume that the shortcut was created for the main application and should not be modified.
        var shortcutPath = GetShortcutPath();
        var isShortcutExists = File.Exists(shortcutPath);

        if (!isShortcutExists)
            return true;

        var shortcut = WindowsShortcut.Load(shortcutPath);
        if (shortcut.Path.IsNullOrEmpty())
            return true;

        // If the shortcut path and the executable path are not the same, it means the shortcut has already been registered for the released application,
        // and there is no need to register it again.
        var exePath = GetExePath();
        return shortcut.Path!.Equals(exePath);
    }

    #endregion
}
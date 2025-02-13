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
        var shortcutPath = GetShortcutPath();
        return File.Exists(shortcutPath);
    }

    public void Register()
    {
        var shortcutPath = GetShortcutPath();
        var exePath = GetExePath();

        using var shortcut = new WindowsShortcut();
        shortcut.Path = exePath;
        shortcut.Description = "Cross platform Download Manager (CDM)";

        shortcut.Save(shortcutPath);
    }

    public void Delete()
    {
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

    #endregion
}
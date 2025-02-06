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
        var startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutPath = Path.Combine(startUpFolderPath, $"{_appName}.lnk");

        return File.Exists(shortcutPath);
    }

    public void Register()
    {
        var startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutPath = Path.Combine(startUpFolderPath, $"{_appName}.lnk");
        var exePath = Path.Combine(Constants.MainDirectory, $"{_appName}.exe");

        using var shortcut = new WindowsShortcut();
        shortcut.Path = exePath;
        shortcut.Description = "Cross platform Download Manager (CDM)";

        shortcut.Save(shortcutPath);
    }

    public void Delete()
    {
        var startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutPath = Path.Combine(startUpFolderPath, $"{_appName}.lnk");

        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
    }
}
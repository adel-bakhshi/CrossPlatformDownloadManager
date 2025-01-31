using System;
using System.IO;
using System.Runtime.Versioning;

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
        string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startUpFolderPath, $"{_appName}.lnk");

        return File.Exists(shortcutPath);
    }

    public void Register()
    {
        string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startUpFolderPath, $"{_appName}.lnk");
        string exePath = Path.Combine(Environment.CurrentDirectory, $"{_appName}.exe");

        var shell = new IWshRuntimeLibrary.WshShell();
        var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.Description = "Launch Cross platform Download Manager (CDM)";
        shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
        shortcut.TargetPath = exePath;
        shortcut.Save();
    }

    public void Delete()
    {
        string startUpFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        string shortcutPath = Path.Combine(startUpFolderPath, $"{_appName}.lnk");

        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
    }
}
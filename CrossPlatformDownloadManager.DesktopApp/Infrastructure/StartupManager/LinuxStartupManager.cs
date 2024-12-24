using System;
using System.IO;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.StartupManager;

[SupportedOSPlatform("linux")]
public class LinuxStartupManager : IStartupManager
{
    private readonly string _appName;
    private readonly string _appExec;

    public LinuxStartupManager(string appName, string appExec)
    {
        _appName = appName;
        _appExec = appExec;
    }

    public bool IsRegistered()
    {
        var desktopEntryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart", $"{_appName}.desktop");
        return File.Exists(desktopEntryPath);
    }

    public void Register()
    {
        var desktopEntryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart", $"{_appName}.desktop");
        var desktopEntryContent = $"""
                                   [Desktop Entry]
                                   Type=Application
                                   Name={_appName}
                                   Comment=Cross platform Download Manager (CDM)
                                   Exec={_appExec}
                                   Hidden=false
                                   NoDisplay=false
                                   X-GNOME-Autostart-enabled=true
                                   """;

        File.WriteAllText(desktopEntryPath, desktopEntryContent);
    }

    public void Delete()
    {
        var desktopEntryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart", $"{_appName}.desktop");
        if (File.Exists(desktopEntryPath))
            File.Delete(desktopEntryPath);
    }
}
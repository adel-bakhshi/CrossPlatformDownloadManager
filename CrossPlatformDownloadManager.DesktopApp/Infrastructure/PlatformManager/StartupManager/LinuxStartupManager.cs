using System;
using System.IO;
using System.Runtime.Versioning;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.StartupManager;

[SupportedOSPlatform("linux")]
public class LinuxStartupManager : IStartupManager
{
    #region Private Fields

    private readonly string _appName;
    private readonly string _appExec;

    #endregion

    public LinuxStartupManager(string appName, string appExec)
    {
        _appName = appName;
        _appExec = appExec;
    }

    public bool IsRegistered()
    {
        var desktopEntryPath = GetDesktopEntryPath();
        return File.Exists(desktopEntryPath);
    }

    public void Register()
    {
        try
        {
            var desktopEntryPath = GetDesktopEntryPath();
            var desktopEntryContent = $"""
                                       [Desktop Entry]
                                       Type=Application
                                       Name={_appName}
                                       Comment=Cross platform Download Manager (CDM)
                                       Exec="{_appExec}"
                                       Hidden=false
                                       NoDisplay=false
                                       X-GNOME-Autostart-enabled=true
                                       Terminal=false
                                       """;

            File.WriteAllText(desktopEntryPath, desktopEntryContent);
            Log.Information($"Autostart entry created: {desktopEntryPath}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create autostart entry. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public void Delete()
    {
        try
        {
            var desktopEntryPath = GetDesktopEntryPath();
            if (!File.Exists(desktopEntryPath))
                return;

            File.Delete(desktopEntryPath);
            Log.Information($"Autostart entry deleted: {desktopEntryPath}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete autostart entry. Error message: {ErrorMessage}", ex.Message);
        }
    }

    #region Helpers

    private string GetDesktopEntryPath()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config",
            "autostart");
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return Path.Combine(directory, $"{_appName}.desktop");
    }

    #endregion
}
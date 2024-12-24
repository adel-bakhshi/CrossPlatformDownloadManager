using IWshRuntimeLibrary;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.StartupManager;

[SupportedOSPlatform("windows")]
public class WindowsStartupManager : IStartupManager
{
    private readonly string _appName;
    private readonly bool _forAllUsers;

    public WindowsStartupManager(string appName, bool forAllUsers)
    {
        _appName = appName;
        _forAllUsers = forAllUsers;
    }

    public bool IsRegistered()
    {
        var key = _forAllUsers
            ? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run")
            : Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");

        return key?.GetValue(_appName) != null;
    }

    public void Register()
    {
        var key = _forAllUsers
            ? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
            : Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

        if (key == null)
        {
            Console.WriteLine("Could not open registry key. Admin rights may be required.");
            return; // Or throw an exception
        }

        var executablePath = Path.Combine(Environment.CurrentDirectory, $"{_appName}.exe");
        if (!System.IO.File.Exists(executablePath))
            throw new FileNotFoundException("The executable file was not found.");

        key.SetValue(_appName, executablePath);

        if (_forAllUsers)
            return;

        // Create a shortcut only for the current user
        var startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutPath = Path.Combine(startupFolderPath, $"{_appName}.lnk");
        if (System.IO.File.Exists(shortcutPath))
            return;

        WshShell shell = new();
        var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = executablePath;
        shortcut.WorkingDirectory = Environment.CurrentDirectory;
        shortcut.Description = $"{_appName}";
        shortcut.Save();
    }

    public void Delete()
    {
        var key = _forAllUsers
            ? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
            : Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

        key?.DeleteValue(_appName, false);

        if (_forAllUsers)
            return;

        // Remove the shortcut only from the current user's startup folder
        var startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var shortcutPath = Path.Combine(startupFolderPath, $"{_appName}.lnk");
        if (System.IO.File.Exists(shortcutPath))
            System.IO.File.Delete(shortcutPath);
    }
}
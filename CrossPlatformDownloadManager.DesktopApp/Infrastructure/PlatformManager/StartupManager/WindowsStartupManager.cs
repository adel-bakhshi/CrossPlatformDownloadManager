using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.StartupManager;

[SupportedOSPlatform("windows")]
public class WindowsStartupManager : IStartupManager
{
    #region Private Fields

    private readonly string _appName;
    private readonly bool _forAllUsers;

    #endregion

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
        if (!File.Exists(executablePath))
            throw new FileNotFoundException("The executable file was not found.");

        key.SetValue(_appName, executablePath);
    }

    public void Delete()
    {
        var key = _forAllUsers
            ? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true)
            : Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

        key?.DeleteValue(_appName, false);
    }
}
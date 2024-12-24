using System;
using System.IO;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.StartupManager;

public static class RegisterStartupManager
{
    private const string AppName = "CrossPlatformDownloadManager.DesktopApp";

    public static bool IsRegistered(bool forAllUsers = false)
    {
        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            startupManager = new WindowsStartupManager(AppName, forAllUsers);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, $"{AppName}.app");
            startupManager = new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Environment.CurrentDirectory, $"{AppName}");
            startupManager = new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return false;
        }

        return startupManager.IsRegistered();
    }

    public static void Register(bool forAllUsers = false)
    {
        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            startupManager = new WindowsStartupManager(AppName, forAllUsers);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, $"{AppName}.app");
            startupManager = new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Environment.CurrentDirectory, $"{AppName}");
            startupManager = new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        startupManager.Register();
    }

    public static void Delete(bool forAllUsers = false)
    {
        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            startupManager = new WindowsStartupManager(AppName, forAllUsers);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, $"{AppName}.app");
            startupManager = new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Environment.CurrentDirectory, $"{AppName}");
            startupManager = new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        startupManager.Delete();
    }
}
using System;
using System.IO;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.StartupManager;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;

public static class PlatformSpecificManager
{
    #region Private Fields

    private const string AppName = "CrossPlatformDownloadManager.DesktopApp";

    private static IStartupManager? _startupManager;
    private static IFileExplorerManager? _fileExplorerManager;
    private static IPowerManager? _powerManager;

    #endregion

    public static bool IsStartupRegistered()
    {
        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            startupManager = _startupManager is WindowsStartupManager ? _startupManager : new WindowsStartupManager(AppName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, $"{AppName}.app");
            startupManager = _startupManager is MacStartupManager ? _startupManager : new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Environment.CurrentDirectory, $"{AppName}");
            startupManager = _startupManager is LinuxStartupManager ? _startupManager : new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return false;
        }

        if (_startupManager == null || _startupManager.GetType() != startupManager.GetType())
            _startupManager = startupManager;

        return startupManager.IsRegistered();
    }

    public static void RegisterStartup()
    {
        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            startupManager = _startupManager is WindowsStartupManager ? _startupManager : new WindowsStartupManager(AppName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, $"{AppName}.app");
            startupManager = _startupManager is MacStartupManager ? _startupManager : new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Environment.CurrentDirectory, AppName);
            startupManager = _startupManager is LinuxStartupManager ? _startupManager : new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_startupManager == null || _startupManager.GetType() != startupManager.GetType())
            _startupManager = startupManager;

        startupManager.Register();
    }

    public static void DeleteStartup()
    {
        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            startupManager = _startupManager is WindowsStartupManager ? _startupManager : new WindowsStartupManager(AppName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Environment.CurrentDirectory, $"{AppName}.app");
            startupManager = _startupManager is MacStartupManager ? _startupManager : new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Environment.CurrentDirectory, $"{AppName}");
            startupManager = _startupManager is LinuxStartupManager ? _startupManager : new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_startupManager == null || _startupManager.GetType() != startupManager.GetType())
            _startupManager = startupManager;

        startupManager.Delete();
    }

    public static void OpenFolder(string folderPath)
    {
        IFileExplorerManager fileExplorerManager;
        if (OperatingSystem.IsWindows())
        {
            fileExplorerManager = _fileExplorerManager is WindowsFileExplorerManager ? _fileExplorerManager : new WindowsFileExplorerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            fileExplorerManager = _fileExplorerManager is MacFileExplorerManager ? _fileExplorerManager : new MacFileExplorerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            fileExplorerManager = _fileExplorerManager is LinuxFileExplorerManager ? _fileExplorerManager : new LinuxFileExplorerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_fileExplorerManager == null || _fileExplorerManager.GetType() != fileExplorerManager.GetType())
            _fileExplorerManager = fileExplorerManager;

        fileExplorerManager.OpenFolder(folderPath);
    }

    public static void OpenContainingFolderAndSelectFile(string filePath)
    {
        IFileExplorerManager fileExplorerManager;
        if (OperatingSystem.IsWindows())
        {
            fileExplorerManager = _fileExplorerManager is WindowsFileExplorerManager ? _fileExplorerManager : new WindowsFileExplorerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            fileExplorerManager = _fileExplorerManager is MacFileExplorerManager ? _fileExplorerManager : new MacFileExplorerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            fileExplorerManager = _fileExplorerManager is LinuxFileExplorerManager ? _fileExplorerManager : new LinuxFileExplorerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_fileExplorerManager == null || _fileExplorerManager.GetType() != fileExplorerManager.GetType())
            _fileExplorerManager = fileExplorerManager;

        fileExplorerManager.OpenContainingFolderAndSelectFile(filePath);
    }

    public static void OpenFile(string filePath)
    {
        IFileExplorerManager fileExplorerManager;
        if (OperatingSystem.IsWindows())
        {
            fileExplorerManager = _fileExplorerManager is WindowsFileExplorerManager ? _fileExplorerManager : new WindowsFileExplorerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            fileExplorerManager = _fileExplorerManager is MacFileExplorerManager ? _fileExplorerManager : new MacFileExplorerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            fileExplorerManager = _fileExplorerManager is LinuxFileExplorerManager ? _fileExplorerManager : new LinuxFileExplorerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_fileExplorerManager == null || _fileExplorerManager.GetType() != fileExplorerManager.GetType())
            _fileExplorerManager = fileExplorerManager;

        fileExplorerManager.OpenFile(filePath);
    }

    public static void Shutdown()
    {
        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        powerManager.Shutdown();
    }

    public static void Sleep()
    {
        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        powerManager.Sleep();
    }

    public static void Hibernate()
    {
        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return;
        }

        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        powerManager.Hibernate();
    }

    public static bool IsHibernateEnabled()
    {
        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Error("Unsupported operating system.");
            return false;
        }

        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        return powerManager.IsHibernateEnabled();
    }
}
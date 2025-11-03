using System;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.StartupManager;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;

using System.IO;

/// <summary>
/// A static class that manages platform-specific operations for the application.
/// This class provides methods for handling startup registration, file operations, and power management.
/// </summary>
public static class PlatformSpecificManager
{
    #region Private Fields

    /// <summary>
    /// The name of the application used for startup registration.
    /// </summary>
    private const string AppName = "CrossPlatformDownloadManager.DesktopApp";

    /// <summary>
    /// Manager for handling startup operations on different operating systems.
    /// </summary>
    private static IStartupManager? _startupManager;

    /// <summary>
    /// Manager for handling file explorer operations on different operating systems.
    /// </summary>
    private static IFileExplorerManager? _fileExplorerManager;

    /// <summary>
    /// Manager for handling power management operations on different operating systems.
    /// </summary>
    private static IPowerManager? _powerManager;

    #endregion

    /// <summary>
    /// Checks if the application is registered as a startup item.
    /// </summary>
    /// <returns>True if the application is registered as a startup item, otherwise false.</returns>
    public static bool IsStartupRegistered()
    {
        Log.Debug("Checking if application is registered as a startup item...");

        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsStartupManager
            startupManager = _startupManager is WindowsStartupManager ? _startupManager : new WindowsStartupManager(AppName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            var appPath = Path.Combine(Constants.MainDirectory, $"{AppName}.app");
            // For macOS, create or use existing MacStartupManager with app path
            startupManager = _startupManager is MacStartupManager ? _startupManager : new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            var appExec = Path.Combine(Constants.MainDirectory, AppName);
            // For Linux, create or use existing LinuxStartupManager with executable path
            startupManager = _startupManager is LinuxStartupManager ? _startupManager : new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return false;
        }

        if (_startupManager == null || _startupManager.GetType() != startupManager.GetType())
            // Update the startup manager if needed
            _startupManager = startupManager;

        return startupManager.IsRegistered();
    }

    /// <summary>
    /// Registers the application as a startup item.
    /// </summary>
    public static void RegisterStartup()
    {
        Log.Debug("Registering application as a startup item...");

        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsStartupManager
            startupManager = _startupManager is WindowsStartupManager ? _startupManager : new WindowsStartupManager(AppName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacStartupManager with app path
            var appPath = Path.Combine(Constants.MainDirectory, $"{AppName}.app");
            startupManager = _startupManager is MacStartupManager ? _startupManager : new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxStartupManager with executable path
            var appExec = Path.Combine(Constants.MainDirectory, AppName);
            startupManager = _startupManager is LinuxStartupManager ? _startupManager : new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the startup manager if needed
        if (_startupManager == null || _startupManager.GetType() != startupManager.GetType())
            _startupManager = startupManager;

        startupManager.Register();
    }

    /// <summary>
    /// Deletes the application's startup configuration.
    /// </summary>
    public static void DeleteStartup()
    {
        Log.Debug("Deleting application startup configuration...");

        IStartupManager startupManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsStartupManager
            startupManager = _startupManager is WindowsStartupManager ? _startupManager : new WindowsStartupManager(AppName);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacStartupManager with app path
            var appPath = Path.Combine(Constants.MainDirectory, $"{AppName}.app");
            startupManager = _startupManager is MacStartupManager ? _startupManager : new MacStartupManager(AppName, appPath);
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxStartupManager with executable path
            var appExec = Path.Combine(Constants.MainDirectory, AppName);
            startupManager = _startupManager is LinuxStartupManager ? _startupManager : new LinuxStartupManager(AppName, appExec);
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the startup manager if needed
        if (_startupManager == null || _startupManager.GetType() != startupManager.GetType())
            _startupManager = startupManager;

        startupManager.Delete();
    }

    /// <summary>
    /// Opens a folder in the system's default file explorer.
    /// </summary>
    /// <param name="folderPath">The path to the folder to open.</param>
    public static void OpenFolder(string folderPath)
    {
        Log.Debug("Opening folder with path '{FolderPath}'...", folderPath);

        IFileExplorerManager fileExplorerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsFileExplorerManager
            fileExplorerManager = _fileExplorerManager is WindowsFileExplorerManager ? _fileExplorerManager : new WindowsFileExplorerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacFileExplorerManager
            fileExplorerManager = _fileExplorerManager is MacFileExplorerManager ? _fileExplorerManager : new MacFileExplorerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxFileExplorerManager
            fileExplorerManager = _fileExplorerManager is LinuxFileExplorerManager ? _fileExplorerManager : new LinuxFileExplorerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the file explorer manager if needed
        if (_fileExplorerManager == null || _fileExplorerManager.GetType() != fileExplorerManager.GetType())
            _fileExplorerManager = fileExplorerManager;

        fileExplorerManager.OpenFolder(folderPath);
    }

    /// <summary>
    /// Opens the folder containing the specified file and selects the file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    public static void OpenContainingFolderAndSelectFile(string filePath)
    {
        Log.Debug("Opening containing folder and selecting file with path '{FilePath}'...", filePath);

        IFileExplorerManager fileExplorerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsFileExplorerManager
            fileExplorerManager = _fileExplorerManager is WindowsFileExplorerManager ? _fileExplorerManager : new WindowsFileExplorerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacFileExplorerManager
            fileExplorerManager = _fileExplorerManager is MacFileExplorerManager ? _fileExplorerManager : new MacFileExplorerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxFileExplorerManager
            fileExplorerManager = _fileExplorerManager is LinuxFileExplorerManager ? _fileExplorerManager : new LinuxFileExplorerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the file explorer manager if needed
        if (_fileExplorerManager == null || _fileExplorerManager.GetType() != fileExplorerManager.GetType())
            _fileExplorerManager = fileExplorerManager;

        fileExplorerManager.OpenContainingFolderAndSelectFile(filePath);
    }

    /// <summary>
    /// Opens a file with the system's default application for that file type.
    /// </summary>
    /// <param name="filePath">The path to the file to open.</param>
    public static void OpenFile(string filePath)
    {
        Log.Debug("Opening file with path '{FilePath}'...", filePath);

        IFileExplorerManager fileExplorerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsFileExplorerManager
            fileExplorerManager = _fileExplorerManager is WindowsFileExplorerManager ? _fileExplorerManager : new WindowsFileExplorerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacFileExplorerManager
            fileExplorerManager = _fileExplorerManager is MacFileExplorerManager ? _fileExplorerManager : new MacFileExplorerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxFileExplorerManager
            fileExplorerManager = _fileExplorerManager is LinuxFileExplorerManager ? _fileExplorerManager : new LinuxFileExplorerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the file explorer manager if needed
        if (_fileExplorerManager == null || _fileExplorerManager.GetType() != fileExplorerManager.GetType())
            _fileExplorerManager = fileExplorerManager;

        fileExplorerManager.OpenFile(filePath);
    }

    /// <summary>
    /// Shuts down the computer.
    /// </summary>
    public static void Shutdown()
    {
        Log.Debug("Shutting down the computer...");

        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsPowerManager
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacPowerManager
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxPowerManager
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the power manager if needed
        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        powerManager.Shutdown();
    }

    /// <summary>
    /// Puts the computer to sleep.
    /// </summary>
    public static void Sleep()
    {
        Log.Debug("Putting the computer to sleep...");

        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsPowerManager
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacPowerManager
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxPowerManager
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the power manager if needed
        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        powerManager.Sleep();
    }

    /// <summary>
    /// Hibernates the computer.
    /// </summary>
    public static void Hibernate()
    {
        Log.Debug("Hibernating the computer...");

        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsPowerManager
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacPowerManager
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxPowerManager
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        // Update the power manager if needed
        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        powerManager.Hibernate();
    }

    /// <summary>
    /// Checks if hibernation is enabled on the system.
    /// </summary>
    /// <returns>True if hibernation is enabled, otherwise false.</returns>
    public static bool IsHibernateEnabled()
    {
        Log.Debug("Checking if hibernation is enabled...");

        IPowerManager powerManager;
        if (OperatingSystem.IsWindows())
        {
            // For Windows, create or use existing WindowsPowerManager
            powerManager = _powerManager is WindowsPowerManager ? _powerManager : new WindowsPowerManager();
        }
        else if (OperatingSystem.IsMacOS())
        {
            // For macOS, create or use existing MacPowerManager
            powerManager = _powerManager is MacPowerManager ? _powerManager : new MacPowerManager();
        }
        else if (OperatingSystem.IsLinux())
        {
            // For Linux, create or use existing LinuxPowerManager
            powerManager = _powerManager is LinuxPowerManager ? _powerManager : new LinuxPowerManager();
        }
        else
        {
            Log.Fatal("Unsupported operating system.");
            return false;
        }

        // Update the power manager if needed
        if (_powerManager == null || _powerManager.GetType() != powerManager.GetType())
            _powerManager = powerManager;

        return powerManager.IsHibernateEnabled();
    }
}
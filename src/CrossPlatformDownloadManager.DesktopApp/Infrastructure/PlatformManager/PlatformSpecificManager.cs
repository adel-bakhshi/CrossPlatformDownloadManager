using System;
using System.IO;
using AutoLaunch;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;

/// <summary>
/// A class that manages platform-specific operations for the application.
/// This class provides methods for handling startup registration, file operations, and power management.
/// </summary>
public class PlatformSpecificManager
{
    #region Constants

    /// <summary>
    /// The name of the application used for startup registration.
    /// </summary>
    private const string AppName = "CrossPlatformDownloadManager.DesktopApp";

    #endregion

    #region Private Fields

    /// <summary>
    /// Singleton instance of <see cref="PlatformSpecificManager"/>.
    /// </summary>
    private static PlatformSpecificManager? _current;

    /// <summary>
    /// The <see cref="AutoLauncher"/> instance to manage the application's startup configuration.
    /// </summary>
    private readonly AutoLauncher _autoLauncher;

    // /// <summary>
    // /// Manager for handling startup operations on different operating systems.
    // /// </summary>
    // private IStartupManager? _startupManager;

    /// <summary>
    /// Manager for handling file explorer operations on different operating systems.
    /// </summary>
    private IFileExplorerManager? _fileExplorerManager;

    /// <summary>
    /// Manager for handling power management operations on different operating systems.
    /// </summary>
    private IPowerManager? _powerManager;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the singleton instance of <see cref="PlatformSpecificManager"/>.
    /// </summary>
    public static PlatformSpecificManager Current => GetOrCreateInstance();

    #endregion

    private PlatformSpecificManager()
    {
        var appExecutable = AppName + (OperatingSystem.IsWindows() ? ".exe" : "");
        var appPath = Path.Combine(Constants.MainDirectory, appExecutable);

        _autoLauncher = new AutoLaunchBuilder()
            .SetAppName("Cross platform Download Manager (CDM)")
            .SetAppPath(appPath)
            .SetWorkScope(WorkScope.CurrentUser)
            .SetWindowsEngine(WindowsEngine.Registry)
            .SetLinuxEngine(LinuxEngine.Freedesktop)
            .SetMacOSEngine(MacOSEngine.AppleScript)
            .Build();
    }

    /// <summary>
    /// Checks if the application is registered as a startup item.
    /// </summary>
    /// <returns>True if the application is registered as a startup item, otherwise false.</returns>
    public bool IsStartupRegistered()
    {
        if (AutoLauncher.IsSupported())
            return _autoLauncher.GetStatus();

        Log.Fatal("Unsupported operating system.");
        return false;
    }

    /// <summary>
    /// Registers the application as a startup item.
    /// </summary>
    public void RegisterStartup()
    {
        Log.Debug("Registering application as a startup item...");

        if (!AutoLauncher.IsSupported())
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        _autoLauncher.Enable();
    }

    /// <summary>
    /// Deletes the application's startup configuration.
    /// </summary>
    public void DeleteStartup()
    {
        if (!AutoLauncher.IsSupported())
        {
            Log.Fatal("Unsupported operating system.");
            return;
        }

        _autoLauncher.Disable();
    }

    /// <summary>
    /// Opens a folder in the system's default file explorer.
    /// </summary>
    /// <param name="folderPath">The path to the folder to open.</param>
    public void OpenFolder(string folderPath)
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
    public void OpenContainingFolderAndSelectFile(string filePath)
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
    public void OpenFile(string filePath)
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
    public void Shutdown()
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
    public void Sleep()
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
    public void Hibernate()
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
    public bool IsHibernateEnabled()
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

    #region Helpers

    /// <summary>
    /// Gets if exists or creates a new instance of the <see cref="PlatformSpecificManager"/> class.
    /// </summary>
    /// <returns>Returns the instance of the <see cref="PlatformSpecificManager"/> class.</returns>
    private static PlatformSpecificManager GetOrCreateInstance()
    {
        _current ??= new PlatformSpecificManager();
        return _current;
    }

    #endregion
}
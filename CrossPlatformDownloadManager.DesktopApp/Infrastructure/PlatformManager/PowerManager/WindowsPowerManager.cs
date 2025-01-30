using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;

public class WindowsPowerManager : IPowerManager
{
    public void Shutdown()
    {
        var result = ExecuteCommand("shutdown", "/s /t 0");
        Log.Information(result);
    }

    public void Sleep()
    {
        // Call SetSuspendState with hibernate = false to sleep the computer
        var result = SetSuspendState(false, false, false);
        if (!result)
            throw new InvalidOperationException("Failed to put the computer to sleep. Error: " + Marshal.GetLastWin32Error());
    }

    public void Hibernate()
    {
        // Check if hibernation is enabled
        if (!IsHibernateEnabled())
            throw new InvalidOperationException("Hibernation is not enabled on this computer.");

        // Call SetSuspendState with hibernate = true to hibernate the computer
        var result = SetSuspendState(true, false, false);
        if (!result)
            throw new InvalidOperationException("Failed to hibernate the computer. Error: " + Marshal.GetLastWin32Error());
    }

    public bool IsHibernateEnabled()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var powerKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power");
        if (powerKey == null)
            return false;

        // Check HibernateEnabled
        var hibernateEnabledValue = powerKey.GetValue("HibernateEnabled");
        if (hibernateEnabledValue != null && int.TryParse(hibernateEnabledValue.ToString(), out var enabled) && enabled == 1)
            return true;

        // Check HibernateEnabledDefault
        var hibernateEnabledDefaultValue = powerKey.GetValue("HibernateEnabledDefault");
        if (hibernateEnabledDefaultValue != null && int.TryParse(hibernateEnabledDefaultValue.ToString(), out var enabledDefault) && enabledDefault == 1)
            return true;

        // Check additional settings in PlatformSettings
        using var platformSettingsKey = powerKey.OpenSubKey("PlatformSettings");
        using var deviceCapabilitiesKey = platformSettingsKey?.OpenSubKey("DeviceCapabilities");
        if (deviceCapabilitiesKey == null)
            return false;

        var supportsHibernate = deviceCapabilitiesKey.GetValue("SupportsHibernate") as int? ?? 0;
        return supportsHibernate == 1;
    }

    #region Imports

    // Import the SetSuspendState function from PowrProf.dll
    [DllImport("PowrProf.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    #endregion

    #region Helpers

    private static string ExecuteCommand(string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        var output = process?.StandardOutput.ReadToEnd();
        var error = process?.StandardError.ReadToEnd();
        process?.WaitForExit();

        // Handle errors
        if (process is not { ExitCode: 0 })
            throw new InvalidOperationException($"Failed to execute command: {command}. Error: {error}");

        return output ?? string.Empty;
    }

    #endregion
}
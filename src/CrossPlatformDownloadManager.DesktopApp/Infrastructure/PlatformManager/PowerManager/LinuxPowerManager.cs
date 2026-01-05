using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;

[SupportedOSPlatform("linux")]
public class LinuxPowerManager : IPowerManager
{
    public void Shutdown()
    {
        ExecuteCommand("systemctl poweroff");
    }

    public void Sleep()
    {
        ExecuteCommand("systemctl suspend");
    }

    public void Hibernate()
    {
        // Check if hibernation is enabled
        if (!IsHibernateEnabled())
            throw new InvalidOperationException("Hibernation is not enabled on this computer.");
        
        ExecuteCommand("systemctl hibernate");
    }

    public bool IsHibernateEnabled()
    {
        if (!OperatingSystem.IsLinux())
            return false;
        
        // Check if hibernate is supported by reading /sys/power/state
        var powerState = ExecuteCommand("cat /sys/power/state");
        return powerState.Contains("disk");
    }

    #region Helpers

    private static string ExecuteCommand(string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
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
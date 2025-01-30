using System;
using System.Diagnostics;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.PowerManager;

public class MacPowerManager : IPowerManager
{
    public void Shutdown()
    {
        ExecuteCommand("sudo shutdown -h now");
    }

    public void Sleep()
    {
        ExecuteCommand("pmset sleepnow");
    }

    public void Hibernate()
    {
        // Check if hibernation is enabled
        if (!IsHibernateEnabled())
            throw new InvalidOperationException("Hibernation is not enabled on this computer.");
        
        // macOS does not have a direct hibernate command, but it uses Safe Sleep
        // This command forces the system to save state to disk and sleep
        ExecuteCommand("pmset sleepnow");
    }

    public bool IsHibernateEnabled()
    {
        if (!OperatingSystem.IsMacOS())
            return false;
        
        // Check if Safe Sleep (hibernatemode 3) is enabled
        var hibernateMode = ExecuteCommand("pmset -g | grep hibernatemode");
        return hibernateMode.Contains('3');
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
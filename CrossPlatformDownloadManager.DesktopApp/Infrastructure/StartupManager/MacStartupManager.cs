using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.StartupManager;

[SupportedOSPlatform("macos")]
public class MacStartupManager : IStartupManager
{
    #region Private Fields

    private readonly string _appName;
    private readonly string _appPath;

    #endregion

    public MacStartupManager(string appName, string appPath)
    {
        _appName = appName;
        _appPath = appPath;
    }

    public bool IsRegistered()
    {
        // This is a simplified check. A more robust check might involve parsing the output of `osascript`.
        // However, for most cases, simply checking if the Login Item exists is sufficient.
        var appleScript = $"""
                                   tell application "System Events"
                                       return exists login item "{_appName}"
                                   end tell
                           """;

        var output = RunAppleScript(appleScript);
        return output.Trim().Equals("true", StringComparison.CurrentCultureIgnoreCase);
    }

    public void Register()
    {
        var appleScript = $$"""
                                    tell application "System Events"
                                        make login item at end with properties {name:"{{_appName}}", path:"{{_appPath}}", hidden:false}
                                    end tell
                            """;

        RunAppleScript(appleScript);
    }

    public void Delete()
    {
        var appleScript = $"""
                                   tell application "System Events"
                                       delete login item "{_appName}" of login items
                                   end tell
                           """;

        RunAppleScript(appleScript);
    }

    #region Helpers

    private static string RunAppleScript(string script)
    {
        using var process = new Process();
        process.StartInfo.FileName = "osascript";
        process.StartInfo.Arguments = $"-e \"{script}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.Start();
        process.WaitForExit();
        return process.StandardOutput.ReadToEnd();
    }

    #endregion
}
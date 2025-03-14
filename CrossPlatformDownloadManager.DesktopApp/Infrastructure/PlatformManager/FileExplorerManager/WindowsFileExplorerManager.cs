using System.Diagnostics;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;

[SupportedOSPlatform("windows")]
public class WindowsFileExplorerManager : IFileExplorerManager
{
    public void OpenFolder(string folderPath)
    {
        Process.Start("explorer.exe", folderPath);
    }

    public void OpenContainingFolderAndSelectFile(string filePath)
    {
        var escapedFilePath = filePath.Replace("\"", "\"\"");
        Process.Start("explorer.exe", $"/select,\"{escapedFilePath}\"");
    }

    public void OpenFile(string filePath)
    {
        Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
    }
}
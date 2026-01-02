using System.Diagnostics;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;

[SupportedOSPlatform("linux")]
public class LinuxFileExplorerManager : IFileExplorerManager
{
    public void OpenFolder(string folderPath)
    {
        Process.Start("xdg-open", folderPath); // Use xdg-open for cross-desktop compatibility
    }

    public void OpenContainingFolderAndSelectFile(string filePath)
    {
        Process.Start("xdg-open", $"\"{filePath}\""); // Use xdg-open for cross-desktop compatibility
    }

    public void OpenFile(string filePath)
    {
        Process.Start("xdg-open", filePath); // Use xdg-open for cross-desktop compatibility
    }
}
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;

[SupportedOSPlatform("macos")]
public class MacFileExplorerManager : IFileExplorerManager
{
    public void OpenFolder(string folderPath)
    {
        Process.Start("open", folderPath);
    }

    public void OpenContainingFolderAndSelectFile(string filePath)
    {
        Process.Start("open", $"-R \"{filePath}\"");
    }

    public void OpenFile(string filePath)
    {
        Process.Start("open", filePath);
    }
}
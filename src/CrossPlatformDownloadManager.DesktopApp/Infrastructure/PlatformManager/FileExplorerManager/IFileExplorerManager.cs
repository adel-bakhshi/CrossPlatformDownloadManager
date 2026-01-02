namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager.FileExplorerManager;

public interface IFileExplorerManager
{
    void OpenFolder(string folderPath);

    void OpenContainingFolderAndSelectFile(string filePath);
    
    void OpenFile(string filePath);
}
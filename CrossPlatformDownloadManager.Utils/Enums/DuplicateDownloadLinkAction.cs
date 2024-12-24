namespace CrossPlatformDownloadManager.Utils.Enums;

public enum DuplicateDownloadLinkAction : byte
{
    LetUserChoose,
    DuplicateWithNumber,
    OverwriteExisting,
    ShowCompleteDialogOrResume
}
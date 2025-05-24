namespace MultipartDownloader.Core.Enums;

public enum RestartReason : byte
{
    /// <summary>
    /// The file size is not match with the chunk length.
    /// </summary>
    FileSizeIsNotMatchWithChunkLength = 0,

    /// <summary>
    /// The file size is greater than the chunk length.
    /// </summary>
    TempFileCorruption
}
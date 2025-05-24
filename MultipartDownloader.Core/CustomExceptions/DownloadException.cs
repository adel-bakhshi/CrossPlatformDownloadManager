namespace MultipartDownloader.Core.CustomExceptions;

public class DownloadException : Exception
{
    #region Error codes

    /// <summary>
    /// Use this code when the size of temp file for a chunk is not equal to the chunk length
    /// </summary>
    internal const int FileSizeNotMatchWithChunkLength = 0;

    /// <summary>
    /// Use this code when the chunk files directory is not valid
    /// </summary>
    internal const int ChunkFilesDirectoryIsNotValid = 1;

    #endregion Error codes

    #region Properties

    /// <summary>
    /// Gets a dictionary of error messages that is accessible by error codes
    /// </summary>
    internal static Dictionary<int, string> ErrorMessages => GetErrorMessages();

    /// <summary>
    /// Gets or sets the error code of the DownloadException
    /// </summary>
    internal int ErrorCode { get; set; }

    #endregion Properties

    public DownloadException()
    {
    }

    public DownloadException(string? message) : base(message)
    {
    }

    public DownloadException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public DownloadException(int errorCode, string? message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DownloadException(int errorCode, string? message, Exception? innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a DownloadException with the specified error code
    /// </summary>
    /// <param name="errorCode">The error code of the DownloadException</param>
    /// <returns>A new instance of DownloadException</returns>
    public static DownloadException CreateDownloadException(int errorCode)
    {
        ErrorMessages.TryGetValue(errorCode, out var message);
        return new DownloadException(errorCode, message);
    }

    #region Helpers

    /// <summary>
    /// Returns a dictionary of error messages that is accessible by error codes
    /// </summary>
    /// <returns>Dictionary of error messages</returns>
    private static Dictionary<int, string> GetErrorMessages()
    {
        return new Dictionary<int, string>
        {
            [FileSizeNotMatchWithChunkLength] = "The file size does not match the downloaded size.",
            [ChunkFilesDirectoryIsNotValid] = "The chunk files directory is not valid.",
        };
    }

    #endregion Helpers
}
using MultipartDownloader.Core.Enums;

namespace MultipartDownloader.Core.CustomEventArgs;

public class ChunkDownloadRestartedEventArgs : EventArgs
{
    #region Properties

    public string ChunkId { get; set; }
    public RestartReason Reason { get; set; }

    #endregion Properties

    public ChunkDownloadRestartedEventArgs(string chunkId, RestartReason reason)
    {
        ChunkId = chunkId;
        Reason = reason;
    }
}
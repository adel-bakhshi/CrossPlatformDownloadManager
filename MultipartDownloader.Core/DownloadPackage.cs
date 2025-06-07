using Microsoft.Extensions.Logging;

namespace MultipartDownloader.Core;

/// <summary>
/// Represents a package containing information about a download operation.
/// </summary>
public class DownloadPackage : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets a value indicating whether the package is currently being saved.
    /// </summary>
    public bool IsSaving { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the save operation is complete.
    /// </summary>
    public bool IsSaveComplete { get; set; }

    /// <summary>
    /// Gets or sets the progress of the save operation.
    /// </summary>
    public double SaveProgress { get; set; }

    /// <summary>
    /// Gets or sets the status of the download operation.
    /// </summary>
    public DownloadStatus Status { get; set; } = DownloadStatus.None;

    /// <summary>
    /// Gets or sets the URLs from which the file is being downloaded.
    /// </summary>
    public string[] Urls { get; set; } = [];

    /// <summary>
    /// Gets or sets the total size of the file to be downloaded.
    /// </summary>
    public long TotalFileSize { get; set; }

    /// <summary>
    /// Gets or sets the name of the file to be saved.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the chunks of the file being downloaded.
    /// </summary>
    public Chunk[] Chunks { get; set; } = [];

    /// <summary>
    /// Gets the total size of the received bytes.
    /// </summary>
    public long ReceivedBytesSize => Chunks?.Sum(chunk => chunk.Position) ?? 0;

    /// <summary>
    /// Gets or sets a value indicating whether the download supports range requests.
    /// </summary>
    public bool IsSupportDownloadInRange { get; set; } = true;

    /// <summary>
    /// Gets or sets the temporary directory that the temp files of the chunks will be saved to.
    /// </summary>
    public string TemporarySavePath { get; set; } = string.Empty;

    /// <summary>
    /// Clears the chunks and resets the package.
    /// </summary>
    public void Clear()
    {
        if (Chunks.Length > 0)
        {
            foreach (Chunk chunk in Chunks)
                chunk.Clear();
        }

        Chunks = [];
    }

    /// <summary>
    /// Validates the chunks and ensures they are in the correct position.
    /// </summary>
    public void Validate()
    {
        foreach (var chunk in Chunks)
        {
            if (!chunk.IsValidPosition())
            {
                if (File.Exists(chunk.TempFilePath))
                    File.Delete(chunk.TempFilePath);

                chunk.Clear();
            }

            if (!IsSupportDownloadInRange)
                chunk.Clear();

            // When a download is canceled or an error occurs during the download, some of the file may have been downloaded but not saved to disk.
            // This is especially true if the MaximumMemoryBufferBytes property value is greater than 0.
            // In this case, the Chunk Position is greater than the File Position (because the downloaded data has not been saved to disk),
            // and if the download continues from the same Chunk Position, part of the file will be lost and the final file will be corrupted.
            // To fix this problem, we must check before downloading that the Chunk Position is not greater than the File Position.
            if (chunk.Position > chunk.FilePosition)
                chunk.Position = chunk.FilePosition;
        }
    }

    /// <summary>
    /// Builds the storage for the download package.
    /// </summary>
    /// <param name="reserveFileSize">Indicates whether to reserve the file size.</param>
    /// <param name="logger">The logger to use for logging.</param>
    public void BuildStorage(bool reserveFileSize, ILogger? logger = null)
    {
        var initSize = reserveFileSize ? TotalFileSize : 0;

        using var stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        if (initSize >= 0 && stream.Length == 0)
            stream.SetLength(initSize);

        logger?.LogInformation("Storage created successfully.");
    }

    /// <summary>
    /// Disposes of the download package, clearing the chunks and disposing of the storage.
    /// </summary>
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the download package, clearing the chunks and disposing of the storage.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        Clear();
        GC.SuppressFinalize(this);
        await ValueTask.FromResult(true);
    }

    /// <summary>
    /// Checks that is the storage is exists or not
    /// </summary>
    /// <returns>If storage is exists return's true, otherwise return's false</returns>
    public bool IsStorageExists()
    {
        if (string.IsNullOrEmpty(FileName))
            return false;

        return File.Exists(FileName);
    }

    /// <summary>
    /// Returns final storage stream
    /// </summary>
    /// <returns>File stream points to final storage</returns>
    public Stream? GetStorageStream()
    {
        if (!File.Exists(FileName))
            return null;

        var stream = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>
    /// Creates the temporary directory for saving the temp files of the chunks.
    /// </summary>
    public void CreateTemporarySavePath()
    {
        // Make sure the final directory exists
        if (!Directory.Exists(TemporarySavePath))
            Directory.CreateDirectory(TemporarySavePath);
    }
}
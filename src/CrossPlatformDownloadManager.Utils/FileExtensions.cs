namespace CrossPlatformDownloadManager.Utils;

/// <summary>
/// Provides extension methods for file operations.
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Asynchronously moves a file from a source path to a destination path.
    /// </summary>
    /// <param name="sourceFile">The source file path to move.</param>
    /// <param name="destinationFile">The destination file path where the file will be moved.</param>
    /// <remarks>
    /// This method copies the file to the destination first, then deletes the source file.
    /// It includes a small delay before deletion to ensure proper file handling.
    /// </remarks>
    public static async Task MoveFileAsync(this string? sourceFile, string? destinationFile)
    {
        // Check if either source or destination file paths are null or empty
        if (sourceFile.IsStringNullOrEmpty() || destinationFile.IsStringNullOrEmpty())
            return;

        // Open source file for reading and destination file for writing
        await using (var sourceStream = new FileStream(sourceFile!, FileMode.Open, FileAccess.Read))
        {
            await using (var destinationStream = new FileStream(destinationFile!, FileMode.Create, FileAccess.Write))
            {
                // Copy source stream to destination stream asynchronously
                await sourceStream.CopyToAsync(destinationStream);
                await destinationStream.FlushAsync();
            }
        }
        
        // Add a small delay before deletion
        await Task.Delay(100);
        // Delete the source file after successful copy
        File.Delete(sourceFile!);
    }
}
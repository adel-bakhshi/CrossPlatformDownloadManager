using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio.Enums;
using CrossPlatformDownloadManager.Utils;
using ManagedBass;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio;

/// <summary>
/// Manages audio operations for the application including initialization and playback of notification sounds.
/// </summary>
public static class AudioManager
{
    /// <summary>
    /// Initializes the audio manager by creating the songs directory and copying audio files from assets.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when song files are not found in assets.</exception>
    public static void Initialize()
    {
        Log.Information("Initializing audio manager...");

        // Create songs directory
        var songsDirectory = Path.Combine(Constants.ApplicationDataDirectory, "Songs");
        if (!Directory.Exists(songsDirectory))
        {
            Log.Debug("Creating songs directory: {SongsDirectory}", songsDirectory);
            Directory.CreateDirectory(songsDirectory);
        }

        // Get songs files from assets
        var assetsUri = new Uri("avares://CrossPlatformDownloadManager.DesktopApp/Assets/Songs");
        var songFiles = AssetLoader.GetAssets(assetsUri, null).ToList();
        if (songFiles.Count == 0)
        {
            Log.Error("No song files found in assets.");
            throw new InvalidOperationException("Song files not found.");
        }

        Log.Debug("Found {SongCount} song files in assets.", songFiles.Count);

        // Copy songs file to songs directory
        foreach (var songFile in songFiles)
        {
            var songName = Path.GetFileName(songFile.LocalPath);
            var songPath = Path.Combine(songsDirectory, songName);

            if (File.Exists(songPath))
            {
                Log.Debug("Song file already exists: {SongPath}", songPath);
                continue;
            }

            Log.Debug("Copying song file: {SongName}", songName);

            using var stream = AssetLoader.Open(songFile);
            using var fileStream = File.Create(songPath);
            stream.CopyTo(fileStream);

            Log.Debug("Song file copied successfully: {SongPath}", songPath);
        }

        // Log information
        Log.Information("Audio manager initialized successfully. Songs directory: {SongsDirectory}", songsDirectory);
    }

    /// <summary>
    /// Plays an audio file from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the audio file to play.</param>
    /// <exception cref="FileNotFoundException">Thrown when the audio file is not found.</exception>
    public static async Task PlayAsync(string filePath)
    {
        Log.Debug("Attempting to play audio file: {FilePath}", filePath);

        if (filePath.IsStringNullOrEmpty() || !File.Exists(filePath))
        {
            Log.Error("Audio file not found: {FilePath}", filePath);
            throw new FileNotFoundException("Audio file not found.");
        }

        try
        {
            var player = new MediaPlayer();
            await player.LoadAsync(filePath);
            player.Volume = 1;
            player.Play();

            Log.Debug("Audio file played successfully: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to play audio file: {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Plays a notification sound based on the specified notification type.
    /// </summary>
    /// <param name="notificationType">The type of notification to play.</param>
    /// <exception cref="InvalidOperationException">Thrown when songs directory or audio file is not found.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid notification type is provided.</exception>
    public static async Task PlayAsync(AppNotificationType notificationType)
    {
        Log.Debug("Playing notification sound for: {NotificationType}", notificationType);

        string filePath;
        var songsDirectory = Path.Combine(Constants.ApplicationDataDirectory, "Songs");

        if (!Directory.Exists(songsDirectory))
        {
            Log.Error("Songs directory not found: {SongsDirectory}", songsDirectory);
            throw new InvalidOperationException("Songs directory not found.");
        }

        // Determine the file path based on notification type
        switch (notificationType)
        {
            case AppNotificationType.DownloadCompleted:
            {
                const string fileName = "download-completed.mp3";
                filePath = Path.Combine(songsDirectory, fileName);
                Log.Debug("Selected audio file for download completed: {FileName}", fileName);
                break;
            }

            case AppNotificationType.DownloadStopped:
            {
                const string fileName = "download-stopped.mp3";
                filePath = Path.Combine(songsDirectory, fileName);
                Log.Debug("Selected audio file for download stopped: {FileName}", fileName);
                break;
            }

            case AppNotificationType.DownloadFailed:
            {
                const string fileName = "download-failed.mp3";
                filePath = Path.Combine(songsDirectory, fileName);
                Log.Debug("Selected audio file for download failed: {FileName}", fileName);
                break;
            }

            case AppNotificationType.QueueStarted:
            {
                const string fileName = "queue-started.mp3";
                filePath = Path.Combine(songsDirectory, fileName);
                Log.Debug("Selected audio file for queue started: {FileName}", fileName);
                break;
            }

            case AppNotificationType.QueueStopped:
            {
                const string fileName = "queue-stopped.mp3";
                filePath = Path.Combine(songsDirectory, fileName);
                Log.Debug("Selected audio file for queue stopped: {FileName}", fileName);
                break;
            }

            case AppNotificationType.QueueFinished:
            {
                const string fileName = "queue-finished.mp3";
                filePath = Path.Combine(songsDirectory, fileName);
                Log.Debug("Selected audio file for queue finished: {FileName}", fileName);
                break;
            }

            default:
            {
                Log.Error("Invalid notification type: {NotificationType}", notificationType);
                throw new ArgumentOutOfRangeException(nameof(notificationType), notificationType, null);
            }
        }

        if (filePath.IsStringNullOrEmpty() || !File.Exists(filePath))
        {
            Log.Error("Audio file not found for notification type {NotificationType}: {FilePath}", notificationType, filePath);
            throw new InvalidOperationException("Audio file not found.");
        }

        Log.Debug("Playing notification sound for {NotificationType} from: {FilePath}", notificationType, filePath);
        await PlayAsync(filePath);

        Log.Debug("Notification sound played successfully for: {NotificationType}", notificationType);
    }
}
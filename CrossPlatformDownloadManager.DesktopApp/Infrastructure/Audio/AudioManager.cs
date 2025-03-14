using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio.Enums;
using CrossPlatformDownloadManager.Utils;
using ManagedBass;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Audio;

public static class AudioManager
{
    public static void Initialize()
    {
        var songsDirectory = Path.Combine(Constants.ApplicationDataDirectory, "Songs");
        if (!Directory.Exists(songsDirectory))
            Directory.CreateDirectory(songsDirectory);

        var assetsUri = new Uri("avares://CrossPlatformDownloadManager.DesktopApp/Assets/Songs");
        var songFiles = AssetLoader.GetAssets(assetsUri, null).ToList();
        if (songFiles.Count == 0)
            throw new InvalidOperationException("Song files not found.");

        foreach (var songFile in songFiles)
        {
            var songName = Path.GetFileName(songFile.LocalPath);
            var songPath = Path.Combine(songsDirectory, songName);
            if (File.Exists(songPath))
                continue;

            using var stream = AssetLoader.Open(songFile);
            using var fileStream = File.Create(songPath);
            stream.CopyTo(fileStream);
        }
    }

    public static async Task PlayAsync(string filePath)
    {
        if (filePath.IsNullOrEmpty() || !File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found.");

        var player = new MediaPlayer();
        await player.LoadAsync(filePath);
        player.Volume = 1;
        player.Play();
    }

    public static async Task PlayAsync(AppNotificationType notificationType)
    {
        string filePath;
        var songsDirectory = Path.Combine(Constants.ApplicationDataDirectory, "Songs");
        if (!Directory.Exists(songsDirectory))
            throw new InvalidOperationException("Songs directory not found.");

        switch (notificationType)
        {
            case AppNotificationType.DownloadCompleted:
                {
                    const string fileName = "download-completed.mp3";
                    filePath = Path.Combine(songsDirectory, fileName);
                    break;
                }

            case AppNotificationType.DownloadStopped:
                {
                    const string fileName = "download-stopped.mp3";
                    filePath = Path.Combine(songsDirectory, fileName);
                    break;
                }

            case AppNotificationType.DownloadFailed:
                {
                    const string fileName = "download-failed.mp3";
                    filePath = Path.Combine(songsDirectory, fileName);
                    break;
                }

            case AppNotificationType.QueueStarted:
                {
                    const string fileName = "queue-started.mp3";
                    filePath = Path.Combine(songsDirectory, fileName);
                    break;
                }

            case AppNotificationType.QueueStopped:
                {
                    const string fileName = "queue-stopped.mp3";
                    filePath = Path.Combine(songsDirectory, fileName);
                    break;
                }

            case AppNotificationType.QueueFinished:
                {
                    const string fileName = "queue-finished.mp3";
                    filePath = Path.Combine(songsDirectory, fileName);
                    break;
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(notificationType), notificationType, null);
        }

        if (filePath.IsNullOrEmpty() || !File.Exists(filePath))
            throw new InvalidOperationException("Audio file not found.");

        await PlayAsync(filePath);
    }
}
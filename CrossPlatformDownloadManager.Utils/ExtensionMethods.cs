using System.Collections.ObjectModel;
using System.Net;
using Avalonia.Media;
using Avalonia.Platform;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Utils;

public static class ExtensionMethods
{
    public static bool IsStringNullOrEmpty(this string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value);
    }

    public static T? ConvertFromJson<T>(this string? json, JsonSerializerSettings? jsonSerializerSettings)
    {
        return json.IsStringNullOrEmpty() ? default : JsonConvert.DeserializeObject<T>(json!, jsonSerializerSettings);
    }

    public static T? ConvertFromJson<T>(this string? json)
    {
        return json.ConvertFromJson<T>(null);
    }

    public static string ConvertToJson(this object? value, JsonSerializerSettings? serializerSettings = null)
    {
        return JsonConvert.SerializeObject(value, serializerSettings);
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T>? items)
    {
        return items == null ? [] : new ObservableCollection<T>(items);
    }

    public static string ToFileSize(this double bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        switch (bytes)
        {
            case < 0:
                return string.Empty;

            case 0:
                return "0 KB";
        }

        var tb = bytes / Constants.TeraByte;
        if (roundSize)
        {
            if (roundToUpper)
                tb = Math.Ceiling(tb);
            else if (roundToLower)
                tb = Math.Floor(tb);
            else
                tb = Math.Round(tb);
        }

        if (tb >= 1)
            return $"{tb:N2} TB";

        var gb = bytes / Constants.GigaByte;
        if (roundSize)
        {
            if (roundToUpper)
                gb = Math.Ceiling(gb);
            else if (roundToLower)
                gb = Math.Floor(gb);
            else
                gb = Math.Round(gb);
        }

        if (gb >= 1)
            return $"{gb:N2} GB";

        var mb = bytes / Constants.MegaByte;
        if (roundSize)
        {
            if (roundToUpper)
                mb = Math.Ceiling(mb);
            else if (roundToLower)
                mb = Math.Floor(mb);
            else
                mb = Math.Round(mb);
        }

        if (mb >= 1)
            return $"{mb:N2} MB";

        var kb = bytes / Constants.KiloByte;
        if (roundSize)
        {
            if (roundToUpper)
                kb = Math.Ceiling(kb);
            else if (roundToLower)
                kb = Math.Floor(kb);
            else
                kb = Math.Round(kb);
        }

        if (kb >= 1)
            return $"{kb:N2} KB";

        return $"{bytes:N2} Byte" + (bytes > 1 ? "s" : "");
    }

    public static string ToFileSize(this double? bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    public static string ToFileSize(this long bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        return ((double)bytes).ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    public static string ToFileSize(this long? bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    public static string ToFileSize(this float bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        return ((double)bytes).ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    public static string ToFileSize(this float? bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    public static bool CheckUrlValidation(this string? url)
    {
        if (url.IsStringNullOrEmpty())
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static string? OpenTextAsset(this Uri? assetUri)
    {
        if (assetUri == null)
            return null;

        using var stream = AssetLoader.Open(assetUri);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string? OpenTextAsset(this string? assetPath)
    {
        if (assetPath.IsStringNullOrEmpty() || assetPath?.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) != true)
            return null;

        var assetUri = new Uri(assetPath);
        return assetUri.OpenTextAsset();
    }

    public static T? OpenJsonAsset<T>(this Uri? assetUri, JsonSerializerSettings? jsonSerializerSettings)
    {
        var json = assetUri.OpenTextAsset();
        return json.IsStringNullOrEmpty() ? default : json.ConvertFromJson<T>(jsonSerializerSettings);
    }

    public static T? OpenJsonAsset<T>(this string? assetPath, JsonSerializerSettings? jsonSerializerSettings)
    {
        if (assetPath.IsStringNullOrEmpty() || assetPath?.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) != true)
            return default;

        var assetUri = new Uri(assetPath);
        return assetUri.OpenJsonAsset<T>();
    }

    public static T? OpenJsonAsset<T>(this Uri? assetUri)
    {
        return assetUri.OpenJsonAsset<T>(null);
    }

    /// <summary>
    /// Gets the file name from an URL.
    /// </summary>
    /// <param name="url">The URL to get the file name from.</param>
    /// <returns>Returns the file name if found, otherwise null.</returns>
    public static string? GetFileName(this string? url)
    {
        if (url.IsStringNullOrEmpty())
            return null;

        url = url!.Replace('\\', '/').Trim();

        var uri = new Uri(url);
        var fileName = string.Empty;
        if (uri.IsFile)
            fileName = Path.GetFileName(uri.LocalPath);

        var tempBaseUri = new Uri("https://localhost/temp");
        if (fileName.IsStringNullOrEmpty())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(tempBaseUri, url);

            fileName = Path.GetFileName(uri.LocalPath);
        }

        if (fileName.IsStringNullOrEmpty())
        {
            var startIndex = url.LastIndexOf('/') + 1;
            var path = url.Substring(startIndex);
            if (path.Contains('.'))
            {
                var endIndex = path.LastIndexOf('.');
                if (path.Substring(endIndex).Contains('?'))
                {
                    endIndex = path.LastIndexOf('?');
                    fileName = path.Substring(0, endIndex);
                }
                else
                {
                    fileName = path;
                }
            }
            else
            {
                fileName = null;
            }
        }

        if (fileName.IsStringNullOrEmpty())
            return fileName;

        if (fileName!.Contains('/'))
            fileName = fileName.Substring(fileName.LastIndexOf('/') + 1);

        if (fileName.Contains('?'))
            fileName = fileName.Substring(0, fileName.IndexOf('?'));

        return fileName;
    }

    /// <summary>
    /// Gets the file name from the Content-Disposition header.
    /// </summary>
    /// <param name="contentDisposition">The Content-Disposition header value.</param>
    /// <returns>Returns the file name if found, otherwise null.</returns>
    public static string? GetFileNameFromContentDisposition(this string? contentDisposition)
    {
        // Possible values for Content-Disposition header:
        // Content-Disposition: inline
        // Content-Disposition: attachment
        // Content-Disposition: attachment; filename="file name.jpg"
        // Content-Disposition: attachment; filename*=UTF-8''file%20name.jpg

        // Check if Content-Disposition header value is null or empty
        if (contentDisposition.IsStringNullOrEmpty())
            return null;

        // Find the file name section and make sure it has value
        var fileNameSection = contentDisposition!.Split(';').FirstOrDefault(s => s.Trim().StartsWith("filename="))?.Trim();
        if (fileNameSection.IsStringNullOrEmpty())
            return null;

        // Get the file name from the section and make sure it has value
        var fileName = fileNameSection!.Split('=').LastOrDefault()?.Trim('\"');
        if (fileName.IsStringNullOrEmpty())
            return null;

        // Check if the filename contains encoded characters
        if (!fileName!.Contains("''"))
            return fileName;

        var encodedFileName = fileName.Split("''").LastOrDefault()?.Trim();
        // Decode the encoded filename
        return encodedFileName.IsStringNullOrEmpty() ? null : WebUtility.UrlDecode(encodedFileName);
    }

    public static bool HasFileExtension(this string? fileName)
    {
        if (fileName.IsStringNullOrEmpty())
            return false;

        return !Path.GetExtension(fileName!).IsStringNullOrEmpty();
    }

    public static string GetShortTime(this TimeSpan? time)
    {
        if (time == null)
            return string.Empty;

        if (time == TimeSpan.Zero)
            return "00 : 00";

        return (time.Value.Hours >= 1 ? $"{time.Value.Hours:00} : " : "") + $"{time.Value.Minutes:00} : {time.Value.Seconds:00}";
    }

    public static T? DeepCopy<T>(this T? obj, JsonSerializerSettings? serializerSettings = null)
    {
        var json = obj.ConvertToJson(serializerSettings);
        return json.ConvertFromJson<T>();
    }

    public static T? DeepCopy<T>(this T? obj, bool ignoreLoops)
    {
        var serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        return DeepCopy(obj, serializerSettings);
    }

    public static async Task CopyFileAsync(this string sourcePath, string destinationPath)
    {
        // Use a buffer size that's a multiple of 4KB for optimal performance.
        const int bufferSize = 4096;

        await using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        await using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
        var buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
            await destinationStream.WriteAsync(buffer.AsMemory(0, bytesRead));
    }

    public static async Task ZipDirectoryAsync(this string sourceDir, string zipFilePath)
    {
        await using var outputStream = File.Create(zipFilePath);
        await using var zipStream = new ZipOutputStream(outputStream);
        zipStream.SetLevel(9); // 0-9, 9 being the highest compression

        var folderOffset = sourceDir.Length + (sourceDir.EndsWith('\\') ? 0 : 1); // Adjust offset for directory separator
        await CompressFolderAsync(sourceDir, zipStream, folderOffset);
        zipStream.Finish();
    }

    public static async Task UnZipFileAsync(this string zipFilePath, string destinationDir)
    {
        if (!Directory.Exists(destinationDir))
            Directory.CreateDirectory(destinationDir);

        await using var fileStream = File.OpenRead(zipFilePath);
        var zipFile = new ZipFile(fileStream);

        foreach (ZipEntry entry in zipFile)
        {
            if (entry.Size <= 0)
                continue;

            var targetFile = Path.Combine(destinationDir, entry.Name).Replace('/', '\\');
            var directoryPath = Path.GetDirectoryName(targetFile);
            if (directoryPath.IsStringNullOrEmpty())
                continue;

            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath!);

            await using var outputFile = File.Create(targetFile);

            await using var zippedStream = zipFile.GetInputStream(entry);
            var buffer = new byte[4096];

            int readBytes;
            while ((readBytes = await zippedStream.ReadAsync(buffer)) > 0)
            {
                await outputFile.WriteAsync(buffer.AsMemory(0, readBytes));
                await outputFile.FlushAsync();
            }
        }
    }

    public static string GetDriveName(this DriveInfo driveInfo)
    {
        var driveName = driveInfo.Name.EndsWith('\\') ? driveInfo.Name.Substring(0, driveInfo.Name.Length - 1) : driveInfo.Name;
        if (!driveName.EndsWith(':'))
            driveName += ":";

        return driveName;
    }

    public static string? GetDomainFromUrl(this string? url)
    {
        if (!url.CheckUrlValidation())
            return null;

        var uri = new Uri(url!);
        return uri.Host;
    }

    public static Color? ConvertFromHex(this string? hexValue)
    {
        if (hexValue.IsStringNullOrEmpty())
            return null;

        return Color.TryParse(hexValue, out var color) ? color : null;
    }

    #region Helpers

    private static async Task CompressFolderAsync(string sourceFolder, ZipOutputStream zipStream, int folderOffset)
    {
        var files = Directory.GetFiles(sourceFolder);
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            var entryName = file.Substring(folderOffset); // Remove the folder path
            var entry = new ZipEntry(entryName)
            {
                DateTime = fileInfo.LastWriteTime,
                Size = fileInfo.Length
            };

            await zipStream.PutNextEntryAsync(entry);

            var buffer = new byte[4096];
            await using var fileStream = File.OpenRead(file);

            int sourceBytes;
            while ((sourceBytes = await fileStream.ReadAsync(buffer)) > 0)
            {
                await zipStream.WriteAsync(buffer.AsMemory(0, sourceBytes));
                await zipStream.FlushAsync();
            }

            zipStream.CloseEntry();
        }

        // Process subdirectories
        var folders = Directory.GetDirectories(sourceFolder);
        foreach (var folder in folders)
        {
            await CompressFolderAsync(folder, zipStream, folderOffset);
        }
    }

    #endregion
}
using System.Collections.ObjectModel;
using System.Net;
using Avalonia.Media;
using Avalonia.Platform;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Utils;

public static class ExtensionMethods
{
    /// <summary>
    /// Checks if a string is null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null or empty, false otherwise.</returns>
    public static bool IsStringNullOrEmpty(this string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Converts a JSON string to an object.
    /// </summary>
    /// <param name="json">The JSON string to convert.</param>
    /// <param name="jsonSerializerSettings">The serializer settings to use.</param>
    /// <typeparam name="T">The type of the object to convert.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T? ConvertFromJson<T>(this string? json, JsonSerializerSettings? jsonSerializerSettings)
    {
        return json.IsStringNullOrEmpty() ? default : JsonConvert.DeserializeObject<T>(json!, jsonSerializerSettings);
    }

    /// <summary>
    /// Converts a JSON string to an object.
    /// </summary>
    /// <param name="json">The JSON string to convert.</param>
    /// <typeparam name="T">The type of the object to convert.</typeparam>
    /// <returns>The deserialized object.</returns>
    public static T? ConvertFromJson<T>(this string? json)
    {
        return json.ConvertFromJson<T>(null);
    }

    /// <summary>
    /// Converts an object to JSON with the specified settings.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <param name="serializerSettings">The serializer settings to use.</param>
    /// <returns>The JSON string.</returns>
    public static string ConvertToJson(this object? value, JsonSerializerSettings? serializerSettings = null)
    {
        return JsonConvert.SerializeObject(value, serializerSettings);
    }

    /// <summary>
    /// Converts an IEnumerable to an ObservableCollection.
    /// </summary>
    /// <param name="items">The items to convert.</param>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <returns>The observable collection.</returns>
    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T>? items)
    {
        return items == null ? [] : new ObservableCollection<T>(items);
    }

    /// <summary>
    /// Converts a double to a file size string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="roundSize">If true, the size will be rounded.</param>
    /// <param name="roundToUpper">If true, the size will be rounded to the upper value.</param>
    /// <param name="roundToLower">If true, the size will be rounded to the lower value.</param>
    /// <returns>The file size string.</returns>
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

    /// <summary>
    /// Converts a double to a file size string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="roundSize">If true, the size will be rounded.</param>
    /// <param name="roundToUpper">If true, the size will be rounded to the upper value.</param>
    /// <param name="roundToLower">If true, the size will be rounded to the lower value.</param>
    /// <returns>The file size string.</returns>
    public static string ToFileSize(this double? bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    /// <summary>
    /// Converts a long to a file size string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="roundSize">If true, the size will be rounded.</param>
    /// <param name="roundToUpper">If true, the size will be rounded to the upper value.</param>
    /// <param name="roundToLower">If true, the size will be rounded to the lower value.</param>
    /// <returns>The file size string.</returns>
    public static string ToFileSize(this long bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        return ((double)bytes).ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    /// <summary>
    /// Converts a long to a file size string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="roundSize">If true, the size will be rounded.</param>
    /// <param name="roundToUpper">If true, the size will be rounded to the upper value.</param>
    /// <param name="roundToLower">If true, the size will be rounded to the lower value.</param>
    /// <returns>The file size string.</returns>
    public static string ToFileSize(this long? bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    /// <summary>
    /// Converts a float to a file size string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="roundSize">If true, the size will be rounded.</param>
    /// <param name="roundToUpper">If true, the size will be rounded to the upper value.</param>
    /// <param name="roundToLower">If true, the size will be rounded to the lower value.</param>
    /// <returns>The file size string.</returns>
    public static string ToFileSize(this float bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        return ((double)bytes).ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    /// <summary>
    /// Converts a float to a file size string.
    /// </summary>
    /// <param name="bytes">The bytes to convert.</param>
    /// <param name="roundSize">If true, the size will be rounded.</param>
    /// <param name="roundToUpper">If true, the size will be rounded to the upper value.</param>
    /// <param name="roundToLower">If true, the size will be rounded to the lower value.</param>
    /// <returns>The file size string.</returns>
    public static string ToFileSize(this float? bytes, bool roundSize = false, bool roundToUpper = false, bool roundToLower = false)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize(roundSize, roundToUpper, roundToLower);
    }

    /// <summary>
    /// Checks if a URL is valid.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if the URL is valid, otherwise false.</returns>
    public static bool CheckUrlValidation(this string? url)
    {
        if (url.IsStringNullOrEmpty())
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Opens a text asset and returns its content as a string.
    /// </summary>
    /// <param name="assetUri">The asset URI.</param>
    /// <returns>Returns the content of the asset if found, otherwise null.</returns>
    public static async Task<string?> OpenTextAssetAsync(this Uri? assetUri)
    {
        if (assetUri == null)
            return null;

        await using var stream = AssetLoader.Open(assetUri);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Opens a text asset and returns its content as a string.
    /// </summary>
    /// <param name="assetPath">The asset path.</param>
    /// <returns>Returns the content of the asset if found, otherwise null.</returns>
    public static async Task<string?> OpenTextAssetAsync(this string? assetPath)
    {
        if (assetPath.IsStringNullOrEmpty() || assetPath?.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) != true)
            return null;

        var assetUri = new Uri(assetPath);
        return await assetUri.OpenTextAssetAsync();
    }

    /// <summary>
    /// Opens a JSON asset and converts it to an object.
    /// </summary>
    /// <param name="assetUri">The asset URI.</param>
    /// <param name="jsonSerializerSettings">The JSON serializer settings.</param>
    /// <typeparam name="T">The type of the object to convert to.</typeparam>
    /// <returns>Returns the object if found, otherwise null.</returns>
    public static async Task<T?> OpenJsonAssetAsync<T>(this Uri? assetUri, JsonSerializerSettings? jsonSerializerSettings)
    {
        var json = await assetUri.OpenTextAssetAsync();
        return json.IsStringNullOrEmpty() ? default : json.ConvertFromJson<T>(jsonSerializerSettings);
    }

    /// <summary>
    /// Opens a JSON asset and converts it to an object.
    /// </summary>
    /// <param name="assetPath">The asset path.</param>
    /// <param name="jsonSerializerSettings">The JSON serializer settings.</param>
    /// <typeparam name="T">The type of the object to convert to.</typeparam>
    /// <returns>Returns the object if found, otherwise null.</returns>
    public static async Task<T?> OpenJsonAssetAsync<T>(this string? assetPath, JsonSerializerSettings? jsonSerializerSettings)
    {
        if (assetPath.IsStringNullOrEmpty() || assetPath?.StartsWith("avares://", StringComparison.OrdinalIgnoreCase) != true)
            return default;

        var assetUri = new Uri(assetPath);
        return await assetUri.OpenJsonAssetAsync<T>(jsonSerializerSettings);
    }

    /// <summary>
    /// Opens a JSON asset and converts it to an object.
    /// </summary>
    /// <param name="assetUri">The asset URI.</param>
    /// <typeparam name="T">The type of the object to convert to.</typeparam>
    /// <returns>Returns the object if found, otherwise null.</returns>
    public static async Task<T?> OpenJsonAssetAsync<T>(this Uri? assetUri)
    {
        return await assetUri.OpenJsonAssetAsync<T>(null);
    }

    /// <summary>
    /// Gets all assets from an assets URI.
    /// </summary>
    /// <param name="assetsUri">The assets URI.</param>
    /// <returns>Returns a list of all assets.</returns>
    public static List<string> GetAllAssets(this Uri? assetsUri)
    {
        if (assetsUri == null)
            return [];

        return AssetLoader.GetAssets(assetsUri, null)
            .Select(uri => uri.OriginalString)
            .ToList();
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

    /// <summary>
    /// Checks if a file name has an extension.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <returns>Returns true if the file name has an extension, otherwise false.</returns>
    public static bool HasFileExtension(this string? fileName)
    {
        if (fileName.IsStringNullOrEmpty())
            return false;

        return !Path.GetExtension(fileName!).IsStringNullOrEmpty();
    }

    /// <summary>
    /// Gets the short time from a TimeSpan in the format "HH : mm : ss".
    /// </summary>
    /// <param name="time">The TimeSpan to get the short time from.</param>
    /// <returns>Returns the short time.</returns>
    public static string GetShortTime(this TimeSpan? time)
    {
        if (time == null)
            return string.Empty;

        if (time == TimeSpan.Zero)
            return "00 : 00";

        return (time.Value.Hours >= 1 ? $"{time.Value.Hours:00} : " : "") + $"{time.Value.Minutes:00} : {time.Value.Seconds:00}";
    }

    /// <summary>
    /// Creates a deep copy of an object.
    /// </summary>
    /// <param name="obj">The object to create a deep copy of.</param>
    /// <param name="serializerSettings">The serializer settings to use.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns the deep copy of the object.</returns>
    public static T? DeepCopy<T>(this T? obj, JsonSerializerSettings? serializerSettings = null)
    {
        var json = obj.ConvertToJson(serializerSettings);
        return json.ConvertFromJson<T>();
    }

    /// <summary>
    /// Creates a deep copy of an object.
    /// </summary>
    /// <param name="obj">The object to create a deep copy of.</param>
    /// <param name="ignoreLoops">Ignore loops in the object.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns the deep copy of the object.</returns>
    public static T? DeepCopy<T>(this T? obj, bool ignoreLoops)
    {
        var serializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        return DeepCopy(obj, serializerSettings);
    }

    /// <summary>
    /// Copies a file asynchronously.
    /// </summary>
    /// <param name="sourcePath">The source path of the file to copy.</param>
    /// <param name="destinationPath">The destination path of the copied file.</param>
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

    /// <summary>
    /// Gets the domain from a URL.
    /// </summary>
    /// <param name="url">The URL to get the domain from.</param>
    /// <returns>Returns the domain.</returns>
    public static string? GetDomainFromUrl(this string? url)
    {
        if (!url.CheckUrlValidation())
            return null;

        var uri = new Uri(url!);
        return uri.Host;
    }

    /// <summary>
    /// Converts a hexadecimal value to a color.
    /// </summary>
    /// <param name="hexValue">The hexadecimal value to convert.</param>
    /// <returns>Returns the color.</returns>
    public static Color? ConvertFromHex(this string? hexValue)
    {
        if (hexValue.IsStringNullOrEmpty())
            return null;

        return Color.TryParse(hexValue, out var color) ? color : null;
    }
}
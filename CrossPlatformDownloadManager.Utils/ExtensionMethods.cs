using System.Collections.ObjectModel;
using Avalonia.Platform;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Utils;

public static class ExtensionMethods
{
    public static bool IsNullOrEmpty(this string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value);
    }

    public static T? ConvertFromJson<T>(this string? json)
    {
        return json.IsNullOrEmpty() ? default : JsonConvert.DeserializeObject<T>(json!);
    }

    public static string ConvertToJson(this object? value)
    {
        return JsonConvert.SerializeObject(value);
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T>? items)
    {
        return items == null ? [] : new ObservableCollection<T>(items);
    }

    public static string ToFileSize(this double bytes)
    {
        switch (bytes)
        {
            case < 0:
                return string.Empty;

            case 0:
                return "0 KB";
        }

        var tb = bytes / Constants.TeraByte;
        if (tb > 1)
            return $"{tb:N2} TB";

        var gb = bytes / Constants.GigaByte;
        if (gb > 1)
            return $"{gb:N2} GB";

        var mb = bytes / Constants.MegaByte;
        if (mb > 1)
            return $"{mb:N2} MB";

        var kb = bytes / Constants.KiloByte;
        if (kb > 1)
            return $"{kb:N2} KB";

        return $"{bytes:N2} Byte" + (bytes > 1 ? "s" : "");
    }

    public static string ToFileSize(this double? bytes)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize();
    }

    public static string ToFileSize(this long bytes)
    {
        return ((double)bytes).ToFileSize();
    }

    public static string ToFileSize(this long? bytes)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize();
    }

    public static string ToFileSize(this float bytes)
    {
        return ((double)bytes).ToFileSize();
    }

    public static string ToFileSize(this float? bytes)
    {
        bytes ??= 0;
        return bytes.Value.ToFileSize();
    }

    public static bool CheckUrlValidation(this string? url)
    {
        if (url.IsNullOrEmpty())
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static T? OpenJsonAsset<T>(this Uri? uri)
    {
        if (uri == null)
            return default;

        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        reader.Close();
        stream.Close();

        return json.ConvertFromJson<T>();
    }

    public static string? GetFileName(this string? url)
    {
        if (url.IsNullOrEmpty())
            return null;

        url = url!.Replace('\\', '/').Trim();

        var uri = new Uri(url);
        var fileName = string.Empty;
        if (uri.IsFile)
            fileName = Path.GetFileName(uri.LocalPath);

        var tempBaseUri = new Uri("https://localhost/temp");
        if (fileName.IsNullOrEmpty())
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                uri = new Uri(tempBaseUri, url);

            fileName = Path.GetFileName(uri.LocalPath);
        }

        if (fileName.IsNullOrEmpty())
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

        if (fileName.IsNullOrEmpty())
            return fileName;

        if (fileName!.Contains('/'))
            fileName = fileName.Substring(fileName.LastIndexOf('/') + 1);

        if (fileName.Contains('?'))
            fileName = fileName.Substring(0, fileName.IndexOf('?'));

        return fileName;
    }

    public static bool HasFileExtension(this string? fileName)
    {
        if (fileName.IsNullOrEmpty())
            return false;

        return !Path.GetExtension(fileName!).IsNullOrEmpty();
    }

    public static string GetShortTime(this TimeSpan? time)
    {
        if (time == null)
            return string.Empty;

        if (time == TimeSpan.Zero)
            return "00 : 00";

        var seconds = time.Value.TotalSeconds;

        var hours = seconds / 3600;
        seconds %= 3600;

        var minutes = seconds / 60;
        seconds %= 60;

        return hours > 1 ? $"{hours:00} : {minutes:00} : {seconds:00}" : $"{minutes:00} : {seconds:00}";
    }

    public static T? DeepCopy<T>(this T? obj)
    {
        var json = obj.ConvertToJson();
        return json.ConvertFromJson<T>();
    }

    public static void UpdateList<T, TKey>(this List<T> oldList, List<T> newList, Func<T, TKey> keySelector) where TKey : notnull
    {
        // Create dictionaries for fast lookup
        var oldItemsByKey = oldList.ToDictionary(keySelector);
        var newItemsByKey = newList.ToDictionary(keySelector);

        // Find items to remove
        var itemsToRemove = oldItemsByKey.Keys.Except(newItemsByKey.Keys).Select(key => oldItemsByKey[key]).ToList();
        foreach (var item in itemsToRemove)
            oldList.Remove(item);

        // Update existing items or add new ones
        foreach (var newItem in newList)
        {
            // Get key from new item
            var key = keySelector(newItem);
            if (oldItemsByKey.TryGetValue(key, out var existingItem))
            {
                // Update existing item by replacing it
                var index = oldList.IndexOf(existingItem);
                oldList[index] = newItem;
            }
            else
            {
                // Add new item
                oldList.Add(newItem);
            }
        }
    }

    public static void UpdateCollection<T, TKey>(this ObservableCollection<T> oldCollection, ObservableCollection<T> newCollection, Func<T, TKey> keySelector) where TKey : notnull
    {
        // Create dictionaries for fast lookup
        var oldItemsByKey = oldCollection.ToDictionary(keySelector);
        var newItemsByKey = newCollection.ToDictionary(keySelector);

        // Find items to remove
        var itemsToRemove = oldItemsByKey.Keys.Except(newItemsByKey.Keys).Select(key => oldItemsByKey[key]).ToList();
        foreach (var item in itemsToRemove)
            oldCollection.Remove(item);

        // Update existing items or add new ones
        foreach (var newItem in newCollection)
        {
            // Get key from new item
            var key = keySelector(newItem);
            if (oldItemsByKey.TryGetValue(key, out var existingItem))
            {
                // Update existing item by replacing it
                var index = oldCollection.IndexOf(existingItem);
                oldCollection[index] = newItem;
            }
            else
            {
                // Add new item
                oldCollection.Add(newItem);
            }
        }
    }
}
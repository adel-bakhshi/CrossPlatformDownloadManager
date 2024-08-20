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

    public static T? DeserializeJson<T>(this string? json)
    {
        try
        {
            if (json.IsNullOrEmpty())
                return default;

            return JsonConvert.DeserializeObject<T>(json!);
        }
        catch
        {
            return default;
        }
    }

    public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T>? items)
    {
        return items == null ? new ObservableCollection<T>() : new ObservableCollection<T>(items);
    }

    public static string ToFileSize(this double bytes)
    {
        if (bytes <= 0)
            return string.Empty;

        var tb = bytes / Constants.TB;
        if (tb > 1)
            return $"{tb:N2} TB";

        var gb = bytes / Constants.GB;
        if (gb > 1)
            return $"{gb:N2} GB";

        var mb = bytes / Constants.MB;
        if (mb > 1)
            return $"{mb:N2} MB";

        var kb = bytes / Constants.KB;
        if (kb > 1)
            return $"{kb:N2} KB";

        return $"{bytes:N2} Byte" + (bytes > 1 ? "s" : "");
    }

    public static bool CheckUrlValidation(this string? url)
    {
        if (url.IsNullOrEmpty())
            return false;

        return Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
    }

    public static T? OpenJsonAsset<T>(this Uri? uri)
    {
        if (uri == null)
            return default;
        
        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return json.DeserializeJson<T>();
    }
}
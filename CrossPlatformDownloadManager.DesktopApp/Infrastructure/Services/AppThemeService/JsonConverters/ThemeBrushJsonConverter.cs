using System;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.JsonConverters;

/// <summary>
/// JSON converter for converting theme brush data to appropriate IThemeBrush implementations.
/// </summary>
public class ThemeBrushJsonConverter : JsonConverter
{
    /// <summary>
    /// Writes JSON representation of the object.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="serializer">The JSON serializer.</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reads JSON data and converts it to appropriate IThemeBrush implementation.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="objectType">The type of the object.</param>
    /// <param name="existingValue">The existing value of the object.</param>
    /// <param name="serializer">The JSON serializer.</param>
    /// <returns>Returns the deserialized IThemeBrush object.</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var brushModeValue = jsonObject.SelectToken("brushMode");
        if (brushModeValue == null)
            return null;

        if (!Enum.TryParse(brushModeValue.Value<byte>().ToString(), out ThemeBrushMode brushMode))
            return null;

        IThemeBrush themeBrush = brushMode switch
        {
            ThemeBrushMode.Solid => new ThemeSolidBrush(),
            ThemeBrushMode.Gradient => new ThemeGradientBrush(),
            _ => throw new JsonSerializationException("Theme data is not valid.")
        };

        serializer.Populate(jsonObject.CreateReader(), themeBrush);
        return themeBrush;
    }

    /// <summary>
    /// Determines whether this converter can convert the specified object type.
    /// </summary>
    /// <param name="objectType">The type of the object.</param>
    /// <returns>Returns true if this converter can convert the specified object type, otherwise false.</returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IThemeBrush);
    }
}
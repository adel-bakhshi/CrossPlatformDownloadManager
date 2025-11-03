using System;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

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
        Log.Debug("Reading JSON data for theme brush conversion...");

        var jsonObject = JObject.Load(reader);
        var brushModeValue = jsonObject.SelectToken("brushMode");
        if (brushModeValue == null)
        {
            Log.Warning("brushMode property not found in JSON data.");
            return null;
        }

        if (!Enum.TryParse(brushModeValue.Value<byte>().ToString(), out ThemeBrushMode brushMode))
        {
            Log.Warning("Invalid brush mode value: {BrushModeValue}", brushModeValue.Value<byte>());
            return null;
        }

        Log.Debug("Creating theme brush with mode: {BrushMode}", brushMode);

        IThemeBrush themeBrush = brushMode switch
        {
            ThemeBrushMode.Solid => new ThemeSolidBrush(),
            ThemeBrushMode.Gradient => new ThemeGradientBrush(),
            _ => throw new JsonSerializationException("Theme data is not valid.")
        };

        serializer.Populate(jsonObject.CreateReader(), themeBrush);

        Log.Debug("Theme brush created successfully: {BrushType}", themeBrush.GetType().Name);
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
using System;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.GradientBrush;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.Models.ThemeBrush.SolidBrush;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppThemeService.JsonConverters;

public class ThemeBrushJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

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

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IThemeBrush);
    }
}
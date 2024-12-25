using Avalonia;
using Avalonia.Controls;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomRadioContentButton : RadioButton
{
    #region Properties

    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty
        .Register<CustomRadioContentButton, string>(nameof(Title), defaultValue: string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<object?> InfoContentProperty = AvaloniaProperty
        .Register<CustomRadioContentButton, object?>(nameof(InfoContent), defaultValue: null);

    public object? InfoContent
    {
        get => GetValue(InfoContentProperty);
        set => SetValue(InfoContentProperty, value);
    }

    #endregion
}
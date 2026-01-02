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

    #endregion
}
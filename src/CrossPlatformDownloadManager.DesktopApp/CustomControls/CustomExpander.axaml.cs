using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomExpander : Expander
{
    #region Properties

    public static readonly StyledProperty<bool> ShowChevronProperty = AvaloniaProperty.Register<CustomExpander, bool>(
        nameof(ShowChevron), defaultValue: true);

    public bool ShowChevron
    {
        get => GetValue(ShowChevronProperty);
        set => SetValue(ShowChevronProperty, value);
    }

    #endregion
}
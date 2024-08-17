using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls;

public partial class ActionButton : UserControl
{
    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<ActionButton, Geometry?>(
            "IconData", defaultValue: null);

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<ActionButton, string>(
        "Text", defaultValue: string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<ICommand?> CommandProperty = AvaloniaProperty.Register<ActionButton, ICommand?>(
        "Command", defaultValue: null);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly StyledProperty<IBrush?> IconColorProperty = AvaloniaProperty.Register<ActionButton, IBrush?>(
        "IconColor", SolidColorBrush.Parse("#FFF"));

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public ActionButton()
    {
        InitializeComponent();
    }
}
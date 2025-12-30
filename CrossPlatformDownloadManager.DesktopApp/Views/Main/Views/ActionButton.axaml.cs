using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.Views.Main.Views;

public partial class ActionButton : UserControl
{
    #region Properties

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

    public static readonly StyledProperty<IBrush?> IconColorProperty = AvaloniaProperty.Register<ActionButton, IBrush?>(
        "IconColor", SolidColorBrush.Parse("#FFF"));

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly StyledProperty<FlyoutBase?> FlyoutProperty =
        AvaloniaProperty.Register<ActionButton, FlyoutBase?>(
            name: nameof(Flyout), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public FlyoutBase? Flyout
    {
        get => GetValue(FlyoutProperty);
        set => SetValue(FlyoutProperty, value);
    }

    #endregion

    #region Commands

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<ActionButton, ICommand?>(
            "Command", defaultValue: null);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ActionButton, object?>(
            "CommandParameter");

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    public ActionButton()
    {
        InitializeComponent();
    }
}
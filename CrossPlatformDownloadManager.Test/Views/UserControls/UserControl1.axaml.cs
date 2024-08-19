using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.Test.Views.UserControls;

public partial class UserControl1 : UserControl
{
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<UserControl1, string?>(
            "PlaceholderText");

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<UserControl1, IBrush?>(
            "PlaceholderForeground");

    public IBrush? PlaceholderForeground
    {
        get => GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }

    public static readonly StyledProperty<Geometry?> IconDataProperty =
        AvaloniaProperty.Register<UserControl1, Geometry?>(
            "IconData");

    public Geometry? IconData
    {
        get => GetValue(IconDataProperty);
        set => SetValue(IconDataProperty, value);
    }

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<UserControl1, double>(
        "IconSize", defaultValue: 16);

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly StyledProperty<IBrush?> IconColorProperty = AvaloniaProperty.Register<UserControl1, IBrush?>(
        "IconColor");

    public IBrush? IconColor
    {
        get => GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty = AvaloniaProperty.Register<UserControl1, IEnumerable?>(
        "ItemsSource");

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty = AvaloniaProperty.Register<UserControl1, IDataTemplate?>(
        "ItemTemplate");

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public static readonly StyledProperty<IDataTemplate?> SelectionBoxItemTemplateProperty = AvaloniaProperty.Register<UserControl1, IDataTemplate?>(
        "SelectionBoxItemTemplate");

    public IDataTemplate? SelectionBoxItemTemplate
    {
        get => GetValue(SelectionBoxItemTemplateProperty);
        set => SetValue(SelectionBoxItemTemplateProperty, value);
    }

    public static readonly StyledProperty<double> MaxDropDownHeightProperty = AvaloniaProperty.Register<UserControl1, double>(
        "MaxDropDownHeight");

    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }
    
    public UserControl1()
    {
        InitializeComponent();
    }
}
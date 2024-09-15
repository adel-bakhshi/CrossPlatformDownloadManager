using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.CustomControls;

public class CustomSelectBox : ListBox
{
    #region Properties

    public static readonly StyledProperty<double> ItemsSpacingProperty =
        AvaloniaProperty.Register<CustomSelectBox, double>(
            name: nameof(ItemsSpacing), defaultValue: 0);

    public double ItemsSpacing
    {
        get => GetValue(ItemsSpacingProperty);
        set => SetValue(ItemsSpacingProperty, value);
    }

    public static readonly StyledProperty<Thickness> ItemsPaddingProperty =
        AvaloniaProperty.Register<CustomSelectBox, Thickness>(
            name: nameof(ItemsPadding), defaultValue: new Thickness(10));

    public Thickness ItemsPadding
    {
        get => GetValue(ItemsPaddingProperty);
        set => SetValue(ItemsPaddingProperty, value);
    }

    public static readonly StyledProperty<double> ItemsMinWidthProperty =
        AvaloniaProperty.Register<CustomSelectBox, double>(
            name: nameof(ItemsMinWidth), defaultValue: 80);

    public double ItemsMinWidth
    {
        get => GetValue(ItemsMinWidthProperty);
        set => SetValue(ItemsMinWidthProperty, value);
    }

    public static readonly StyledProperty<IBrush?> ItemsBackgroundProperty =
        AvaloniaProperty.Register<CustomSelectBox, IBrush?>(
            name: nameof(ItemsBackground), defaultValue: null);

    public IBrush? ItemsBackground
    {
        get => GetValue(ItemsBackgroundProperty);
        set => SetValue(ItemsBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnHoverItemsBackgroundProperty =
        AvaloniaProperty.Register<CustomSelectBox, IBrush?>(
            name: nameof(OnHoverItemsBackground), defaultValue: null);

    public IBrush? OnHoverItemsBackground
    {
        get => GetValue(OnHoverItemsBackgroundProperty);
        set => SetValue(OnHoverItemsBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnFocusItemsBackgroundProperty =
        AvaloniaProperty.Register<CustomSelectBox, IBrush?>(
            name: nameof(OnFocusItemsBackground), defaultValue: null);

    public IBrush? OnFocusItemsBackground
    {
        get => GetValue(OnFocusItemsBackgroundProperty);
        set => SetValue(OnFocusItemsBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush?> OnSelectItemsBackgroundProperty =
        AvaloniaProperty.Register<CustomSelectBox, IBrush?>(
            name: nameof(OnSelectItemsBackground), defaultValue: null);

    public IBrush? OnSelectItemsBackground
    {
        get => GetValue(OnSelectItemsBackgroundProperty);
        set => SetValue(OnSelectItemsBackgroundProperty, value);
    }

    #endregion
}
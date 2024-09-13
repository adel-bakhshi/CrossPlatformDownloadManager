using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class ProxyView : UserControl
{
    #region Properties

    public static readonly StyledProperty<bool> DisableProxyProperty = AvaloniaProperty.Register<ProxyView, bool>(
        name: nameof(DisableProxy), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool DisableProxy
    {
        get => GetValue(DisableProxyProperty);
        set => SetValue(DisableProxyProperty, value);
    }

    public static readonly StyledProperty<bool> UseSystemProxySettingsProperty =
        AvaloniaProperty.Register<ProxyView, bool>(
            name: nameof(UseSystemProxySettings), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool UseSystemProxySettings
    {
        get => GetValue(UseSystemProxySettingsProperty);
        set => SetValue(UseSystemProxySettingsProperty, value);
    }

    public static readonly StyledProperty<bool> UseCustomProxyProperty =
        AvaloniaProperty.Register<ProxyView, bool>(
            name: nameof(UseCustomProxy), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool UseCustomProxy
    {
        get => GetValue(UseCustomProxyProperty);
        set => SetValue(UseCustomProxyProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<string>> ProxyTypesProperty =
        AvaloniaProperty.Register<ProxyView, ObservableCollection<string>>(
            name: nameof(ProxyTypes), defaultValue: [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableCollection<string> ProxyTypes
    {
        get => GetValue(ProxyTypesProperty);
        set => SetValue(ProxyTypesProperty, value);
    }

    public static readonly StyledProperty<string?> SelectedProxyTypeProperty =
        AvaloniaProperty.Register<ProxyView, string?>(
            name: nameof(SelectedProxyType), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? SelectedProxyType
    {
        get => GetValue(SelectedProxyTypeProperty);
        set => SetValue(SelectedProxyTypeProperty, value);
    }

    public static readonly StyledProperty<string?> HostProperty = AvaloniaProperty.Register<ProxyView, string?>(
        name: nameof(Host), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? Host
    {
        get => GetValue(HostProperty);
        set => SetValue(HostProperty, value);
    }

    public static readonly StyledProperty<string?> PortProperty = AvaloniaProperty.Register<ProxyView, string?>(
        name: nameof(Port), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? Port
    {
        get => GetValue(PortProperty);
        set => SetValue(PortProperty, value);
    }

    public static readonly StyledProperty<string?> UsernameProperty = AvaloniaProperty.Register<ProxyView, string?>(
        name: nameof(Username), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? Username
    {
        get => GetValue(UsernameProperty);
        set => SetValue(UsernameProperty, value);
    }

    public static readonly StyledProperty<string?> PasswordProperty = AvaloniaProperty.Register<ProxyView, string?>(
        name: nameof(Password), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? Password
    {
        get => GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    #endregion

    public ProxyView()
    {
        InitializeComponent();
    }
}
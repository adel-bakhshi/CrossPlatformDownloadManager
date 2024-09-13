using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class GeneralsView : UserControl
{
    #region Properties

    public static readonly StyledProperty<bool> StartOnSystemStartupProperty =
        AvaloniaProperty.Register<GeneralsView, bool>(
            name: nameof(StartOnSystemStartup), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool StartOnSystemStartup
    {
        get => GetValue(StartOnSystemStartupProperty);
        set => SetValue(StartOnSystemStartupProperty, value);
    }

    public static readonly StyledProperty<bool> UseBrowserExtensionProperty =
        AvaloniaProperty.Register<GeneralsView, bool>(
            name: nameof(UseBrowserExtension), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool UseBrowserExtension
    {
        get => GetValue(UseBrowserExtensionProperty);
        set => SetValue(UseBrowserExtensionProperty, value);
    }

    #endregion

    public GeneralsView()
    {
        InitializeComponent();
    }
}
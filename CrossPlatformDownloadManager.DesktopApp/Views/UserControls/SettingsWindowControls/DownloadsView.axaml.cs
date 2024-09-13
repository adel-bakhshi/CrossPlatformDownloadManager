using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class DownloadsView : UserControl
{
    #region Properties

    public static readonly StyledProperty<bool> ShowStartDownloadDialogProperty =
        AvaloniaProperty.Register<DownloadsView, bool>(
            name: nameof(ShowStartDownloadDialog), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool ShowStartDownloadDialog
    {
        get => GetValue(ShowStartDownloadDialogProperty);
        set => SetValue(ShowStartDownloadDialogProperty, value);
    }

    public static readonly StyledProperty<bool> ShowCompleteDownloadDialogProperty =
        AvaloniaProperty.Register<DownloadsView, bool>(
            name: nameof(ShowCompleteDownloadDialog), defaultValue: false, defaultBindingMode: BindingMode.TwoWay);

    public bool ShowCompleteDownloadDialog
    {
        get => GetValue(ShowCompleteDownloadDialogProperty);
        set => SetValue(ShowCompleteDownloadDialogProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<string>> DuplicateDownloadLinkActionsProperty =
        AvaloniaProperty.Register<DownloadsView, ObservableCollection<string>>(
            name: nameof(DuplicateDownloadLinkActions), defaultValue: [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableCollection<string> DuplicateDownloadLinkActions
    {
        get => GetValue(DuplicateDownloadLinkActionsProperty);
        set => SetValue(DuplicateDownloadLinkActionsProperty, value);
    }

    public static readonly StyledProperty<string?> SelectedDuplicateDownloadLinkActionProperty =
        AvaloniaProperty.Register<DownloadsView, string?>(
            name: nameof(SelectedDuplicateDownloadLinkAction), defaultValue: null,
            defaultBindingMode: BindingMode.TwoWay);

    public string? SelectedDuplicateDownloadLinkAction
    {
        get => GetValue(SelectedDuplicateDownloadLinkActionProperty);
        set => SetValue(SelectedDuplicateDownloadLinkActionProperty, value);
    }

    #endregion

    public DownloadsView()
    {
        InitializeComponent();
    }
}
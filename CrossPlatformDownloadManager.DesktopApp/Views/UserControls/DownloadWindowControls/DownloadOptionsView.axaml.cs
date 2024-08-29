using System;
using System.Collections;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.DownloadWindowControls;

public partial class DownloadOptionsView : UserControl
{
    #region Properties

    public static readonly StyledProperty<IEnumerable?> TurnOffModesDropDownItemsSourceProperty =
        AvaloniaProperty.Register<DownloadOptionsView, IEnumerable?>(
            "TurnOffModesDropDownItemsSource");

    public IEnumerable? TurnOffModesDropDownItemsSource
    {
        get => GetValue(TurnOffModesDropDownItemsSourceProperty);
        set => SetValue(TurnOffModesDropDownItemsSourceProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<DownloadOptionsViewEventArgs> OptionsStateChanged;

    #endregion

    public DownloadOptionsView()
    {
        InitializeComponent();

        this.Loaded += DownloadOptionsViewOnLoaded;
    }

    private void DownloadOptionsViewOnLoaded(object? sender, RoutedEventArgs e)
    {
        TurnOffModesComboBox.SelectedItem = Enumerable.FirstOrDefault<object?>(TurnOffModesComboBox.Items);
    }

    private void OpenFolderToggleSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ChangeOptionsState();
    }

    private void ExitProgramToggleSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ChangeOptionsState();
    }

    private void TurnOffComputerToggleSwitch_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        ChangeOptionsState();
    }

    private void TurnOffModesComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        ChangeOptionsState();
    }

    private void ChangeOptionsState()
    {
        var eventArgs = new DownloadOptionsViewEventArgs
        {
            OpenFolderAfterDownloadFinished = OpenFolderToggleSwitch.IsChecked ?? false,
            ExitProgramAfterDownloadFinished = ExitProgramToggleSwitch.IsChecked ?? false,
            TurnOffComputerAfterDownloadFinished = TurnOffComputerToggleSwitch.IsChecked ?? false,
            TurnOffComputerMode = TurnOffModesComboBox.SelectedItem as string,
        };

        OptionsStateChanged?.Invoke(this, eventArgs);
    }
}
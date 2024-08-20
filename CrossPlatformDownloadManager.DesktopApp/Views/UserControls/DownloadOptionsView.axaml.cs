using System;
using System.Collections;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls;

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

    #region Commands

    public static readonly StyledProperty<ICommand?> OptionsStateChangedCommandProperty =
        AvaloniaProperty.Register<DownloadOptionsView, ICommand?>(
            "OptionsStateChangedCommand");

    public ICommand? OptionsStateChangedCommand
    {
        get => GetValue(OptionsStateChangedCommandProperty);
        set => SetValue(OptionsStateChangedCommandProperty, value);
    }

    #endregion

    public DownloadOptionsView()
    {
        InitializeComponent();

        this.Loaded += DownloadOptionsViewOnLoaded;
    }

    private void DownloadOptionsViewOnLoaded(object? sender, RoutedEventArgs e)
    {
        TurnOffModesComboBox.SelectedItem = TurnOffModesComboBox.Items.FirstOrDefault();
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

        this.OptionsStateChanged?.Invoke(this, eventArgs);
        this.OptionsStateChangedCommand?.Execute(eventArgs);
    }
}
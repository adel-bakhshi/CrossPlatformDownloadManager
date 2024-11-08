using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;

namespace CrossPlatformDownloadManager.DesktopApp.Views;

public partial class DialogWindow : MyWindowBase<DialogWindowViewModel>
{
    public DialogWindow()
    {
        InitializeComponent();
    }

    private void WindowOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        BeginMoveDrag(e);
    }

    private void DialogButtonOnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button button)
                return;

            var content = button.Content?.ToString()?.ToLower();
            if (content.IsNullOrEmpty())
                return;

            var dialogResult = content switch
            {
                "ok" => DialogResult.Ok,
                "yes" => DialogResult.Yes,
                "no" => DialogResult.No,
                "cancel" => DialogResult.Cancel,
                _ => DialogResult.None
            };

            ViewModel?.SendDialogResult(this, dialogResult);
        }
        catch (Exception ex)
        {
            // TODO: Add error to log files
            App.Desktop?.Shutdown();
        }
    }
}
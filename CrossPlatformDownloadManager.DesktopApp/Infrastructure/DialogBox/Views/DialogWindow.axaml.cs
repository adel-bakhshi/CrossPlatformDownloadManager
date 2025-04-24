using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.ViewModels;
using CrossPlatformDownloadManager.Utils;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Views;

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
            if (content.IsStringNullOrEmpty())
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
            Log.Error(ex, "An error occurred while trying to save dialog result. Error message: {ErrorMessage}", ex.Message);
            App.Desktop?.Shutdown();
        }
    }
}
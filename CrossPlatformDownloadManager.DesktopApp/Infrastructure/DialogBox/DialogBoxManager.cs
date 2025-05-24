using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Views;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;

/// <summary>
/// Manager for the dialog box.
/// </summary>
public static class DialogBoxManager
{
    /// <summary>
    /// Shows an info dialog to the user with the given parameters.
    /// </summary>
    /// <param name="dialogHeader">The header of the dialog.</param>
    /// <param name="dialogMessage">The message of the dialog.</param>
    /// <param name="dialogButtons">The buttons of the dialog.</param>
    /// <param name="showCopyToClipboard">Whether to show the copy to clipboard button.</param>
    /// <param name="infoMessage">The additional message that shows in a tooltip.</param>
    /// <param name="useMainWindowAsOwner">Whether to use the main window as the owner of the dialog.</param>
    /// <returns>The result of the dialog.</returns>
    public static async Task<DialogResult> ShowInfoDialogAsync(string dialogHeader,
        string dialogMessage,
        DialogButtons dialogButtons,
        bool showCopyToClipboard = true,
        string? infoMessage = null,
        bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader,
            dialogMessage,
            dialogButtons,
            DialogType.Information,
            showCopyToClipboard: showCopyToClipboard,
            infoMessage: infoMessage,
            useMainWindowAsOwner: useMainWindowAsOwner);
    }

    /// <summary>
    /// Shows a warning dialog to the user with the given parameters.
    /// </summary>
    /// <param name="dialogHeader">The header of the dialog.</param>
    /// <param name="dialogMessage">The message of the dialog.</param>
    /// <param name="dialogButtons">The buttons of the dialog.</param>
    /// <param name="showCopyToClipboard">Whether to show the copy to clipboard button.</param>
    /// <param name="infoMessage">The additional message that shows in a tooltip.</param>
    /// <param name="useMainWindowAsOwner">Whether to use the main window as the owner of the dialog.</param>
    /// <returns>The result of the dialog.</returns>
    public static async Task<DialogResult> ShowWarningDialogAsync(string dialogHeader,
        string dialogMessage,
        DialogButtons dialogButtons,
        bool showCopyToClipboard = true,
        string? infoMessage = null,
        bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader,
            dialogMessage,
            dialogButtons,
            DialogType.Warning,
            showCopyToClipboard: showCopyToClipboard,
            infoMessage: infoMessage,
            useMainWindowAsOwner: useMainWindowAsOwner);
    }

    /// <summary>
    /// Shows a success dialog to the user with the given parameters.
    /// </summary>
    /// <param name="dialogHeader">The header of the dialog.</param>
    /// <param name="dialogMessage">The message of the dialog.</param>
    /// <param name="dialogButtons">The buttons of the dialog.</param>
    /// <param name="showCopyToClipboard">Whether to show the copy to clipboard button.</param>
    /// <param name="infoMessage">The additional message that shows in a tooltip.</param>
    /// <param name="useMainWindowAsOwner">Whether to use the main window as the owner of the dialog.</param>
    /// <returns>The result of the dialog.</returns>
    public static async Task<DialogResult> ShowSuccessDialogAsync(string dialogHeader,
        string dialogMessage,
        DialogButtons dialogButtons,
        bool showCopyToClipboard = true,
        string? infoMessage = null,
        bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader,
            dialogMessage,
            dialogButtons,
            DialogType.Success,
            showCopyToClipboard: showCopyToClipboard,
            infoMessage: infoMessage,
            useMainWindowAsOwner: useMainWindowAsOwner);
    }

    /// <summary>
    /// Shows a danger dialog to the user with the given parameters.
    /// </summary>
    /// <param name="dialogHeader">The header of the dialog.</param>
    /// <param name="dialogMessage">The message of the dialog.</param>
    /// <param name="dialogButtons">The buttons of the dialog.</param>
    /// <param name="showCopyToClipboard">Whether to show the copy to clipboard button.</param>
    /// <param name="infoMessage">The additional message that shows in a tooltip.</param>
    /// <param name="useMainWindowAsOwner">Whether to use the main window as the owner of the dialog.</param>
    /// <returns>The result of the dialog.</returns>
    public static async Task<DialogResult> ShowDangerDialogAsync(string dialogHeader,
        string dialogMessage,
        DialogButtons dialogButtons,
        bool showCopyToClipboard = true,
        string? infoMessage = null,
        bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader,
            dialogMessage,
            dialogButtons,
            DialogType.Danger,
            showCopyToClipboard: showCopyToClipboard,
            infoMessage: infoMessage,
            useMainWindowAsOwner: useMainWindowAsOwner);
    }

    /// <summary>
    /// Shows an error dialog to the user with the given parameters.
    /// </summary>
    /// <param name="exception">The exception that caused the error.</param>
    /// <param name="useMainWindowAsOwner">Whether to use the main window as the owner of the dialog.</param>
    /// <returns>The result of the dialog.</returns>
    public static async Task<DialogResult> ShowErrorDialogAsync(Exception exception, bool useMainWindowAsOwner = false)
    {
        const string dialogHeader = "Error";
        const string dialogMessage = "We ran into an unexpected issue. Please give it another try. If the problem continues, feel free to report it to us so we can help.";

        return await ShowDangerDialogAsync(dialogHeader,
            dialogMessage,
            DialogButtons.Ok,
            showCopyToClipboard: true,
            infoMessage: exception.Message,
            useMainWindowAsOwner: useMainWindowAsOwner);
    }

    #region Helpers

    /// <summary>
    /// Shows a dialog to the user with the given parameters.
    /// </summary>
    /// <param name="dialogHeader">The header of the dialog.</param>
    /// <param name="dialogMessage">The message of the dialog.</param>
    /// <param name="dialogButtons">The buttons of the dialog.</param>
    /// <param name="dialogType">The type of the dialog.</param>
    /// <param name="showCopyToClipboard">Whether to show the copy to clipboard button.</param>
    /// <param name="infoMessage">The additional message that shows in a tooltip.</param>
    /// <param name="useMainWindowAsOwner">Whether to use the main window as the owner of the dialog.</param>
    /// <returns>The result of the dialog.</returns>
    private static async Task<DialogResult> ShowDialogAsync(string dialogHeader,
        string dialogMessage,
        DialogButtons dialogButtons,
        DialogType dialogType,
        bool showCopyToClipboard = true,
        string? infoMessage = null,
        bool useMainWindowAsOwner = false)
    {
        // Run the following code on the UI thread to avoid any cross-thread exceptions
        // If we use the UI thread to show the dialog window, we can show on every thread without any issues
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            // Try to find the owner of the dialog box window
            var owner = useMainWindowAsOwner ? App.Desktop?.MainWindow : App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
            if (App.Desktop?.MainWindow != null && owner == App.Desktop.MainWindow && !App.Desktop.MainWindow.IsVisible)
                owner = App.Desktop.Windows.OfType<ManagerWindow>().FirstOrDefault();

            // Check if the owner is invisible, then set it to null
            // We don't want an owner that is not visible
            if (owner is { IsVisible: false })
                owner = null;

            // Try to find app service
            var appService = Application.Current?.GetServiceProvider().GetService<IAppService?>();
            if (appService == null)
                return DialogResult.None;

            // Create view model for dialog window
            var viewModel = new DialogWindowViewModel(appService)
            {
                DialogHeader = dialogHeader,
                DialogMessage = dialogMessage,
                DialogButtons = dialogButtons,
                DialogType = dialogType,
                CopyToClipboardButtonIsVisible = showCopyToClipboard,
                InfoMessage = infoMessage
            };

            // Create dialog window and set the view model as data context
            var window = new DialogWindow { DataContext = viewModel };
            // If owner is not null, show dialog as modal and wait to close
            if (owner != null)
            {
                await window.ShowDialog(owner);
            }
            // Otherwise, show the dialog normally and wait for the dialog window to close
            else
            {
                // Create a task completion source to handle the showing of the dialog
                var dialogCompletionSource = new TaskCompletionSource<bool>();
                // Dialog window must be on top of the other windows
                window.Topmost = true;
                // Listen for the closed event of the dialog window
                window.Closed += (_, _) =>
                {
                    try
                    {
                        // Try to set the result of the task completion source
                        dialogCompletionSource.TrySetResult(true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while closing the dialog window.");
                        dialogCompletionSource.TrySetException(ex);
                    }
                };

                // Show the window
                window.Show();
                // Wait to close the dialog window
                await dialogCompletionSource.Task;
            }

            // Return the result of the dialog
            return viewModel.DialogResult;
        });
    }

    #endregion
}
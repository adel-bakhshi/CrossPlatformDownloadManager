using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Views;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;

public static class DialogBoxManager
{
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

    private static async Task<DialogResult> ShowDialogAsync(string dialogHeader,
        string dialogMessage,
        DialogButtons dialogButtons,
        DialogType dialogType,
        bool showCopyToClipboard = true,
        string? infoMessage = null,
        bool useMainWindowAsOwner = false)
    {
        var owner = useMainWindowAsOwner ? App.Desktop?.MainWindow : App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
        if (App.Desktop?.MainWindow != null && owner == App.Desktop.MainWindow && !App.Desktop.MainWindow.IsVisible)
            owner = App.Desktop.Windows.OfType<ManagerWindow>().FirstOrDefault();

        if (owner is { IsVisible: false })
            owner = null;

        var serviceProvider = Application.Current?.GetServiceProvider();
        var appService = serviceProvider?.GetService<IAppService>();
        if (appService == null)
            return DialogResult.None;

        var vm = new DialogWindowViewModel(appService)
        {
            DialogHeader = dialogHeader,
            DialogMessage = dialogMessage,
            DialogButtons = dialogButtons,
            DialogType = dialogType,
            CopyToClipboardButtonIsVisible = showCopyToClipboard,
            InfoMessage = infoMessage
        };

        var window = new DialogWindow { DataContext = vm };
        if (owner != null)
        {
            await window.ShowDialog(owner);
        }
        else
        {
            var isDialogWindowClosed = false;
            window.Topmost = true;
            window.Closed += (_, _) => isDialogWindowClosed = true;
            window.Show();

            while (!isDialogWindowClosed)
                await Task.Delay(100);
        }

        return vm.DialogResult;
    }

    #endregion
}
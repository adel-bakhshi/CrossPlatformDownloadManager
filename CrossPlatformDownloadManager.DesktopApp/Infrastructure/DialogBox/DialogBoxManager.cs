using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Views;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using Microsoft.Extensions.DependencyInjection;
using RolandK.AvaloniaExtensions.DependencyInjection;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;

public static class DialogBoxManager
{
    public static async Task<DialogResult> ShowInfoDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Information, useMainWindowAsOwner);
    }

    public static async Task<DialogResult> ShowWarningDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Warning, useMainWindowAsOwner);
    }

    public static async Task<DialogResult> ShowSuccessDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Success, useMainWindowAsOwner);
    }

    public static async Task<DialogResult> ShowDangerDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Danger, useMainWindowAsOwner);
    }

    public static async Task<DialogResult> ShowErrorDialogAsync(Exception exception, bool useMainWindowAsOwner = false)
    {
        const string dialogHeader = "Error";
        var dialogMessage = $"An error occurred: {exception.Message}";
        return await ShowDangerDialogAsync(dialogHeader, dialogMessage, DialogButtons.Ok, useMainWindowAsOwner);
    }

    #region Helpers

    private static async Task<DialogResult> ShowDialogAsync(string dialogHeader, string dialogMessage, DialogButtons dialogButtons, DialogType dialogType,
        bool useMainWindowAsOwner = false)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = useMainWindowAsOwner ? App.Desktop?.MainWindow : App.Desktop?.Windows.FirstOrDefault(w => w.IsFocused) ?? App.Desktop?.MainWindow;
            if (owner == null)
                return DialogResult.None;

            var serviceProvider = Application.Current?.GetServiceProvider();
            var appService = serviceProvider?.GetService<IAppService>();
            if (appService == null)
                return DialogResult.None;

            var vm = new DialogWindowViewModel(appService)
            {
                DialogHeader = dialogHeader,
                DialogMessage = dialogMessage,
                DialogButtons = dialogButtons,
                DialogType = dialogType
            };

            var window = new DialogWindow { DataContext = vm };
            await window.ShowDialog(owner);

            return vm.DialogResult;
        });
    }

    #endregion
}
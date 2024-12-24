using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public abstract class ViewModelBase : ReactiveObject
{
    #region Properties

    protected IAppService AppService { get; }

    #endregion

    protected ViewModelBase(IAppService appService)
    {
        AppService = appService;
        AppService.DownloadFileService.DataChanged += DownloadFileServiceOnDataChanged;
        AppService.DownloadFileService.ErrorOccurred += DownloadFileServiceOnErrorOccurred;
        AppService.DownloadQueueService.DataChanged += DownloadQueueServiceOnDataChanged;
        AppService.SettingsService.DataChanged += SettingsServiceOnDataChanged;
    }

    public async Task<DialogResult> ShowDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, DialogType dialogType, bool useMainWindowAsOwner = false)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var owner = useMainWindowAsOwner ? App.Desktop?.MainWindow : App.Desktop?.Windows.FirstOrDefault(w => w.IsActive) ?? App.Desktop?.MainWindow;
            if (owner == null)
                return DialogResult.None;

            var vm = new DialogWindowViewModel(AppService)
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

    public async Task<DialogResult> ShowInfoDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Information, useMainWindowAsOwner);
    }

    public async Task<DialogResult> ShowWarningDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Warning, useMainWindowAsOwner);
    }

    public async Task<DialogResult> ShowSuccessDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Success, useMainWindowAsOwner);
    }

    public async Task<DialogResult> ShowDangerDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, bool useMainWindowAsOwner = false)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Danger, useMainWindowAsOwner);
    }

    public async Task<DialogResult> ShowErrorDialogAsync(Exception exception, bool useMainWindowAsOwner = false)
    {
        const string dialogHeader = "Error";
        var dialogMessage = $"An error occurred: {exception.Message}";
        return await ShowDangerDialogAsync(dialogHeader, dialogMessage, DialogButtons.Ok, useMainWindowAsOwner);
    }

    #region Virtual Methods

    protected virtual void OnDownloadFileServiceDataChanged()
    {
    }

    protected virtual void OnDownloadFileServiceErrorOccured(DownloadFileErrorEventArgs e)
    {
    }

    protected virtual void OnDownloadQueueServiceDataChanged()
    {
    }

    protected virtual void OnSettingsServiceDataChanged()
    {
    }

    #endregion

    #region Helpers

    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e) => OnDownloadFileServiceDataChanged();

    private void DownloadFileServiceOnErrorOccurred(object? sender, DownloadFileErrorEventArgs e) => OnDownloadFileServiceErrorOccured(e);

    private void DownloadQueueServiceOnDataChanged(object? sender, EventArgs e) => OnDownloadQueueServiceDataChanged();

    private void SettingsServiceOnDataChanged(object? sender, EventArgs e) => OnSettingsServiceDataChanged();

    #endregion
}
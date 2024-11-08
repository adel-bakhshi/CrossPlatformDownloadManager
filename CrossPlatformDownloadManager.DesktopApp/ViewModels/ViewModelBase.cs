using System;
using System.Linq;
using System.Threading.Tasks;
using CrossPlatformDownloadManager.Data.Services.AppService;
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
        AppService.DownloadQueueService.DataChanged += DownloadQueueServiceOnDataChanged;
    }

    public async Task<DialogResult> ShowDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons, DialogType dialogType)
    {
        var owner = App.Desktop?.Windows.FirstOrDefault(w => w.IsActive);
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
    }

    public async Task<DialogResult> ShowInfoDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Information);
    }

    public async Task<DialogResult> ShowWarningDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Warning);
    }

    public async Task<DialogResult> ShowSuccessDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Success);
    }

    public async Task<DialogResult> ShowDangerDialogAsync(string dialogHeader, string dialogMessage,
        DialogButtons dialogButtons)
    {
        return await ShowDialogAsync(dialogHeader, dialogMessage, dialogButtons, DialogType.Danger);
    }

    public async Task<DialogResult> ShowErrorDialogAsync(Exception exception)
    {
        const string dialogHeader = "Error";
        var dialogMessage = $"An error occurred: {exception.Message}";
        return await ShowDangerDialogAsync(dialogHeader, dialogMessage, DialogButtons.Ok);
    }

    #region Virtual Methods

    protected virtual void OnDownloadFileServiceDataChanged()
    {
    }

    protected virtual void OnDownloadQueueServiceDataChanged()
    {
    }

    #endregion

    #region Helpers

    private void DownloadFileServiceOnDataChanged(object? sender, EventArgs e) =>
        OnDownloadFileServiceDataChanged();

    private void DownloadQueueServiceOnDataChanged(object? sender, EventArgs e) =>
        OnDownloadQueueServiceDataChanged();

    #endregion
}
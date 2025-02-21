using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.DownloadWindowViewModels;

public class DownloadStatusViewModel : ViewModelBase
{
    #region Private Fields

    private DownloadFileViewModel? _downloadFile;
    private string _resumeCapabilityState = "Checking...";

    #endregion

    #region Properties

    public DownloadFileViewModel? DownloadFile
    {
        get => _downloadFile;
        set => this.RaiseAndSetIfChanged(ref _downloadFile, value);
    }

    public string ResumeCapabilityState
    {
        get => _resumeCapabilityState;
        set => this.RaiseAndSetIfChanged(ref _resumeCapabilityState, value);
    }

    #endregion

    #region Commands

    public ICommand OpenSaveLocationCommand { get; }

    #endregion

    public DownloadStatusViewModel(IAppService appService, DownloadFileViewModel downloadFile) : base(appService)
    {
        DownloadFile = downloadFile;
        DownloadFile.PropertyChanged += DownloadFileOnPropertyChanged;

        OpenSaveLocationCommand = ReactiveCommand.CreateFromTask(OpenSaveLocationAsync);
    }

    public void RemoveEventHandlers()
    {
        if (DownloadFile != null)
            DownloadFile.PropertyChanged -= DownloadFileOnPropertyChanged;
    }

    private void DownloadFileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.Equals(nameof(DownloadFile.CanResumeDownload)) != true || DownloadFile?.CanResumeDownload == null)
            return;

        ResumeCapabilityState = DownloadFile!.CanResumeDownload == true ? "Yes" : "No";
    }

    private async Task OpenSaveLocationAsync()
    {
        try
        {
            if (DownloadFile?.SaveLocation.IsNullOrEmpty() != false)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open folder",
                    "The folder you are trying to access is not available. It may have been removed or relocated.",
                    DialogButtons.Ok);

                return;
            }

            if (!Directory.Exists(DownloadFile.SaveLocation))
            {
                await DialogBoxManager.ShowInfoDialogAsync("Open folder",
                    "The folder you are trying to access is not available. It may have been removed or relocated.",
                    DialogButtons.Ok);

                return;
            }

            PlatformSpecificManager.OpenContainingFolderAndSelectFile(DownloadFile.SaveLocation);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to open the folder.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
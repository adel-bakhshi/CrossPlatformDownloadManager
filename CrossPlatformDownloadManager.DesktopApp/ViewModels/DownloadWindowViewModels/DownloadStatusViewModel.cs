using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

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
                await ShowInfoDialogAsync("Open folder",
                    "The folder you are trying to access is not available. It may have been removed or relocated.",
                    DialogButtons.Ok);

                return;
            }

            if (!Directory.Exists(DownloadFile.SaveLocation))
            {
                await ShowInfoDialogAsync("Open folder",
                    "The folder you are trying to access is not available. It may have been removed or relocated.",
                    DialogButtons.Ok);

                return;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = DownloadFile.SaveLocation,
                    UseShellExecute = true
                }
            };

            process.Start();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
    }
}
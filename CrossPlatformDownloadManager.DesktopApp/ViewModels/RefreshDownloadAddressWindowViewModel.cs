using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class RefreshDownloadAddressWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly DownloadFileViewModel? _downloadFile;

    private string _newAddress = string.Empty;

    #endregion

    #region Properties

    public string NewAddress
    {
        get => _newAddress;
        set => this.RaiseAndSetIfChanged(ref _newAddress, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    
    public ICommand CancelCommand { get; }

    #endregion

    public RefreshDownloadAddressWindowViewModel(IAppService appService, DownloadFileViewModel? downloadFile) : base(appService)
    {
        _downloadFile = downloadFile;

        NewAddress = _downloadFile?.Url ?? string.Empty;
        
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            NewAddress = NewAddress.Replace("\\", "/").Trim();
            if (owner == null || NewAddress.IsNullOrEmpty() || !NewAddress.CheckUrlValidation())
                return;
            
            var downloadFile = AppService
                .DownloadFileService
                .DownloadFiles
                .FirstOrDefault(df => df.Id == _downloadFile?.Id);

            if (downloadFile == null || downloadFile.IsDownloading || downloadFile.IsCompleted)
                return;
            
            // Send a HEAD request to get the headers only
            var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Head, NewAddress);
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve URL: {response.StatusCode}");

            var newFileSize = response.Content.Headers.ContentLength ?? 0;
            var oldFileSize = (long)(downloadFile.Size ?? 0);
            if (newFileSize != oldFileSize)
                return;

            downloadFile.Url = NewAddress;

            await AppService
                .DownloadFileService
                .UpdateDownloadFileAsync(downloadFile);
            
            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to refresh the download address.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task CancelAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (!NewAddress.IsNullOrEmpty() && NewAddress.CheckUrlValidation())
            {
                NewAddress = NewAddress.Replace("\\", "/").Trim();

                var downloadFile = AppService
                    .DownloadFileService
                    .DownloadFiles
                    .FirstOrDefault(df => df.Id == _downloadFile?.Id);

                if (downloadFile != null && !downloadFile.Url.IsNullOrEmpty() && !downloadFile.Url!.Equals(NewAddress))
                {
                    var result = await DialogBoxManager.ShowWarningDialogAsync(
                        "Refresh Download Address",
                        "Are you sure you want to cancel the refresh of the download address without saving the changes?",
                        DialogButtons.YesNoCancel);

                    switch (result)
                    {
                        case DialogResult.No:
                        {
                            await SaveAsync(owner);
                            return;
                        }

                        case DialogResult.Cancel:
                        {
                            return;
                        }
                    }
                }
            }
            
            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to close the window.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }
}
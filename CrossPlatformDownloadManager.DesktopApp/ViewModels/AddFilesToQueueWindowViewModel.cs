using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddFilesToQueueWindowViewModel : ViewModelBase
{
    #region Private Fields

    private int? _downloadQueueId;
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    #endregion

    #region Properties

    public int? DownloadQueueId
    {
        private get => _downloadQueueId;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadQueueId, value);
            DownloadFiles = GetDownloadFiles();
        }
    }

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    public List<DownloadFileViewModel> SelectedDownloadFiles { get; set; } = [];

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    #endregion

    public AddFilesToQueueWindowViewModel(IAppService appService) : base(appService)
    {
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (SelectedDownloadFiles.Count == 0)
                return;

            owner.Close(SelectedDownloadFiles);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to close the window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private ObservableCollection<DownloadFileViewModel> GetDownloadFiles()
    {
        var downloadFiles = AppService
            .DownloadFileService
            .DownloadFiles
            .Where(df => (DownloadQueueId == 0 || (DownloadQueueId > 0 && (df.DownloadQueueId ?? 0) != DownloadQueueId)) && !df.IsCompleted)
            .ToObservableCollection();

        return downloadFiles;
    }
}
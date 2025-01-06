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
    #region Properties

    private int? _downloadQueueId;

    public int? DownloadQueueId
    {
        private get => _downloadQueueId;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadQueueId, value);
            DownloadFiles = GetDownloadFiles();
        }
    }

    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

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
            Log.Error(ex, "An error occured while trying to close the window.");
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private ObservableCollection<DownloadFileViewModel> GetDownloadFiles()
    {
        var downloadFiles = AppService
            .DownloadFileService
            .DownloadFiles
            .Where(df => (df.DownloadQueueId ?? 0) != DownloadQueueId)
            .ToObservableCollection();

        return downloadFiles;
    }
}
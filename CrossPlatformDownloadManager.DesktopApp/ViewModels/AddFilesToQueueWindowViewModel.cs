using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

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

    private List<DownloadFileViewModel> _selectedDownloadFiles = [];

    public List<DownloadFileViewModel> SelectedDownloadFiles
    {
        get => _selectedDownloadFiles;
        set => this.RaiseAndSetIfChanged(ref _selectedDownloadFiles, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    #endregion

    public AddFilesToQueueWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(
        unitOfWork, downloadFileService)
    {
        SaveCommand = ReactiveCommand.Create<Window?>(Save);
    }

    private void Save(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;
            
            if (!SelectedDownloadFiles.Any())
                return;
            
            owner.Close(SelectedDownloadFiles);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private ObservableCollection<DownloadFileViewModel> GetDownloadFiles()
    {
        var downloadFiles = DownloadFileService.DownloadFiles
            .Where(df => (df.DownloadQueueId ?? 0) != DownloadQueueId)
            .ToObservableCollection();

        return downloadFiles;
    }
}
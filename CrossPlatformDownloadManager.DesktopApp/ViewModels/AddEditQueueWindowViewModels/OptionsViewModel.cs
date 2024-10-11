using System.Windows.Input;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.AddEditQueueWindowViewModels;

public class OptionsViewModel : ViewModelBase
{
    #region Properties

    private DownloadQueueViewModel _downloadQueue;

    public DownloadQueueViewModel DownloadQueue
    {
        get => _downloadQueue;
        set => this.RaiseAndSetIfChanged(ref _downloadQueue, value);
    }

    #endregion

    #region Commands

    public ICommand? ChangeStartDownloadDateCommand { get; }

    #endregion
    
    public OptionsViewModel(IAppService appService) : base(appService)
    {
        DownloadQueue = new DownloadQueueViewModel();
        
        ChangeStartDownloadDateCommand = ReactiveCommand.Create<string?>(ChangeStartDownloadDate);
    }
    
    private void ChangeStartDownloadDate(string? value)
    {
        if (value.IsNullOrEmpty())
            return;

        DownloadQueue.IsDaily = value!.Equals("Daily");
    }
}
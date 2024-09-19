using AutoMapper;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class GeneralsViewModel : ViewModelBase
{
    #region Properties

    private bool _startOnSystemStartup;

    public bool StartOnSystemStartup
    {
        get => _startOnSystemStartup;
        set => this.RaiseAndSetIfChanged(ref _startOnSystemStartup, value);
    }

    private bool _useBrowserExtension;

    public bool UseBrowserExtension
    {
        get => _useBrowserExtension;
        set => this.RaiseAndSetIfChanged(ref _useBrowserExtension, value);
    }

    private bool _darkMode;

    public bool DarkMode
    {
        get => _darkMode;
        set => this.RaiseAndSetIfChanged(ref _darkMode, value);
    }

    #endregion

    public GeneralsViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService, IMapper mapper) : base(
        unitOfWork, downloadFileService, mapper)
    {
    }
}
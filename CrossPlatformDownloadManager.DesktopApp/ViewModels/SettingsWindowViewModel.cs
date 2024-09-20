using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    #region Properties

    private ObservableCollection<string> _tabItems = [];

    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    private string? _selectedTabItem;

    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
    }

    private GeneralsViewModel? _generalsViewModel;

    public GeneralsViewModel? GeneralsViewModel
    {
        get => _generalsViewModel;
        set => this.RaiseAndSetIfChanged(ref _generalsViewModel, value);
    }

    private FileTypesViewModel? _fileTypesViewModel;

    public FileTypesViewModel? FileTypesViewModel
    {
        get => _fileTypesViewModel;
        set => this.RaiseAndSetIfChanged(ref _fileTypesViewModel, value);
    }

    private SaveLocationsViewModel? _saveLocationsViewModel;

    public SaveLocationsViewModel? SaveLocationsViewModel
    {
        get => _saveLocationsViewModel;
        set => this.RaiseAndSetIfChanged(ref _saveLocationsViewModel, value);
    }

    private DownloadsViewModel? _downloadsViewModel;

    public DownloadsViewModel? DownloadsViewModel
    {
        get => _downloadsViewModel;
        set => this.RaiseAndSetIfChanged(ref _downloadsViewModel, value);
    }

    private ProxyViewModel? _proxyViewModel;

    public ProxyViewModel? ProxyViewModel
    {
        get => _proxyViewModel;
        set => this.RaiseAndSetIfChanged(ref _proxyViewModel, value);
    }

    private NotificationsViewModel? _notificationsViewModel;

    public NotificationsViewModel? NotificationsViewModel
    {
        get => _notificationsViewModel;
        set => this.RaiseAndSetIfChanged(ref _notificationsViewModel, value);
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public SettingsWindowViewModel(IAppService appService) : base(appService)
    {
        GeneralsViewModel = new GeneralsViewModel(appService);
        FileTypesViewModel = new FileTypesViewModel(appService);
        SaveLocationsViewModel = new SaveLocationsViewModel(appService);
        DownloadsViewModel = new DownloadsViewModel(appService);
        ProxyViewModel = new ProxyViewModel(appService);
        NotificationsViewModel = new NotificationsViewModel(appService);

        GenerateTabs();

        SaveCommand = ReactiveCommand.Create<Window?>(Save);
        CancelCommand = ReactiveCommand.Create<Window?>(Cancel);
    }

    private void GenerateTabs()
    {
        var tabItems = new List<string>
        {
            "Generals",
            "File Types",
            "Save Locations",
            "Downloads",
            "Proxy",
            "Notifications",
        };

        TabItems = tabItems.ToObservableCollection();
        SelectedTabItem = TabItems.FirstOrDefault();
    }

    private void Save(Window? owner)
    {
        throw new System.NotImplementedException();
    }

    private void Cancel(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;
            
            owner.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
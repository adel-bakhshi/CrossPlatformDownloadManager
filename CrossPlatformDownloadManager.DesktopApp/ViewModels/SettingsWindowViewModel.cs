using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    #region Private Fields

    private string? _selectedTabItem;
    private GeneralsViewModel? _generalsViewModel;
    private FileTypesViewModel? _fileTypesViewModel;
    private SaveLocationsViewModel? _saveLocationsViewModel;
    private DownloadsViewModel? _downloadsViewModel;
    private ProxyViewModel? _proxyViewModel;
    private NotificationsViewModel? _notificationsViewModel;

    #endregion

    #region Properties

    private ObservableCollection<string> _tabItems = [];

    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set => this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
    }

    public GeneralsViewModel? GeneralsViewModel
    {
        get => _generalsViewModel;
        set => this.RaiseAndSetIfChanged(ref _generalsViewModel, value);
    }

    public FileTypesViewModel? FileTypesViewModel
    {
        get => _fileTypesViewModel;
        set => this.RaiseAndSetIfChanged(ref _fileTypesViewModel, value);
    }

    public SaveLocationsViewModel? SaveLocationsViewModel
    {
        get => _saveLocationsViewModel;
        set => this.RaiseAndSetIfChanged(ref _saveLocationsViewModel, value);
    }

    public DownloadsViewModel? DownloadsViewModel
    {
        get => _downloadsViewModel;
        set => this.RaiseAndSetIfChanged(ref _downloadsViewModel, value);
    }

    public ProxyViewModel? ProxyViewModel
    {
        get => _proxyViewModel;
        set => this.RaiseAndSetIfChanged(ref _proxyViewModel, value);
    }

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

        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
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

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            // Check required data before saving
            if (owner == null ||
                GeneralsViewModel == null ||
                FileTypesViewModel == null ||
                SaveLocationsViewModel == null ||
                DownloadsViewModel == null ||
                ProxyViewModel == null ||
                NotificationsViewModel == null)
            {
                throw new InvalidOperationException("An error occured while trying to save settings.");
            }

            // Save proxy settings
            switch (ProxyViewModel)
            {
                case { DisableProxy: true }:
                {
                    await AppService
                        .SettingsService
                        .DisableProxyAsync();
            
                    break;
                }
            
                case { UseSystemProxySettings: true }:
                {
                    await AppService
                        .SettingsService
                        .UseSystemProxySettingsAsync();
            
                    break;
                }
            
                case { UseCustomProxy: true }:
                {
                    var activeProxy = ProxyViewModel
                        .AvailableProxies
                        .FirstOrDefault(p => p.IsActive);
                    
                    if (activeProxy == null)
                        break;

                    await AppService
                        .SettingsService
                        .UseCustomProxyAsync(activeProxy);
            
                    break;
                }
            
                default:
                    throw new InvalidOperationException("An error occured while trying to save settings.");
            }

            owner.Close();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
        }
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
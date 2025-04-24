using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

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
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
            
            switch (SelectedTabItem)
            {
                case "File Types":
                {
                    // Load file extensions when selected tab changed
                    FileTypesViewModel?.LoadFileExtensionsAsync();
                    break;
                }
                
                case "Save Locations":
                {
                    // Load file extensions when selected tab changed
                    SaveLocationsViewModel?.LoadFileExtensionsAsync();
                    break;
                }

                case "Proxy":
                {
                    // Load available proxies when selected tab changed
                    ProxyViewModel?.LoadAvailableProxiesAsync();
                    break;
                }
            }
        }
    }

    public GeneralsViewModel? GeneralsViewModel
    {
        get => _generalsViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _generalsViewModel, value);
            this.RaisePropertyChanged(nameof(GeneralsView));
        }
    }

    public FileTypesViewModel? FileTypesViewModel
    {
        get => _fileTypesViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _fileTypesViewModel, value);
            this.RaisePropertyChanged(nameof(FileTypesView));
        }
    }

    public SaveLocationsViewModel? SaveLocationsViewModel
    {
        get => _saveLocationsViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _saveLocationsViewModel, value);
            this.RaisePropertyChanged(nameof(SaveLocationsView));
        }
    }

    public DownloadsViewModel? DownloadsViewModel
    {
        get => _downloadsViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _downloadsViewModel, value);
            this.RaisePropertyChanged(nameof(DownloadsView));
        }
    }

    public ProxyViewModel? ProxyViewModel
    {
        get => _proxyViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _proxyViewModel, value);
            this.RaisePropertyChanged(nameof(ProxyView));
        }
    }

    public NotificationsViewModel? NotificationsViewModel
    {
        get => _notificationsViewModel;
        set
        {
            this.RaiseAndSetIfChanged(ref _notificationsViewModel, value);
            this.RaisePropertyChanged(nameof(NotificationsView));
        }
    }

    public GeneralsView GeneralsView => new() { DataContext = GeneralsViewModel };
    public FileTypesView FileTypesView => new() { DataContext = FileTypesViewModel };
    public SaveLocationsView SaveLocationsView => new() { DataContext = SaveLocationsViewModel };
    public DownloadsView DownloadsView => new() { DataContext = DownloadsViewModel };
    public ProxyView ProxyView => new() { DataContext = ProxyViewModel };
    public NotificationsView NotificationsView => new() { DataContext = NotificationsViewModel };

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
                throw new InvalidOperationException("An error occurred while trying to save settings.");
            }
            
            // Validate font
            if (GeneralsViewModel.SelectedFont.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Font Not Specified",
                    "Please choose a valid font and try again.",
                    DialogButtons.Ok);
                
                return;
            }

            // Validate save locations settings
            if (SaveLocationsViewModel.DisableCategories)
            {
                // Check that the global save location is valid
                if (SaveLocationsViewModel.GlobalSaveDirectory.IsStringNullOrEmpty() ||
                    !Directory.Exists(SaveLocationsViewModel.GlobalSaveDirectory))
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Save Location Not Specified",
                        "Please specify a save directory for your files.",
                        DialogButtons.Ok);

                    return;
                }
            }
            else
            {
                // Check that all categories have a valid save location
                var categoryWithNoSaveDirectory = SaveLocationsViewModel
                    .Categories
                    .FirstOrDefault(c =>
                        c.CategorySaveDirectory == null || c.CategorySaveDirectory.SaveDirectory.IsStringNullOrEmpty());

                if (categoryWithNoSaveDirectory != null)
                {
                    await DialogBoxManager.ShowInfoDialogAsync("Save Location Not Specified",
                        $"Please specify a save directory for the category '{categoryWithNoSaveDirectory.Title}'.",
                        DialogButtons.Ok);

                    return;
                }
            }

            // Validate selected duplicate download link action
            if (DownloadsViewModel.SelectedDuplicateDownloadLinkAction.IsStringNullOrEmpty())
            {
                await DialogBoxManager.ShowInfoDialogAsync("Duplicate Link Handling Not Specified",
                    "No action has been specified for handling duplicate links. Please define how duplicate links should be managed when added.",
                    DialogButtons.Ok);

                return;
            }

            // Validate selected maximum connections count
            if (DownloadsViewModel.SelectedMaximumConnectionsCount == 0)
            {
                await DialogBoxManager.ShowInfoDialogAsync("Invalid or Unspecified File Divisions",
                    "The number of file divisions for the download is either unspecified or invalid. Please choose a valid option and try saving again.",
                    DialogButtons.Ok);

                return;
            }

            // Validate selected speed unit
            if (DownloadsViewModel.SelectedSpeedUnit.IsStringNullOrEmpty())
            {
                var result = await DialogBoxManager.ShowInfoDialogAsync("Speed Limiter Unit Not Specified",
                    "You haven’t specified the unit for the speed limiter. Would you like to use KB as the default unit?",
                    DialogButtons.YesNo);

                if (result == DialogResult.Yes)
                    DownloadsViewModel.SelectedSpeedUnit = Constants.SpeedLimiterUnits.FirstOrDefault();
                else
                    return;
            }

            // Save general settings
            AppService.SettingsService.Settings.StartOnSystemStartup = GeneralsViewModel.StartOnSystemStartup;
            AppService.SettingsService.Settings.UseBrowserExtension = GeneralsViewModel.UseBrowserExtension;
            AppService.SettingsService.Settings.DarkMode = GeneralsViewModel.DarkMode;
            AppService.SettingsService.Settings.UseManager = GeneralsViewModel.UseManager;
            AppService.SettingsService.Settings.AlwaysKeepManagerOnTop = GeneralsViewModel.UseManager && GeneralsViewModel.AlwaysKeepManagerOnTop;
            AppService.SettingsService.Settings.ApplicationFont = GeneralsViewModel.SelectedFont;

            // Save categories settings
            AppService.SettingsService.Settings.DisableCategories = SaveLocationsViewModel.DisableCategories;
            AppService.SettingsService.Settings.GlobalSaveLocation = SaveLocationsViewModel.GlobalSaveDirectory;

            // Get primary keys from save locations
            var primaryKeys = SaveLocationsViewModel
                .Categories
                .Select(c => c.Id)
                .ToList();

            // Find save locations
            var saveDirectories = AppService
                .CategoryService
                .Categories
                .Select(c => c.CategorySaveDirectory)
                .Where(csd => csd is { CategoryId: not null } && primaryKeys.Contains(csd.CategoryId.Value))
                .ToList();

            // Update save locations
            var saveCategoryDirectories = false;
            foreach (var saveDirectory in saveDirectories)
            {
                // Find category and make sure it has a save directory
                var category = SaveLocationsViewModel
                    .Categories
                    .FirstOrDefault(c => c.Id == saveDirectory!.CategoryId);

                // If category doesn't have a save directory or the save directory is the same, continue
                if (category?.CategorySaveDirectory?.SaveDirectory.IsStringNullOrEmpty() != false || saveDirectory!.SaveDirectory.Equals(category.CategorySaveDirectory?.SaveDirectory))
                    continue;

                // Update save directory
                saveDirectory.SaveDirectory = category.CategorySaveDirectory!.SaveDirectory;
                // Update category save directory
                await AppService.CategoryService.UpdateSaveDirectoryAsync(category, saveDirectory, reloadData: false);
                saveCategoryDirectories = true;
            }

            // Save all category directories
            if (saveCategoryDirectories)
                await AppService.CategoryService.LoadCategoriesAsync();

            // Save downloads settings
            AppService.SettingsService.Settings.ShowStartDownloadDialog = DownloadsViewModel.ShowStartDownloadDialog;
            AppService.SettingsService.Settings.ShowCompleteDownloadDialog = DownloadsViewModel.ShowCompleteDownloadDialog;

            // Get selected duplicate download link action
            var duplicateAction = Constants.GetDuplicateActionFromMessage(DownloadsViewModel.SelectedDuplicateDownloadLinkAction ?? string.Empty);
            AppService.SettingsService.Settings.DuplicateDownloadLinkAction = duplicateAction;

            // Update download options
            AppService.SettingsService.Settings.MaximumConnectionsCount = DownloadsViewModel.SelectedMaximumConnectionsCount;
            AppService.SettingsService.Settings.IsSpeedLimiterEnabled = DownloadsViewModel.IsSpeedLimiterEnabled;
            AppService.SettingsService.Settings.LimitSpeed = DownloadsViewModel.IsSpeedLimiterEnabled ? DownloadsViewModel.SpeedLimit : null;
            AppService.SettingsService.Settings.LimitUnit = DownloadsViewModel.IsSpeedLimiterEnabled ? DownloadsViewModel.SelectedSpeedUnit : null;

            // Save proxy settings
            switch (ProxyViewModel)
            {
                case { DisableProxy: true }:
                {
                    await AppService.SettingsService.DisableProxyAsync();
                    break;
                }

                case { UseSystemProxySettings: true }:
                {
                    await AppService.SettingsService.UseSystemProxySettingsAsync();
                    break;
                }

                case { UseCustomProxy: true }:
                {
                    var activeProxy = ProxyViewModel
                        .AvailableProxies
                        .FirstOrDefault(p => p.IsActive);

                    if (activeProxy == null)
                        break;

                    await AppService.SettingsService.UseCustomProxyAsync(activeProxy);
                    break;
                }

                default:
                    throw new InvalidOperationException("An error occurred while trying to save settings.");
            }

            // Save notifications settings
            AppService.SettingsService.Settings.UseDownloadCompleteSound = NotificationsViewModel.DownloadComplete;
            AppService.SettingsService.Settings.UseDownloadStoppedSound = NotificationsViewModel.DownloadStopped;
            AppService.SettingsService.Settings.UseDownloadFailedSound = NotificationsViewModel.DownloadFailed;
            AppService.SettingsService.Settings.UseQueueStartedSound = NotificationsViewModel.QueueStarted;
            AppService.SettingsService.Settings.UseQueueStoppedSound = NotificationsViewModel.QueueStopped;
            AppService.SettingsService.Settings.UseQueueFinishedSound = NotificationsViewModel.QueueFinished;
            AppService.SettingsService.Settings.UseSystemNotifications = NotificationsViewModel.UseSystemNotifications;

            // Save settings
            await AppService.SettingsService.SaveSettingsAsync(AppService.SettingsService.Settings);
            owner.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to save settings. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static void Cancel(Window? owner)
    {
        owner?.Close();
    }
}
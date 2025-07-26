using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class ManageLinksWindowViewModel : ViewModelBase
{
    #region Private fields

    private bool _hideHtmlFiles;
    private bool _saveEachFileByCategory = true;
    private bool _assignAllFilesToSpecificCategory;
    private ObservableCollection<CategoryViewModel> _categories = [];
    private CategoryViewModel? _selectedCategory;
    private bool _saveAllFilesToSpecificDirectory;
    private string _saveAllFilesToSpecificDirectoryPath = string.Empty;
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];
    private bool _selectAllDownloadFiles;

    #endregion

    #region Properties

    public bool HideHtmlFiles
    {
        get => _hideHtmlFiles;
        set => this.RaiseAndSetIfChanged(ref _hideHtmlFiles, value);
    }

    public bool SaveEachFileByCategory
    {
        get => _saveEachFileByCategory;
        set => this.RaiseAndSetIfChanged(ref _saveEachFileByCategory, value);
    }

    public bool AssignAllFilesToSpecificCategory
    {
        get => _assignAllFilesToSpecificCategory;
        set => this.RaiseAndSetIfChanged(ref _assignAllFilesToSpecificCategory, value);
    }

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    public bool SaveAllFilesToSpecificDirectory
    {
        get => _saveAllFilesToSpecificDirectory;
        set => this.RaiseAndSetIfChanged(ref _saveAllFilesToSpecificDirectory, value);
    }

    public string SaveAllFilesToSpecificDirectoryPath
    {
        get => _saveAllFilesToSpecificDirectoryPath;
        set => this.RaiseAndSetIfChanged(ref _saveAllFilesToSpecificDirectoryPath, value);
    }

    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    public bool SelectAllDownloadFiles
    {
        get => _selectAllDownloadFiles;
        set => this.RaiseAndSetIfChanged(ref _selectAllDownloadFiles, value);
    }

    #endregion

    #region Commands

    public ICommand BrowseSaveDirectoryCommand { get; }

    public ICommand SelectAllRowsCommand { get; }

    public ICommand ChangeSaveModeCommand { get; }

    #endregion

    public ManageLinksWindowViewModel(IAppService appService) : base(appService)
    {
        BrowseSaveDirectoryCommand = ReactiveCommand.CreateFromTask<Window?>(BrowseSaveDirectoryAsync);
        SelectAllRowsCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRows);
        ChangeSaveModeCommand = ReactiveCommand.Create<ToggleSwitch?>(ChangeSaveMode);

        LoadCategories();
        LoadDefaultDirectorySavePath();
    }

    #region Command actions

    private async Task BrowseSaveDirectoryAsync(Window? owner)
    {
        try
        {
            var storageProvider = owner?.StorageProvider;
            if (storageProvider == null)
            {
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Error",
                    dialogMessage: "Storage provider is null. Please restart the application.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            var options = new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false,
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(SaveAllFilesToSpecificDirectoryPath)
            };

            var directories = await storageProvider.OpenFolderPickerAsync(options);
            if (!directories.Any())
                return;

            SaveAllFilesToSpecificDirectoryPath = directories[0].Path.LocalPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while browsing save directory. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void SelectAllRows(DataGrid? dataGrid)
    {
        if (dataGrid == null)
            return;

        if (DownloadFiles.Count == 0)
        {
            SelectAllDownloadFiles = false;
            dataGrid.SelectedIndex = -1;
            return;
        }

        if (!SelectAllDownloadFiles)
        {
            dataGrid.SelectedIndex = -1;
        }
        else
        {
            dataGrid.SelectAll();
        }
    }

    private void ChangeSaveMode(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch == null)
            return;

        switch (toggleSwitch.Name)
        {
            case "SaveEachFileByCategoryToggleSwitch":
            {
                SaveEachFileByCategory = true;
                AssignAllFilesToSpecificCategory = SaveAllFilesToSpecificDirectory = false;
                break;
            }

            case "AssignAllFilesToSpecificCategoryToggleSwitch":
            {
                AssignAllFilesToSpecificCategory = true;
                SaveEachFileByCategory = SaveAllFilesToSpecificDirectory = false;
                break;
            }

            case "SaveAllFilesToSpecificDirectoryToggleSwitch":
            {
                SaveAllFilesToSpecificDirectory = true;
                SaveEachFileByCategory = AssignAllFilesToSpecificCategory = false;
                break;
            }
        }
    }

    #endregion

    #region Helpers

    private void LoadCategories()
    {
        Categories = AppService.CategoryService.Categories;
        SelectedCategory = Categories.FirstOrDefault();
    }

    private void LoadDefaultDirectorySavePath()
    {
        var isCategoriesEnabled = !AppService.SettingsService.Settings.DisableCategories;
        if (isCategoriesEnabled)
        {
            var generalCategory = Categories.FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle));
            if (generalCategory?.CategorySaveDirectory != null)
            {
                SaveAllFilesToSpecificDirectoryPath = generalCategory.CategorySaveDirectory.SaveDirectory;
                return;
            }
        }

        SaveAllFilesToSpecificDirectoryPath = AppService.SettingsService.Settings.GlobalSaveLocation ?? string.Empty;
    }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.DownloadFileService.Models;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class ManageLinksWindowViewModel : ViewModelBase
{
    #region Private fields

    /// <summary>
    /// List of download files that we want to get their info from the web.
    /// </summary>
    private readonly List<DownloadFileViewModel> _downloadFileDataList;

    /// <summary>
    /// The cancellation token to cancel loading files.
    /// </summary>
    private readonly CancellationTokenSource _cancelToken;

    /// <summary>
    /// Indicates whether the HTML files should be removed from the download files list.
    /// </summary>
    private bool _hideHtmlFiles;

    /// <summary>
    /// Indicates whether the download files should be saved by category.
    /// </summary>
    private bool _saveEachFileByCategory = true;

    /// <summary>
    /// Indicates whether the download files should be saved into one single category.
    /// </summary>
    private bool _assignAllFilesToSpecificCategory;

    /// <summary>
    /// The list of categories that are in the application.
    /// </summary>
    private ObservableCollection<CategoryViewModel> _categories = [];

    /// <summary>
    /// Indicates the selected category from the list of categories.
    /// </summary>
    private CategoryViewModel? _selectedCategory;

    /// <summary>
    /// Indicates whether the download files should be saved in the specific directory.
    /// </summary>
    private bool _saveAllFilesToSpecificDirectory;

    /// <summary>
    /// Indicates the directory that download files should be saved on it.
    /// </summary>
    private string _saveAllFilesToSpecificDirectoryPath = string.Empty;

    /// <summary>
    /// The list of download files that exists in the <see cref="_downloadFileDataList"/> list.
    /// </summary>
    private ObservableCollection<DownloadFileViewModel> _downloadFiles = [];

    /// <summary>
    /// Indicates whether all download files in the data grid are selected.
    /// </summary>
    private bool _selectAllDownloadFiles;

    /// <summary>
    /// Indicates whether the save button is enabled or not.
    /// </summary>
    private bool _saveButtonIsEnabled = true;

    /// <summary>
    /// Indicates the text of the save button.
    /// </summary>
    private string _saveButtonText = "Save";

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates whether the HTML files should be removed from the download files list.
    /// </summary>
    public bool HideHtmlFiles
    {
        get => _hideHtmlFiles;
        set
        {
            this.RaiseAndSetIfChanged(ref _hideHtmlFiles, value);
            FilterDownloadFiles();
        }
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the download files should be saved by category.
    /// </summary>
    public bool SaveEachFileByCategory
    {
        get => _saveEachFileByCategory;
        set => this.RaiseAndSetIfChanged(ref _saveEachFileByCategory, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the download files should be saved into one single category.
    /// </summary>
    public bool AssignAllFilesToSpecificCategory
    {
        get => _assignAllFilesToSpecificCategory;
        set => this.RaiseAndSetIfChanged(ref _assignAllFilesToSpecificCategory, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the list of categories that are in the application.
    /// </summary>
    public ObservableCollection<CategoryViewModel> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the selected category from the list of categories.
    /// </summary>
    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the download files should be saved in the specific directory.
    /// </summary>
    public bool SaveAllFilesToSpecificDirectory
    {
        get => _saveAllFilesToSpecificDirectory;
        set => this.RaiseAndSetIfChanged(ref _saveAllFilesToSpecificDirectory, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the directory that download files should be saved on it.
    /// </summary>
    public string SaveAllFilesToSpecificDirectoryPath
    {
        get => _saveAllFilesToSpecificDirectoryPath;
        set => this.RaiseAndSetIfChanged(ref _saveAllFilesToSpecificDirectoryPath, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the list of download files that exists in the <see cref="_downloadFileDataList"/> list.
    /// </summary>
    public ObservableCollection<DownloadFileViewModel> DownloadFiles
    {
        get => _downloadFiles;
        set => this.RaiseAndSetIfChanged(ref _downloadFiles, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether all download files in the data grid are selected.
    /// </summary>
    public bool SelectAllDownloadFiles
    {
        get => _selectAllDownloadFiles;
        set => this.RaiseAndSetIfChanged(ref _selectAllDownloadFiles, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates whether the save button is enabled or not.
    /// </summary>
    public bool SaveButtonIsEnabled
    {
        get => _saveButtonIsEnabled;
        set => this.RaiseAndSetIfChanged(ref _saveButtonIsEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value that indicates the text of the save button.
    /// </summary>
    public string SaveButtonText
    {
        get => _saveButtonText;
        set => this.RaiseAndSetIfChanged(ref _saveButtonText, value);
    }

    /// <summary>
    /// Gets a value that indicates the filtered download files.
    /// </summary>
    public ObservableCollection<DownloadFileViewModel> FilteredDownloadFiles { get; private set; } = [];

    #endregion

    #region Commands

    /// <summary>
    /// Gets a value that indicates the command to browse the save directory.
    /// </summary>
    public ICommand BrowseSaveDirectoryCommand { get; }

    /// <summary>
    /// Gets a value that indicates the command to select all rows in download files data grid.
    /// </summary>
    public ICommand SelectAllRowsCommand { get; }

    /// <summary>
    /// Gets a value that indicates the command to change the save mode.
    /// </summary>
    public ICommand ChangeSaveModeCommand { get; }

    /// <summary>
    /// Gets a value that indicates the command to save the changes.
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// Gets a value that indicates the command to cancel the changes.
    /// </summary>
    public ICommand CancelCommand { get; }

    #endregion

    public ManageLinksWindowViewModel(IAppService appService, List<DownloadFileViewModel> downloadFileDataList) : base(appService)
    {
        _downloadFileDataList = downloadFileDataList;
        _cancelToken = new CancellationTokenSource();

        BrowseSaveDirectoryCommand = ReactiveCommand.CreateFromTask<Window?>(BrowseSaveDirectoryAsync);
        SelectAllRowsCommand = ReactiveCommand.Create<DataGrid?>(SelectAllRows);
        ChangeSaveModeCommand = ReactiveCommand.Create<ToggleSwitch?>(ChangeSaveMode);
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);

        LoadCategories();
        LoadDefaultDirectorySavePath();
        _ = LoadDownloadFilesAsync();
    }

    #region Command actions

    /// <summary>
    /// Handles the browse save directory command.
    /// Opens a folder picker dialog to select a directory.
    /// </summary>
    /// <param name="owner">The owner window of the dialog.</param>
    private async Task BrowseSaveDirectoryAsync(Window? owner)
    {
        try
        {
            var storageProvider = owner?.StorageProvider;
            // If the storage provider is null, show an error dialog and return
            if (storageProvider == null)
            {
                await DialogBoxManager.ShowDangerDialogAsync(dialogHeader: "Error",
                    dialogMessage: "Storage provider is null. Please restart the application.",
                    dialogButtons: DialogButtons.Ok);

                return;
            }

            // Create a new folder picker open options object
            var options = new FolderPickerOpenOptions
            {
                Title = "Select Directory",
                AllowMultiple = false,
                SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(SaveAllFilesToSpecificDirectoryPath)
            };

            // Open the folder picker and get the selected directories
            var directories = await storageProvider.OpenFolderPickerAsync(options);
            // If no directories are selected, return
            if (!directories.Any())
                return;

            // Set the save all files to specific directory path to the selected directory
            SaveAllFilesToSpecificDirectoryPath = directories[0].Path.IsAbsoluteUri ? directories[0].Path.LocalPath : directories[0].Path.OriginalString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while browsing save directory. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the select all rows command.
    /// Selects all rows in the data grid.
    /// </summary>
    /// <param name="dataGrid">The data grid to select all rows in.</param>
    private void SelectAllRows(DataGrid? dataGrid)
    {
        // Check if the dataGrid is null
        if (dataGrid == null)
            return;

        // Check if the DownloadFiles list is empty
        if (DownloadFiles.Count == 0)
        {
            // Set the SelectAllDownloadFiles flag to false
            SelectAllDownloadFiles = false;
            // Set the selected index of the dataGrid to -1
            dataGrid.SelectedIndex = -1;
            return;
        }

        // Check if the SelectAllDownloadFiles flag is false
        if (!SelectAllDownloadFiles)
        {
            // Set the selected index of the dataGrid to -1
            dataGrid.SelectedIndex = -1;
        }
        else
        {
            // Select all rows in the dataGrid
            dataGrid.SelectAll();
        }
    }

    /// <summary>
    /// Handles the change save mode command.
    /// Changes the save mode based on the selected toggle switch.
    /// </summary>
    /// <param name="toggleSwitch">The toggle switch that was changed.</param>
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

    /// <summary>
    /// Handles the save command.
    /// Saves the download files to the database.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task SaveAsync(Window? owner)
    {
        try
        {
            // Check if the owner is null or if the save button is not enabled
            if (owner == null || !SaveButtonIsEnabled)
                return;

            // Find the data grid in the owner window
            var dataGrid = owner.FindControl<DataGrid>("DownloadFilesDataGrid");
            if (dataGrid == null)
                return;

            // Get the selected download files from the data grid
            var selectedDownloadFiles = dataGrid
                .SelectedItems
                .OfType<DownloadFileViewModel>()
                .ToList();

            // Check if there are any selected download files
            if (selectedDownloadFiles.Count == 0)
                return;

            // Check which save option is selected and perform the corresponding action
            switch (this)
            {
                // If the save each file by category option is selected
                case { SaveEachFileByCategory: true }:
                {
                    // Add each selected download file to the download file service
                    foreach (var downloadFile in selectedDownloadFiles)
                    {
                        try
                        {
                            await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "An error occurred while adding the download file to the download file service. Error message: {ErrorMessage}",
                                ex.Message);
                        }
                    }

                    break;
                }

                // If the assign all files to specific category option is selected
                case { AssignAllFilesToSpecificCategory: true }:
                {
                    // Check if a category is selected
                    if (SelectedCategory == null)
                    {
                        // Show an info dialog if no category is selected
                        await DialogBoxManager.ShowInfoDialogAsync(dialogHeader: "No category selected",
                            dialogMessage: "Please select a category to assign all files to.",
                            dialogButtons: DialogButtons.Ok);

                        return;
                    }

                    // Assign the selected category to each selected download file and add it to the download file service
                    foreach (var downloadFile in selectedDownloadFiles)
                    {
                        try
                        {
                            downloadFile.CategoryId = SelectedCategory.Id;
                            await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "An error occurred while adding the download file to the download file service. Error message: {ErrorMessage}",
                                ex.Message);
                        }
                    }

                    break;
                }

                // If the save all files to specific directory option is selected
                case { SaveAllFilesToSpecificDirectory: true }:
                {
                    // Check if a directory is selected
                    if (SaveAllFilesToSpecificDirectoryPath.IsStringNullOrEmpty() || !Directory.Exists(SaveAllFilesToSpecificDirectoryPath))
                    {
                        // Show an info dialog if no directory is selected
                        await DialogBoxManager.ShowInfoDialogAsync(dialogHeader: "Select Save Location",
                            dialogMessage: "Please select a destination folder for saving all files.",
                            dialogButtons: DialogButtons.Ok);

                        return;
                    }

                    // Assign the selected directory to each selected download file and add it to the download file service
                    foreach (var downloadFile in selectedDownloadFiles)
                    {
                        try
                        {
                            downloadFile.SaveLocation = SaveAllFilesToSpecificDirectoryPath;
                            await AppService.DownloadFileService.AddDownloadFileAsync(downloadFile);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "An error occurred while adding the download file to the download file service. Error message: {ErrorMessage}",
                                ex.Message);
                        }
                    }

                    break;
                }

                // If no save option is selected, do nothing
                default:
                    return;
            }

            // Close the owner
            owner.Close();
        }
        catch (Exception ex)
        {
            // Log the error and show an error dialog
            Log.Error(ex, "An error occurred while saving the download files. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    /// <summary>
    /// Handles the cancel command.
    /// Closes the Manage Links window and cancel operation.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    private async Task CancelAsync(Window? owner)
    {
        try
        {
            // Cancel loading operation
            await _cancelToken.CancelAsync();
            // Dispose the cancellation token
            _cancelToken.Dispose();
            // Close the window
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while closing the Manage Links window. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Loads categories from the application service.
    /// </summary>
    private void LoadCategories()
    {
        Categories = AppService.CategoryService.Categories;
        SelectedCategory = Categories.FirstOrDefault();
    }

    /// <summary>
    /// Loads default directory save path from the application service.
    /// </summary>
    private void LoadDefaultDirectorySavePath()
    {
        // Check if the categories are enabled
        var areCategoriesEnabled = !AppService.SettingsService.Settings.DisableCategories;
        // If categories are enabled, find general category and set its save directory as default directory save path
        if (areCategoriesEnabled)
        {
            // Find general category
            var generalCategory = Categories.FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle));
            // Make sure general category exists
            if (generalCategory?.CategorySaveDirectory != null)
            {
                // Set general category's save-directory as default directory save path
                SaveAllFilesToSpecificDirectoryPath = generalCategory.CategorySaveDirectory.SaveDirectory;
                return;
            }
        }

        // Otherwise, set default directory save path to the global save location
        SaveAllFilesToSpecificDirectoryPath = AppService.SettingsService.Settings.GlobalSaveLocation ?? string.Empty;
    }

    /// <summary>
    /// Loads download files from the download file data list.
    /// </summary>
    private async Task LoadDownloadFilesAsync()
    {
        try
        {
            // Change save button state
            SaveButtonIsEnabled = false;
            SaveButtonText = "Loading...";

            // Loop through each download file
            foreach (var data in _downloadFileDataList)
            {
                // Check if cancellation is requested
                if (_cancelToken.IsCancellationRequested)
                    return;

                // Create download options
                var options = new DownloadFileOptions
                {
                    Referer = data.Referer,
                    PageAddress = data.PageAddress,
                    Description = data.Description
                };

                // Get the download file from the URL
                var downloadFile = await AppService.DownloadFileService.GetDownloadFileFromUrlAsync(data.Url, options, _cancelToken.Token);
                // Validate the download file
                var isValid = await AppService.DownloadFileService.ValidateDownloadFileAsync(downloadFile, showMessage: false);
                // If the download file is not valid, skip to the next URL
                if (!isValid)
                    continue;

                // Get the file extension
                var ext = Path.GetExtension(downloadFile.FileName);
                // Get the file type from the category service
                var fileType = AppService
                    .CategoryService
                    .Categories
                    .FirstOrDefault(c => c.Id == downloadFile.CategoryId)?
                    .FileExtensions
                    .FirstOrDefault(fe => fe.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))?
                    .Alias ?? Constants.UnknownFileType;

                // Set the file type of the download file
                downloadFile.FileType = fileType;
                // Add the download file to the download files list
                DownloadFiles.Add(downloadFile);
                // Filter download files
                FilterDownloadFiles();
            }

            // Raise the property changed event for the download files list
            this.RaisePropertyChanged(nameof(DownloadFiles));
        }
        catch (Exception ex)
        {
            Log.Error("An error occurred while loading download files. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }

        // Reset save button state
        SaveButtonIsEnabled = true;
        SaveButtonText = "Save";
    }

    /// <summary>
    /// Filters download files to show/hide HTML files.
    /// </summary>
    private void FilterDownloadFiles()
    {
        if (HideHtmlFiles)
        {
            FilteredDownloadFiles = DownloadFiles
                .Where(df => Path.GetExtension(df.FileName)?.Equals(".html") == false && Path.GetExtension(df.FileName)?.Equals(".htm") == false)
                .ToObservableCollection();
        }
        else
        {
            FilteredDownloadFiles = DownloadFiles;
        }

        this.RaisePropertyChanged(nameof(FilteredDownloadFiles));
    }

    #endregion
}
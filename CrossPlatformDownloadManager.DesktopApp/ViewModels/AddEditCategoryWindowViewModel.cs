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

public class AddEditCategoryWindowViewModel : ViewModelBase
{
    #region Private Fields

    private string? _categoryTitle;
    private ObservableCollection<CategoryFileExtensionViewModel> _fileExtensions = [];
    private CategoryFileExtensionViewModel? _selectedFileExtension;
    private CategoryFileExtensionViewModel _currentFileExtension = new();
    private ObservableCollection<string> _siteAddresses = [];
    private string? _selectedSiteAddress;
    private string? _saveDirectory;
    private bool _isEditMode;
    private int? _categoryId;
    private bool _isDefaultCategory;
    private bool _isGeneralCategory;

    #endregion

    #region Properties

    public string? CategoryTitle
    {
        get => _categoryTitle;
        set => this.RaiseAndSetIfChanged(ref _categoryTitle, value);
    }

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => _fileExtensions;
        set
        {
            this.RaiseAndSetIfChanged(ref _fileExtensions, value);
            this.RaisePropertyChanged(nameof(IsFileTypesDataGridVisible));
        }
    }

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => _selectedFileExtension;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFileExtension, value);
            if (SelectedFileExtension != null)
            {
                CurrentFileExtension = new CategoryFileExtensionViewModel
                {
                    Id = SelectedFileExtension.Id,
                    Extension = SelectedFileExtension.Extension,
                    Alias = SelectedFileExtension.Alias,
                    CategoryId = SelectedFileExtension.CategoryId,
                    Category = SelectedFileExtension.Category
                };
            }
        }
    }

    public CategoryFileExtensionViewModel CurrentFileExtension
    {
        get => _currentFileExtension;
        set
        {
            this.RaiseAndSetIfChanged(ref _currentFileExtension, value);
            this.RaisePropertyChanged(nameof(IsDeleteClearFileExtensionButtonEnabled));
            this.RaisePropertyChanged(nameof(IsSaveFileExtensionButtonEnabled));
        }
    }

    public ObservableCollection<string> SiteAddresses
    {
        get => _siteAddresses;
        set
        {
            this.RaiseAndSetIfChanged(ref _siteAddresses, value);
            this.RaisePropertyChanged(nameof(IsSiteAddressesDataGridVisible));
        }
    }

    public string? SelectedSiteAddress
    {
        get => _selectedSiteAddress;
        set => this.RaiseAndSetIfChanged(ref _selectedSiteAddress, value);
    }

    public string? SaveDirectory
    {
        get => _saveDirectory;
        set => this.RaiseAndSetIfChanged(ref _saveDirectory, value);
    }

    public string Title => IsEditMode ? "CDM - Edit Category" : "CDM - Add New Category";

    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEditMode, value);
            this.RaisePropertyChanged(nameof(Title));
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoryId, value);
            LoadCategory();
        }
    }

    public bool IsDefaultCategory
    {
        get => _isDefaultCategory;
        set => this.RaiseAndSetIfChanged(ref _isDefaultCategory, value);
    }
    
    public bool IsGeneralCategory
    {
        get => _isGeneralCategory;
        set => this.RaiseAndSetIfChanged(ref _isGeneralCategory, value);
    }

    public bool IsFileTypesDataGridVisible => FileExtensions.Count > 0;
    public bool IsSiteAddressesDataGridVisible => SiteAddresses.Count > 0;

    public bool IsDeleteClearFileExtensionButtonEnabled =>
        CurrentFileExtension.Id > 0 || !CurrentFileExtension.Extension.IsNullOrEmpty() || !CurrentFileExtension.Alias.IsNullOrEmpty();

    public bool IsSaveFileExtensionButtonEnabled => !CurrentFileExtension.Extension.IsNullOrEmpty() && !CurrentFileExtension.Alias.IsNullOrEmpty();

    #endregion

    #region Commands

    public ICommand SaveFileExtensionCommand { get; }

    public ICommand DeleteClearFileExtensionCommand { get; }

    public ICommand SaveSiteAddressCommand { get; }

    public ICommand DeleteClearSiteAddressCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public AddEditCategoryWindowViewModel(IAppService appService) : base(appService)
    {
        LoadCategory();

        SaveFileExtensionCommand = ReactiveCommand.Create(SaveFileExtension);
        DeleteClearFileExtensionCommand = ReactiveCommand.Create(DeleteClearFileExtension);
        SaveSiteAddressCommand = ReactiveCommand.Create(SaveSiteAddress);
        DeleteClearSiteAddressCommand = ReactiveCommand.Create(DeleteClearSiteAddress);
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private void SaveFileExtension()
    {
        CurrentFileExtension.Extension = CurrentFileExtension.Extension.Trim('.').Replace(".", "").Replace(" ", "").ToLower();
        CurrentFileExtension.Alias = CurrentFileExtension.Alias.Trim();
        if (CurrentFileExtension.Extension.IsNullOrEmpty() || CurrentFileExtension.Alias.IsNullOrEmpty())
            return;

        var existingFileExtension = FileExtensions
            .FirstOrDefault(fe => fe.Extension.Equals("." + CurrentFileExtension.Extension, StringComparison.OrdinalIgnoreCase));

        if (existingFileExtension != null)
        {
            existingFileExtension.Alias = CurrentFileExtension.Alias.Trim();
            CurrentFileExtension = new CategoryFileExtensionViewModel();
            SelectedFileExtension = null;
            return;
        }

        var newFileExtension = new CategoryFileExtensionViewModel
        {
            Extension = "." + CurrentFileExtension.Extension,
            Alias = CurrentFileExtension.Alias
        };

        FileExtensions.Add(newFileExtension);
        this.RaisePropertyChanged(nameof(IsFileTypesDataGridVisible));

        SelectedFileExtension = null;
        CurrentFileExtension = new CategoryFileExtensionViewModel();
    }

    private void DeleteClearFileExtension()
    {
        // If editing an existing file extension, remove file extension from list
        if (CurrentFileExtension.Id > 0)
        {
            var fileExtension = FileExtensions.FirstOrDefault(fe => fe.Id == CurrentFileExtension.Id);
            if (fileExtension == null || !fileExtension.Extension.Equals(CurrentFileExtension.Extension))
            {
                CurrentFileExtension = new CategoryFileExtensionViewModel();
                SelectedFileExtension = null;
                return;
            }

            FileExtensions.Remove(fileExtension);
            this.RaisePropertyChanged(nameof(IsFileTypesDataGridVisible));
        }
        // If creating a new file extension, clear current file extension
        else
        {
            var fileExtension = FileExtensions
                .FirstOrDefault(fe => fe.Extension.Equals(CurrentFileExtension.Extension, StringComparison.OrdinalIgnoreCase)
                                      && fe.Alias.Equals(CurrentFileExtension.Alias, StringComparison.OrdinalIgnoreCase));

            if (fileExtension is { Id: <= 0 })
            {
                FileExtensions.Remove(fileExtension);
                this.RaisePropertyChanged(nameof(IsFileTypesDataGridVisible));
            }

            CurrentFileExtension = new CategoryFileExtensionViewModel();
            SelectedFileExtension = null;
        }
    }

    private void SaveSiteAddress()
    {
        var siteAddress = SelectedSiteAddress?.Trim().Replace('\\', '/').Replace(" ", "").GetDomainFromUrl();
        if (siteAddress.IsNullOrEmpty())
            return;

        if (SiteAddresses.Any(sa => sa.Equals(siteAddress!)))
        {
            SelectedSiteAddress = string.Empty;
            return;
        }

        SiteAddresses.Add(siteAddress!);
        this.RaisePropertyChanged(nameof(IsSiteAddressesDataGridVisible));
        
        SelectedSiteAddress = string.Empty;
    }

    private void DeleteClearSiteAddress()
    {
        if (SelectedSiteAddress.IsNullOrEmpty())
        {
            SelectedSiteAddress = string.Empty;
            return;
        }

        var existingSiteAddress = SiteAddresses.FirstOrDefault(sa => sa.Equals(SelectedSiteAddress!));
        if (existingSiteAddress.IsNullOrEmpty())
        {
            SelectedSiteAddress = string.Empty;
            return;
        }

        SiteAddresses.Remove(existingSiteAddress!);
        this.RaisePropertyChanged(nameof(IsSiteAddressesDataGridVisible));
        
        SelectedSiteAddress = string.Empty;
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null || CategoryTitle.IsNullOrEmpty() || SaveDirectory.IsNullOrEmpty())
                return;

            CategoryTitle = CategoryTitle!.Trim();
            SaveDirectory = SaveDirectory!.Trim();

            CategoryViewModel? category;
            if (IsEditMode)
            {
                category = AppService
                    .CategoryService
                    .Categories
                    .FirstOrDefault(c => c.Id == CategoryId);

                if (category == null)
                    return;

                category.Title = CategoryTitle!;
                category.AutoAddLinkFromSites = SiteAddresses.Any() ? SiteAddresses.ConvertToJson() : null;

                await AppService.CategoryService.UpdateCategoryAsync(category);
            }
            else
            {
                category = new CategoryViewModel
                {
                    Icon = Constants.NewCategoryIcon,
                    Title = CategoryTitle!,
                    AutoAddLinkFromSites = SiteAddresses.Any() ? SiteAddresses.ConvertToJson() : null,
                    IsDefault = false
                };

                category.Id = await AppService.CategoryService.AddNewCategoryAsync(category);
                // Load added category
                category = AppService.CategoryService.Categories.FirstOrDefault(c => c.Id == category.Id);
                if (category == null)
                    throw new InvalidOperationException("Category not found.");
            }

            // Remove old file extensions
            if (IsEditMode && category.FileExtensions.Count > 0)
                await AppService.CategoryService.DeleteAllFileExtensionsAsync(category);

            // Convert file extensions to correct format
            var fileExtensions = FileExtensions
                .Select(fe =>
                {
                    fe.Id = 0;
                    return fe;
                })
                .ToList();

            // Add new file extensions for selected category
            await AppService.CategoryService.AddFileExtensionsAsync(category, fileExtensions);

            CategorySaveDirectoryViewModel? saveDirectory;
            if (IsEditMode)
            {
                saveDirectory = AppService
                    .CategoryService
                    .Categories
                    .FirstOrDefault(c => c.Id == category.Id)
                    ?.CategorySaveDirectory;

                if (saveDirectory == null)
                    return;

                saveDirectory.SaveDirectory = SaveDirectory!;
                await AppService.CategoryService.UpdateSaveDirectoryAsync(category, saveDirectory);
            }
            else
            {
                saveDirectory = new CategorySaveDirectoryViewModel { SaveDirectory = SaveDirectory! };
                await AppService.CategoryService.AddSaveDirectoryAsync(category, saveDirectory);
            }

            // Close window
            owner.Close(true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "And error occurred while trying to save the category. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "And error occurred while trying to cancel. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void LoadCategory()
    {
        if (CategoryId != null)
        {
            var category = AppService
                .CategoryService
                .Categories
                .FirstOrDefault(c => c.Id == CategoryId);

            if (category == null)
                return;

            CategoryTitle = category.Title;
            IsDefaultCategory = category.IsDefault;
            FileExtensions = category.FileExtensions.DeepCopy(ignoreLoops: true)!;
            IsGeneralCategory = category.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase);

            var json = category.AutoAddLinkFromSites;
            SiteAddresses = json.IsNullOrEmpty() ? [] : json!.ConvertFromJson<List<string>>().ToObservableCollection();
            if (category.CategorySaveDirectory != null)
                SaveDirectory = category.CategorySaveDirectory.SaveDirectory;
        }
        else
        {
            var generalCategory = AppService
                .CategoryService
                .Categories
                .FirstOrDefault(c => c.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase));

            if (generalCategory?.CategorySaveDirectory == null)
                return;

            SaveDirectory = generalCategory.CategorySaveDirectory.SaveDirectory;
        }
    }
}
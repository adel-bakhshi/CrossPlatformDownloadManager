using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
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
    private CategoryFileExtensionViewModel _newFileExtension = new();
    private ObservableCollection<string> _siteAddresses = [];
    private string? _siteAddress;
    private string? _saveDirectory;
    private bool _isEditMode;
    private int? _categoryId;

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
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    public CategoryFileExtensionViewModel NewFileExtension
    {
        get => _newFileExtension;
        set => this.RaiseAndSetIfChanged(ref _newFileExtension, value);
    }

    public ObservableCollection<string> SiteAddresses
    {
        get => _siteAddresses;
        set => this.RaiseAndSetIfChanged(ref _siteAddresses, value);
    }

    public string? SiteAddress
    {
        get => _siteAddress;
        set => this.RaiseAndSetIfChanged(ref _siteAddress, value);
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
            LoadCategoryAsync().GetAwaiter();
        }
    }

    #endregion

    #region Commands

    public ICommand AddNewFileExtensionCommand { get; }

    public ICommand DeleteFileExtensionCommand { get; }

    public ICommand AddNewSiteAddressCommand { get; }

    public ICommand DeleteSiteAddressCommand { get; }

    public ICommand SaveCommand { get; }

    #endregion

    public AddEditCategoryWindowViewModel(IAppService appService) : base(appService)
    {
        LoadCategoryAsync().GetAwaiter();

        AddNewFileExtensionCommand = ReactiveCommand.Create<Window?>(AddNewFileExtension);
        DeleteFileExtensionCommand = ReactiveCommand.Create<CategoryFileExtensionViewModel?>(DeleteFileExtension);
        AddNewSiteAddressCommand = ReactiveCommand.Create(AddNewSiteAddress);
        DeleteSiteAddressCommand = ReactiveCommand.Create<string?>(DeleteSiteAddress);
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            if (owner == null || CategoryTitle.IsNullOrEmpty() || SaveDirectory.IsNullOrEmpty())
                return;

            CategoryTitle = CategoryTitle!.Trim();
            SaveDirectory = SaveDirectory!.Trim();

            Category? category;
            if (IsEditMode)
            {
                category = await AppService
                    .UnitOfWork
                    .CategoryRepository
                    .GetAsync(where: c => c.Id == CategoryId,
                        includeProperties: "FileExtensions");

                if (category == null)
                    return;

                category.Title = CategoryTitle!;
                category.AutoAddLinkFromSites = SiteAddresses.Any() ? SiteAddresses.ConvertToJson() : null;
            }
            else
            {
                category = new Category
                {
                    Icon = Constants.NewCategoryIcon,
                    Title = CategoryTitle!,
                    AutoAddLinkFromSites = SiteAddresses.Any() ? SiteAddresses.ConvertToJson() : null,
                    IsDefault = false,
                };

                await AppService
                    .UnitOfWork
                    .CategoryRepository
                    .AddAsync(category);
            }

            await AppService.UnitOfWork.SaveAsync();

            if (IsEditMode && category.FileExtensions.Count > 0)
            {
                await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .DeleteAllAsync(category.FileExtensions);

                await AppService.UnitOfWork.SaveAsync();
            }

            var fileExtensions = FileExtensions
                .Select(fe => new CategoryFileExtension
                {
                    CategoryId = category.Id,
                    Extension = "." + fe.Extension!,
                    Alias = fe.Alias!,
                })
                .ToList();

            await AppService
                .UnitOfWork
                .CategoryFileExtensionRepository
                .AddRangeAsync(fileExtensions);

            await AppService.UnitOfWork.SaveAsync();

            CategorySaveDirectory? saveDirectory;
            if (IsEditMode)
            {
                saveDirectory = await AppService
                    .UnitOfWork
                    .CategorySaveDirectoryRepository
                    .GetAsync(where: cs => cs.CategoryId == category.Id);

                if (saveDirectory == null)
                    return;

                saveDirectory.SaveDirectory = SaveDirectory!;
            }
            else
            {
                saveDirectory = new CategorySaveDirectory
                {
                    SaveDirectory = SaveDirectory!,
                    CategoryId = category.Id,
                };

                await AppService
                    .UnitOfWork
                    .CategorySaveDirectoryRepository
                    .AddAsync(saveDirectory);
            }

            await AppService.UnitOfWork.SaveAsync();

            category.CategorySaveDirectoryId = saveDirectory.Id;
            await AppService.UnitOfWork.SaveAsync();

            owner.Close(true);
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "And error occured while trying to save the category. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private async Task LoadCategoryAsync()
    {
        try
        {
            if (CategoryId != null)
            {
                var category = await AppService
                    .UnitOfWork
                    .CategoryRepository
                    .GetAsync(where: c => c.Id == CategoryId, includeProperties: ["CategorySaveDirectory"]);

                if (category == null)
                    return;

                CategoryTitle = category.Title;
                FileExtensions = AppService
                    .Mapper
                    .Map<List<CategoryFileExtensionViewModel>>(category.FileExtensions)
                    .Select(fe =>
                    {
                        fe.Extension = fe.Extension!.TrimStart('.');
                        return fe;
                    })
                    .ToObservableCollection();

                var json = category.AutoAddLinkFromSites;
                SiteAddresses = json.IsNullOrEmpty() ? [] : json!.ConvertFromJson<List<string>>().ToObservableCollection();
                if (category.CategorySaveDirectory != null)
                    SaveDirectory = category.CategorySaveDirectory.SaveDirectory;
            }
            else
            {
                var generalCategory = await AppService
                    .UnitOfWork
                    .CategoryRepository
                    .GetAsync(where: c => c.Title == Constants.GeneralCategoryTitle, includeProperties: ["CategorySaveDirectory"]);

                if (generalCategory?.CategorySaveDirectory == null)
                    return;

                SaveDirectory = generalCategory.CategorySaveDirectory.SaveDirectory;
            }
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "And error occured while trying to load the category. Error message: {ErrorMessage}", ex.Message);
        }
    }

    private void DeleteSiteAddress(string? siteAddress)
    {
        if (siteAddress.IsNullOrEmpty())
            return;

        var siteAddresses = SiteAddresses.ToList();
        siteAddresses.Remove(siteAddress!);
        SiteAddresses = siteAddresses.ToObservableCollection();
    }

    private void AddNewSiteAddress()
    {
        if (SiteAddress.IsNullOrEmpty())
            return;

        if (SiteAddresses.Any(sa => sa.Equals(SiteAddress)))
            return;

        var siteAddresses = SiteAddresses.ToList();
        siteAddresses.Add(SiteAddress!.Trim().Replace(" ", ""));
        SiteAddresses = siteAddresses.ToObservableCollection();

        SiteAddress = string.Empty;
    }

    private void AddNewFileExtension(Window? owner)
    {
        if (NewFileExtension.Extension.IsNullOrEmpty() || NewFileExtension.Alias.IsNullOrEmpty())
            return;

        var isExist = FileExtensions
            .Any(fe => fe.Extension!.Equals(NewFileExtension.Extension, StringComparison.OrdinalIgnoreCase));

        if (isExist)
        {
            NewFileExtension = new CategoryFileExtensionViewModel();
            return;
        }

        var newFileExtension = new CategoryFileExtensionViewModel
        {
            Extension = NewFileExtension.Extension!.Trim().Replace(".", "").Replace(" ", "").ToLower(),
            Alias = NewFileExtension.Alias!.Trim(),
        };

        FileExtensions.Add(newFileExtension);
        this.RaisePropertyChanged(nameof(FileExtensions));
        NewFileExtension = new CategoryFileExtensionViewModel();
    }

    private void DeleteFileExtension(CategoryFileExtensionViewModel? fileExtension)
    {
        if (fileExtension == null)
            return;

        FileExtensions.Remove(fileExtension);
        this.RaisePropertyChanged(nameof(FileExtensions));
    }
}
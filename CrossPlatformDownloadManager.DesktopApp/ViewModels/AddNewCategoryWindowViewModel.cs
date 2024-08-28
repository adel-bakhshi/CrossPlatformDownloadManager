using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddNewCategoryWindowViewModel : ViewModelBase
{
    #region Properties

    private string? _categoryTitle;

    public string? CategoryTitle
    {
        get => _categoryTitle;
        set => this.RaiseAndSetIfChanged(ref _categoryTitle, value);
    }

    private ObservableCollection<CategoryFileExtension> _fileExtensions = [];

    public ObservableCollection<CategoryFileExtension> FileExtensions
    {
        get => _fileExtensions;
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    private CategoryFileExtension _newFileExtension = new();

    public CategoryFileExtension NewFileExtension
    {
        get => _newFileExtension;
        set => this.RaiseAndSetIfChanged(ref _newFileExtension, value);
    }

    private ObservableCollection<string> _siteAddresses = [];

    public ObservableCollection<string> SiteAddresses
    {
        get => _siteAddresses;
        set => this.RaiseAndSetIfChanged(ref _siteAddresses, value);
    }

    private string? _siteAddress;

    public string? SiteAddress
    {
        get => _siteAddress;
        set => this.RaiseAndSetIfChanged(ref _siteAddress, value);
    }

    private string? _saveDirectory;

    public string? SaveDirectory
    {
        get => _saveDirectory;
        set => this.RaiseAndSetIfChanged(ref _saveDirectory, value);
    }

    #endregion

    #region Commands

    public ICommand AddNewFileExtensionCommand { get; }

    public ICommand DeleteFileExtensionCommand { get; }

    public ICommand AddNewSiteAddressCommand { get; }

    public ICommand DeleteSiteAddressCommand { get; }

    public ICommand SaveCommand { get; }

    #endregion

    public AddNewCategoryWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(
        unitOfWork, downloadFileService)
    {
        SaveDirectory = GetGeneralDirectoryAsync().Result;

        AddNewFileExtensionCommand = ReactiveCommand.Create(AddNewFileExtension);
        DeleteFileExtensionCommand = ReactiveCommand.Create<CategoryFileExtension?>(DeleteFileExtension);
        AddNewSiteAddressCommand = ReactiveCommand.Create(AddNewSiteAddress);
        DeleteSiteAddressCommand = ReactiveCommand.Create<string?>(DeleteSiteAddress);
        SaveCommand = ReactiveCommand.Create<Window?>(Save);
    }

    private async void Save(Window? owner)
    {
        try
        {
            // TODO: Show message box
            if (owner == null || CategoryTitle.IsNullOrEmpty() || SaveDirectory.IsNullOrEmpty())
                return;

            var category = new Category
            {
                Icon = Constants.NewCategoryIcon,
                Title = CategoryTitle!.Trim(),
                AutoAddLinkFromSites = SiteAddresses.Any() ? SiteAddresses.ConvertToJson() : null,
                IsDefault = false,
            };

            await UnitOfWork.CategoryRepository.AddAsync(category);
            await UnitOfWork.SaveAsync();

            var fileExtensions = FileExtensions
                .Select(fe =>
                {
                    fe.CategoryId = category.Id;
                    fe.Extension = "." + fe.Extension.ToLower();
                    return fe;
                })
                .ToList();

            await UnitOfWork.CategoryFileExtensionRepository.AddRangeAsync(fileExtensions);
            await UnitOfWork.SaveAsync();

            var saveDirectory = new CategorySaveDirectory
            {
                SaveDirectory = SaveDirectory!.Trim(),
                CategoryId = category.Id,
            };

            await UnitOfWork.CategorySaveDirectoryRepository.AddAsync(saveDirectory);
            await UnitOfWork.SaveAsync();

            category.CategorySaveDirectoryId = saveDirectory.Id;
            await UnitOfWork.SaveAsync();

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task<string?> GetGeneralDirectoryAsync()
    {
        try
        {
            var saveDirectory = (await UnitOfWork.CategorySaveDirectoryRepository
                    .GetAsync(where: sd => sd.CategoryId == null))?
                .SaveDirectory;

            return saveDirectory;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return string.Empty;
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

    private void AddNewFileExtension()
    {
        if (NewFileExtension.Extension.IsNullOrEmpty() || NewFileExtension.Alias.IsNullOrEmpty())
            return;

        var isExist = FileExtensions
            .Any(fe => fe.Extension.Equals(NewFileExtension.Extension, StringComparison.OrdinalIgnoreCase));

        if (isExist)
        {
            NewFileExtension = new CategoryFileExtension();
            return;
        }

        var newFileExtension = new CategoryFileExtension
        {
            Extension = NewFileExtension.Extension.Trim().Replace(".", "").Replace(" ", "").ToLower(),
            Alias = NewFileExtension.Alias.Trim(),
        };

        var fileExtensions = FileExtensions.ToList();
        fileExtensions.Add(newFileExtension);
        FileExtensions = fileExtensions.ToObservableCollection();

        NewFileExtension = new CategoryFileExtension();
    }

    private void DeleteFileExtension(CategoryFileExtension? fileExtension)
    {
        if (fileExtension == null)
            return;

        var fileExtensions = FileExtensions.ToList();
        fileExtensions.Remove(fileExtension);
        FileExtensions = fileExtensions.ToObservableCollection();
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
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

    private ObservableCollection<CategoryFileExtension> _fileExtensions;

    public ObservableCollection<CategoryFileExtension> FileExtensions
    {
        get => _fileExtensions;
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    private CategoryFileExtension _newFileExtension = new CategoryFileExtension();

    public CategoryFileExtension NewFileExtension
    {
        get => _newFileExtension;
        set => this.RaiseAndSetIfChanged(ref _newFileExtension, value);
    }

    private ObservableCollection<string> _siteAddresses;

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

    public AddNewCategoryWindowViewModel(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
        FileExtensions = new ObservableCollection<CategoryFileExtension>();
        SiteAddresses = new ObservableCollection<string>();
        SaveDirectory = GetGeneralDirectory();

        AddNewFileExtensionCommand = ReactiveCommand.Create(AddNewFileExtension);
        DeleteFileExtensionCommand = ReactiveCommand.Create<CategoryFileExtension?>(DeleteFileExtension);
        AddNewSiteAddressCommand = ReactiveCommand.Create(AddNewSiteAddress);
        DeleteSiteAddressCommand = ReactiveCommand.Create<string?>(DeleteSiteAddress);
        SaveCommand = ReactiveCommand.Create<Window?>(Save);
    }

    private void Save(Window? owner)
    {
        try
        {
            if (owner == null || CategoryTitle.IsNullOrEmpty() || SaveDirectory.IsNullOrEmpty())
                return;

            var categoryItem = new Category
            {
                Icon =
                    "M500.3 7.3C507.7 13.3 512 22.4 512 32l0 144c0 26.5-28.7 48-64 48s-64-21.5-64-48s28.7-48 64-48l0-57L352 90.2 352 208c0 26.5-28.7 48-64 48s-64-21.5-64-48s28.7-48 64-48l0-96c0-15.3 10.8-28.4 25.7-31.4l160-32c9.4-1.9 19.1 .6 26.6 6.6zM74.7 304l11.8-17.8c5.9-8.9 15.9-14.2 26.6-14.2l61.7 0c10.7 0 20.7 5.3 26.6 14.2L213.3 304l26.7 0c26.5 0 48 21.5 48 48l0 112c0 26.5-21.5 48-48 48L48 512c-26.5 0-48-21.5-48-48L0 352c0-26.5 21.5-48 48-48l26.7 0zM192 408a48 48 0 1 0 -96 0 48 48 0 1 0 96 0zM478.7 278.3L440.3 368l55.7 0c6.7 0 12.6 4.1 15 10.4s.6 13.3-4.4 17.7l-128 112c-5.6 4.9-13.9 5.3-19.9 .9s-8.2-12.4-5.3-19.2L391.7 400 336 400c-6.7 0-12.6-4.1-15-10.4s-.6-13.3 4.4-17.7l128-112c5.6-4.9 13.9-5.3 19.9-.9s8.2 12.4 5.3 19.2zm-339-59.2c-6.5 6.5-17 6.5-23 0L19.9 119.2c-28-29-26.5-76.9 5-103.9c27-23.5 68.4-19 93.4 6.5l10 10.5 9.5-10.5c25-25.5 65.9-30 93.9-6.5c31 27 32.5 74.9 4.5 103.9l-96.4 99.9z",
                Title = CategoryTitle!.Trim(),
                AutoAddLinkFromSites = SiteAddresses.Any() ? SiteAddresses.ConvertToJson() : null,
            };

            UnitOfWork.CategoryRepository.Add(categoryItem);

            var fileExtensions = FileExtensions
                .Select(fe =>
                {
                    fe.CategoryId = categoryItem.Id;
                    fe.Extension = "." + fe.Extension.ToLower();
                    return fe;
                })
                .ToList();

            UnitOfWork.CategoryFileExtensionRepository.AddRange(fileExtensions);

            var saveDirectory = new CategorySaveDirectory
            {
                SaveDirectory = SaveDirectory!.Trim(),
                CategoryId = categoryItem.Id,
            };

            UnitOfWork.CategorySaveDirectoryRepository.Add(saveDirectory);
            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private string? GetGeneralDirectory()
    {
        return UnitOfWork.CategorySaveDirectoryRepository
            .Get(where: sd => sd.CategoryId == null)?
            .SaveDirectory;
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

        if (FileExtensions.Any(
                fe => fe.Extension.Equals(NewFileExtension.Extension, StringComparison.OrdinalIgnoreCase)))
            return;

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
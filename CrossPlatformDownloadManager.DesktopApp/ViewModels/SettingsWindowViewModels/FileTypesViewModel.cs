using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class FileTypesViewModel : ViewModelBase
{
    #region Private Fields

    private readonly List<CategoryFileExtensionViewModel> _dbFileExtensions;

    #endregion

    #region Properties

    private string? _searchText;

    public string? SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterFileExtensions();
        }
    }

    private ObservableCollection<CategoryFileExtensionViewModel> _fileExtensions = [];

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => _fileExtensions;
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    private CategoryFileExtensionViewModel? _selectedFileExtension;

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => _selectedFileExtension;
        set => this.RaiseAndSetIfChanged(ref _selectedFileExtension, value);
    }

    private int? _categoryId;

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoryId, value);
            LoadFileExtensionsAsync().GetAwaiter();
        }
    }

    private bool _dependsOnCategory;

    public bool DependsOnCategory
    {
        get => _dependsOnCategory;
        set
        {
            this.RaiseAndSetIfChanged(ref _dependsOnCategory, value);
            LoadFileExtensionsAsync().GetAwaiter();
        }
    }

    #endregion

    #region Commands

    public ICommand? AddNewFileTypeCommand { get; set; }

    public ICommand? EditFileTypeCommand { get; set; }

    public ICommand? DeleteFileTypeCommand { get; set; }

    #endregion

    public FileTypesViewModel(IAppService appService) : base(appService)
    {
        _dbFileExtensions = [];

        LoadFileExtensionsAsync().GetAwaiter();

        AddNewFileTypeCommand = ReactiveCommand.Create<Window?>(AddNewFileType);
        EditFileTypeCommand = ReactiveCommand.Create<Window?>(EditFileType);
        DeleteFileTypeCommand = ReactiveCommand.Create(DeleteFileType);
    }

    private async void AddNewFileType(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditFileTypeWindowViewModel(AppService) { IsEditMode = false };
            vm.SetSelectedCategory(CategoryId, DependsOnCategory);
            var window = new AddEditFileTypeWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void EditFileType(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null)
                return;

            var vm = new AddEditFileTypeWindowViewModel(AppService)
            {
                IsEditMode = true,
                CategoryFileExtensionId = SelectedFileExtension?.Id
            };

            var window = new AddEditFileTypeWindow { DataContext = vm };
            var result = await window.ShowDialog<bool?>(owner);
            if (result != true)
                return;

            await LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void DeleteFileType()
    {
        // TODO: Show message box
        try
        {
            if (SelectedFileExtension == null)
                return;

            var fileExtension = await AppService
                .UnitOfWork
                .CategoryFileExtensionRepository
                .GetAsync(where: fe => fe.Id == SelectedFileExtension.Id);

            if (fileExtension == null)
                return;

            await AppService
                .UnitOfWork
                .CategoryFileExtensionRepository
                .DeleteAsync(fileExtension);

            await AppService
                .UnitOfWork
                .SaveAsync();

            await LoadFileExtensionsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task LoadFileExtensionsAsync()
    {
        // TODO: Show message box
        try
        {
            _dbFileExtensions.Clear();

            List<CategoryFileExtension> fileExtensions;
            if (DependsOnCategory)
            {
                fileExtensions = await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .GetAllAsync(where: fe => fe.CategoryId == CategoryId,
                        orderBy: o => o.OrderBy(fe => fe.Category.Id)
                            .ThenBy(fe => fe.Extension),
                        includeProperties: ["Category"]);
            }
            else
            {
                fileExtensions = await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .GetAllAsync(orderBy: o => o.OrderBy(fe => fe.Category.Id)
                            .ThenBy(fe => fe.Extension),
                        includeProperties: ["Category"]);
            }

            var fileExtensionViewModels = AppService
                .Mapper
                .Map<List<CategoryFileExtensionViewModel>>(fileExtensions);

            _dbFileExtensions.AddRange(fileExtensionViewModels);
            FilterFileExtensions();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private void FilterFileExtensions()
    {
        var selectedFileExtension = SelectedFileExtension;
        if (SearchText.IsNullOrEmpty())
        {
            FileExtensions = _dbFileExtensions.ToObservableCollection();
        }
        else
        {
            FileExtensions = _dbFileExtensions
                .FindAll(fe =>
                    (!fe.Extension.IsNullOrEmpty() &&
                     fe.Extension!.Contains(SearchText!, StringComparison.OrdinalIgnoreCase))
                    || (!fe.Alias.IsNullOrEmpty() &&
                        fe.Alias!.Contains(SearchText!, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(fe => fe.CategoryTitle)
                .ThenBy(fe => fe.Extension)
                .ToObservableCollection();
        }

        if (selectedFileExtension != null)
            SelectedFileExtension = FileExtensions.FirstOrDefault(fe => fe.Id == selectedFileExtension.Id);
    }
}
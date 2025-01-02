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
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.SettingsWindowViewModels;

public class FileTypesViewModel : ViewModelBase
{
    #region Private Fields

    private readonly List<CategoryFileExtensionViewModel> _dbFileExtensions;

    private string? _searchText;
    private ObservableCollection<CategoryFileExtensionViewModel> _fileExtensions = [];
    private CategoryFileExtensionViewModel? _selectedFileExtension;
    private int? _categoryId;
    private bool _dependsOnCategory;

    #endregion

    #region Properties

    public string? SearchText
    {
        get => _searchText;
        set
        {
            this.RaiseAndSetIfChanged(ref _searchText, value);
            FilterFileExtensions();
        }
    }

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => _fileExtensions;
        set => this.RaiseAndSetIfChanged(ref _fileExtensions, value);
    }

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => _selectedFileExtension;
        set => this.RaiseAndSetIfChanged(ref _selectedFileExtension, value);
    }

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoryId, value);
            LoadFileExtensionsAsync().GetAwaiter();
        }
    }

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

    public ICommand AddNewFileTypeCommand { get; set; }

    public ICommand EditFileTypeCommand { get; set; }

    public ICommand DeleteFileTypeCommand { get; set; }

    #endregion

    public FileTypesViewModel(IAppService appService) : base(appService)
    {
        _dbFileExtensions = [];

        LoadFileExtensionsAsync().GetAwaiter();

        AddNewFileTypeCommand = ReactiveCommand.CreateFromTask<Window?>(AddNewFileTypeAsync);
        EditFileTypeCommand = ReactiveCommand.CreateFromTask<Window?>(EditFileTypeAsync);
        DeleteFileTypeCommand = ReactiveCommand.CreateFromTask(DeleteFileTypeAsync);
    }

    private async Task AddNewFileTypeAsync(Window? owner)
    {
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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task EditFileTypeAsync(Window? owner)
    {
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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private async Task DeleteFileTypeAsync()
    {
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
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    public async Task LoadFileExtensionsAsync()
    {
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
                        orderBy: o => o.OrderBy(fe => fe.Category!.Id).ThenBy(fe => fe.Extension),
                        includeProperties: ["Category"]);
            }
            else
            {
                fileExtensions = await AppService
                    .UnitOfWork
                    .CategoryFileExtensionRepository
                    .GetAllAsync(orderBy: o => o.OrderBy(fe => fe.Category!.Id).ThenBy(fe => fe.Extension),
                        includeProperties: ["Category"]);
            }

            var fileExtensionViewModels = AppService.Mapper.Map<List<CategoryFileExtensionViewModel>>(fileExtensions);
            _dbFileExtensions.AddRange(fileExtensionViewModels);
            FilterFileExtensions();
        }
        catch (Exception ex)
        {
            await DialogBoxManager.ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to load file extensions.");
        }
    }

    #region Helpers

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
                .FindAll(fe => fe.Extension?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) == true ||
                               fe.Alias?.Contains(SearchText!, StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(fe => fe.CategoryTitle)
                .ThenBy(fe => fe.Extension)
                .ToObservableCollection();
        }

        if (selectedFileExtension != null)
            SelectedFileExtension = FileExtensions.FirstOrDefault(fe => fe.Id == selectedFileExtension.Id);
    }

    #endregion
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.DesktopApp.Views.UserControls.MainWindow;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.MainWindow;

public class CategoriesTreeViewModel : ViewModelBase
{
    #region Private Fields

    private ObservableCollection<CategoriesTreeItemView> _categoriesTreeItemViews = [];
    private CategoriesTreeItemView? _selectedCategoriesTreeItemView;

    #endregion

    #region Properties

    public ObservableCollection<CategoriesTreeItemView> CategoriesTreeItemViews
    {
        get => _categoriesTreeItemViews;
        set => this.RaiseAndSetIfChanged(ref _categoriesTreeItemViews, value);
    }

    public CategoriesTreeItemView? SelectedCategoriesTreeItemView
    {
        get => _selectedCategoriesTreeItemView;
        set
        {
            // Clear previous selected category
            if (SelectedCategoriesTreeItemViewModel != null)
                SelectedCategoriesTreeItemViewModel.SelectedCategory = null;
            
            this.RaiseAndSetIfChanged(ref _selectedCategoriesTreeItemView, value);
            RaiseSelectedItemChanged();
        }
    }

    public CategoriesTreeItemViewModel? SelectedCategoriesTreeItemViewModel => SelectedCategoriesTreeItemView?.DataContext as CategoriesTreeItemViewModel;

    #endregion

    #region Events

    public event EventHandler? SelectedItemChanged;

    #endregion

    public CategoriesTreeViewModel(IAppService appService) : base(appService)
    {
        LoadCategories();
    }

    private void LoadCategories()
    {
        // Find previous selected category header id
        int? selectedCategoryHeaderId = null;
        if (SelectedCategoriesTreeItemView is { DataContext: CategoriesTreeItemViewModel viewModel })
            selectedCategoryHeaderId = viewModel.Id;

        RemoveChangedEvents();

        CategoriesTreeItemViews = AppService
            .CategoryService
            .CategoryHeaders
            .Select(ch => new CategoriesTreeItemView
            {
                DataContext = new CategoriesTreeItemViewModel(AppService)
                {
                    Id = ch.Id,
                    Title = ch.Title,
                    Icon = ch.Icon,
                    Categories = ch.Categories
                }
            })
            .ToObservableCollection();

        AddChangedEvents();

        if (selectedCategoryHeaderId == null)
        {
            SelectedCategoriesTreeItemView = CategoriesTreeItemViews.FirstOrDefault();
        }
        else
        {
            SelectedCategoriesTreeItemView = CategoriesTreeItemViews
                .FirstOrDefault(v => (v.DataContext as CategoriesTreeItemViewModel)?.Id == selectedCategoryHeaderId);
        }
    }

    private void RemoveChangedEvents()
    {
        if (CategoriesTreeItemViews.Count == 0)
            return;

        var viewModels = CategoriesTreeItemViews
            .Where(v => v.DataContext is CategoriesTreeItemViewModel)
            .Select(v => (v.DataContext as CategoriesTreeItemViewModel)!)
            .ToList();
        
        foreach (var viewModel in viewModels)
            viewModel.PropertyChanged -= CategoriesTreeItemViewModelOnPropertyChanged;
    }

    private void AddChangedEvents()
    {
        if (CategoriesTreeItemViews.Count == 0)
            return;

        var viewModels = CategoriesTreeItemViews
            .Where(v => v.DataContext is CategoriesTreeItemViewModel)
            .Select(v => (v.DataContext as CategoriesTreeItemViewModel)!)
            .ToList();

        foreach (var viewModel in viewModels)
            viewModel.PropertyChanged += CategoriesTreeItemViewModelOnPropertyChanged;
    }

    private void CategoriesTreeItemViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName?.Equals(nameof(CategoriesTreeItemViewModel.SelectedCategory)) != true)
            return;
        
        RaiseSelectedItemChanged();
    }

    private void RaiseSelectedItemChanged()
    {
        SelectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnCategoryServiceCategoriesChanged()
    {
        base.OnCategoryServiceCategoriesChanged();
        LoadCategories();
    }

    protected override void OnSettingsServiceDataChanged()
    {
        base.OnSettingsServiceDataChanged();
        LoadCategories();
    }
}
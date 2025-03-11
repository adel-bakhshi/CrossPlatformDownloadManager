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

    private ObservableCollection<CategoriesTreeItemViewModel> _categoriesTreeItemViewModels = [];
    private CategoriesTreeItemView? _selectedCategoriesTreeItemView;

    #endregion

    #region Properties

    public ObservableCollection<CategoriesTreeItemViewModel> CategoriesTreeItemViewModels
    {
        get => _categoriesTreeItemViewModels;
        set
        {
            this.RaiseAndSetIfChanged(ref _categoriesTreeItemViewModels, value);
            this.RaisePropertyChanged(nameof(CategoriesTreeItemViews));
        }
    }

    public CategoriesTreeItemViewModel? SelectedCategoriesTreeItemViewModel => SelectedCategoriesTreeItemView?.DataContext as CategoriesTreeItemViewModel;

    public CategoriesTreeItemView? SelectedCategoriesTreeItemView
    {
        get => _selectedCategoriesTreeItemView;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCategoriesTreeItemView, value);
            this.RaisePropertyChanged(nameof(SelectedCategoriesTreeItemViewModel));
            RaiseSelectedItemChanged();
        }
    }

    public ObservableCollection<CategoriesTreeItemView> CategoriesTreeItemViews
    {
        get
        {
            return CategoriesTreeItemViewModels
                .Select(vm => new CategoriesTreeItemView { DataContext = vm })
                .ToObservableCollection();
        }
    }

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
        var selectedCategoryHeaderId = SelectedCategoriesTreeItemViewModel?.Id;

        RemoveChangedEvents();

        CategoriesTreeItemViewModels = AppService
            .CategoryService
            .CategoryHeaders
            .Select(ch => new CategoriesTreeItemViewModel(AppService)
            {
                Id = ch.Id,
                Title = ch.Title,
                Icon = ch.Icon,
                Categories = ch.Categories
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
        if (CategoriesTreeItemViewModels.Count == 0)
            return;

        foreach (var viewModel in CategoriesTreeItemViewModels)
            viewModel.PropertyChanged -= CategoriesTreeItemViewModelOnPropertyChanged;
    }

    private void AddChangedEvents()
    {
        if (CategoriesTreeItemViewModels.Count == 0)
            return;

        foreach (var viewModel in CategoriesTreeItemViewModels)
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
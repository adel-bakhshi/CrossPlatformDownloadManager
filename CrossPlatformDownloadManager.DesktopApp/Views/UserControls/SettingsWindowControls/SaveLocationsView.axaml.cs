using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class SaveLocationsView : UserControl
{
    #region Properties

    public static readonly StyledProperty<ObservableCollection<CategoryViewModel>> CategoriesProperty =
        AvaloniaProperty.Register<SaveLocationsView, ObservableCollection<CategoryViewModel>>(
            name: nameof(Categories), defaultValue: [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableCollection<CategoryViewModel> Categories
    {
        get => GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public static readonly StyledProperty<CategoryViewModel?> SelectedCategoryProperty =
        AvaloniaProperty.Register<SaveLocationsView, CategoryViewModel?>(
            name: nameof(SelectedCategory), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public CategoryViewModel? SelectedCategory
    {
        get => GetValue(SelectedCategoryProperty);
        set => SetValue(SelectedCategoryProperty, value);
    }

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<SaveLocationsView, string?>(
            name: nameof(SearchText), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<CategoryFileExtensionViewModel>> FileExtensionsProperty =
        AvaloniaProperty.Register<SaveLocationsView, ObservableCollection<CategoryFileExtensionViewModel>>(
            name: nameof(FileExtensions), defaultValue: [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => GetValue(FileExtensionsProperty);
        set => SetValue(FileExtensionsProperty, value);
    }

    public static readonly StyledProperty<CategoryFileExtensionViewModel?> SelectedFileExtensionProperty =
        AvaloniaProperty.Register<SaveLocationsView, CategoryFileExtensionViewModel?>(
            name: nameof(SelectedFileExtension), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => GetValue(SelectedFileExtensionProperty);
        set => SetValue(SelectedFileExtensionProperty, value);
    }

    public static readonly StyledProperty<string?> SaveLocationProperty =
        AvaloniaProperty.Register<SaveLocationsView, string?>(
            name: nameof(SaveLocation), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? SaveLocation
    {
        get => GetValue(SaveLocationProperty);
        set => SetValue(SaveLocationProperty, value);
    }

    #endregion

    #region Commands

    public static readonly StyledProperty<ICommand> DeleteCategoryCommandProperty =
        AvaloniaProperty.Register<SaveLocationsView, ICommand>(
            name: nameof(DeleteCategoryCommand), defaultBindingMode: BindingMode.TwoWay);

    public ICommand DeleteCategoryCommand
    {
        get => GetValue(DeleteCategoryCommandProperty);
        set => SetValue(DeleteCategoryCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> EditCategoryCommandProperty =
        AvaloniaProperty.Register<SaveLocationsView, ICommand>(
            name: nameof(EditCategoryCommand), defaultBindingMode: BindingMode.TwoWay);

    public ICommand EditCategoryCommand
    {
        get => GetValue(EditCategoryCommandProperty);
        set => SetValue(EditCategoryCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> AddNewCategoryCommandProperty =
        AvaloniaProperty.Register<SaveLocationsView, ICommand>(
            name: nameof(AddNewCategoryCommand), defaultBindingMode: BindingMode.TwoWay);

    public ICommand AddNewCategoryCommand
    {
        get => GetValue(AddNewCategoryCommandProperty);
        set => SetValue(AddNewCategoryCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> DeleteFileTypeCommandProperty =
        AvaloniaProperty.Register<SaveLocationsView, ICommand>(
            name: nameof(DeleteFileTypeCommand), defaultBindingMode: BindingMode.TwoWay);

    public ICommand DeleteFileTypeCommand
    {
        get => GetValue(DeleteFileTypeCommandProperty);
        set => SetValue(DeleteFileTypeCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> EditFileTypeCommandProperty =
        AvaloniaProperty.Register<SaveLocationsView, ICommand>(
            name: nameof(EditFileTypeCommand), defaultBindingMode: BindingMode.TwoWay);

    public ICommand EditFileTypeCommand
    {
        get => GetValue(EditFileTypeCommandProperty);
        set => SetValue(EditFileTypeCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand> AddNewFileTypeCommandProperty =
        AvaloniaProperty.Register<SaveLocationsView, ICommand>(
            name: nameof(AddNewFileTypeCommand), defaultBindingMode: BindingMode.TwoWay);

    public ICommand AddNewFileTypeCommand
    {
        get => GetValue(AddNewFileTypeCommandProperty);
        set => SetValue(AddNewFileTypeCommandProperty, value);
    }

    #endregion

    #region Events

    public event EventHandler<EventArgs>? BrowseFolders;

    #endregion

    public SaveLocationsView()
    {
        InitializeComponent();
    }

    private void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        BrowseFolders?.Invoke(this, EventArgs.Empty);
    }
}
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using CrossPlatformDownloadManager.Data.ViewModels;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.SettingsWindowControls;

public partial class FileTypesView : UserControl
{
    #region Properties

    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<FileTypesView, string?>(
            name: nameof(SearchText), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly StyledProperty<ObservableCollection<CategoryFileExtensionViewModel>> FileExtensionsProperty =
        AvaloniaProperty.Register<FileTypesView, ObservableCollection<CategoryFileExtensionViewModel>>(
            name: nameof(FileExtensions), defaultValue: [], defaultBindingMode: BindingMode.TwoWay);

    public ObservableCollection<CategoryFileExtensionViewModel> FileExtensions
    {
        get => GetValue(FileExtensionsProperty);
        set => SetValue(FileExtensionsProperty, value);
    }

    public static readonly StyledProperty<CategoryFileExtensionViewModel?> SelectedFileExtensionProperty =
        AvaloniaProperty.Register<FileTypesView, CategoryFileExtensionViewModel?>(
            name: nameof(SelectedFileExtension), defaultValue: null, defaultBindingMode: BindingMode.TwoWay);

    public CategoryFileExtensionViewModel? SelectedFileExtension
    {
        get => GetValue(SelectedFileExtensionProperty);
        set => SetValue(SelectedFileExtensionProperty, value);
    }

    #endregion

    #region Commands

    public static readonly StyledProperty<ICommand?> AddNewFileTypeCommandProperty =
        AvaloniaProperty.Register<FileTypesView, ICommand?>(
            name: nameof(AddNewFileTypeCommand));

    public ICommand? AddNewFileTypeCommand
    {
        get => GetValue(AddNewFileTypeCommandProperty);
        set => SetValue(AddNewFileTypeCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> EditFileTypeCommandProperty =
        AvaloniaProperty.Register<FileTypesView, ICommand?>(
            name: nameof(EditFileTypeCommand));

    public ICommand? EditFileTypeCommand
    {
        get => GetValue(EditFileTypeCommandProperty);
        set => SetValue(EditFileTypeCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> DeleteFileTypeCommandProperty =
        AvaloniaProperty.Register<FileTypesView, ICommand?>(
            name: nameof(DeleteFileTypeCommand));

    public ICommand? DeleteFileTypeCommand
    {
        get => GetValue(DeleteFileTypeCommandProperty);
        set => SetValue(DeleteFileTypeCommandProperty, value);
    }

    #endregion

    public FileTypesView()
    {
        InitializeComponent();
    }
}
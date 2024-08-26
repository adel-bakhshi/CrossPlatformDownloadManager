using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;

namespace CrossPlatformDownloadManager.DesktopApp.Views.UserControls.AddNewQueueWindow;

public partial class FilesView : UserControl
{
    #region Private Fields

    private List<int>? _previousSelectedItems;

    #endregion

    #region Events

    public event EventHandler<DownloadQueueListPriorityChangedEventArgs>? DownloadQueueListPriorityChanged;

    #endregion

    #region Properties

    public static readonly StyledProperty<IEnumerable<DownloadFileViewModel>> FilesItemsSourceProperty =
        AvaloniaProperty.Register<FilesView, IEnumerable<DownloadFileViewModel>>(
            "FilesItemsSource", defaultValue: new List<DownloadFileViewModel>(),
            defaultBindingMode: BindingMode.TwoWay);

    public IEnumerable<DownloadFileViewModel> FilesItemsSource
    {
        get => GetValue(FilesItemsSourceProperty);
        set => SetValue(FilesItemsSourceProperty, value);
    }

    public static readonly StyledProperty<int> DownloadFilesCountAtTheSameTimeProperty =
        AvaloniaProperty.Register<FilesView, int>(
            "DownloadFilesCountAtTheSameTime", defaultValue: 1, defaultBindingMode: BindingMode.TwoWay);

    public int DownloadFilesCountAtTheSameTime
    {
        get => GetValue(DownloadFilesCountAtTheSameTimeProperty);
        set => SetValue(DownloadFilesCountAtTheSameTimeProperty, value);
    }

    #endregion

    public FilesView()
    {
        InitializeComponent();
    }

    private void ChangePriorityToHigherLevelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ChangeItemsPriority(true);
    }

    private void ChangePriorityToLowerLevelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ChangeItemsPriority(false);
    }

    private void FilesDataGrid_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        var propName = e.Property.Name;
        if (!propName.Equals("ItemsSource"))
            return;

        if (_previousSelectedItems == null || !_previousSelectedItems.Any())
            return;

        var items = GetValue(FilesItemsSourceProperty)
            .Where(df => _previousSelectedItems.Contains(df.Id))
            .ToList();

        foreach (var item in items)
            FilesDataGrid.SelectedItems.Add(item);

        _previousSelectedItems = null;
    }

    private void DeleteItemFromDataGridButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (FilesDataGrid.SelectedItems == null || FilesDataGrid.SelectedItems.Count == 0)
            return;

        var list = GetValue(FilesItemsSourceProperty).ToList();
        _previousSelectedItems = null;

        foreach (var item in FilesDataGrid.SelectedItems)
        {
            var downloadFile = item as DownloadFileViewModel;
            if (downloadFile == null)
                continue;

            list.Remove(downloadFile);
        }
        
        var eventArgs = new DownloadQueueListPriorityChangedEventArgs
        {
            NewList = list,
        };

        DownloadQueueListPriorityChanged?.Invoke(this, eventArgs);
    }

    private void AddItemToDataGridButton_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new NotImplementedException();
    }

    #region Helpers

    private void ChangeItemsPriority(bool isHighPriority)
    {
        if (FilesDataGrid.SelectedItems == null || FilesDataGrid.SelectedItems.Count == 0)
            return;

        var list = GetValue(FilesItemsSourceProperty).ToList();
        _previousSelectedItems = new List<int>();

        for (int i = isHighPriority ? 0 : FilesDataGrid.SelectedItems.Count - 1;
             isHighPriority ? i < FilesDataGrid.SelectedItems.Count : i >= 0;
             i = isHighPriority ? i + 1 : i - 1)
        {
            var downloadFile = FilesDataGrid.SelectedItems[i] as DownloadFileViewModel;
            if (downloadFile == null)
                continue;

            _previousSelectedItems.Add(downloadFile.Id);

            var index = list.IndexOf(downloadFile);
            if (index == (isHighPriority ? 0 : list.Count - 1))
                continue;

            if (FilesDataGrid.SelectedItems.Contains(list[isHighPriority ? index - 1 : index + 1]))
                continue;

            list.RemoveAt(index);
            list.Insert(isHighPriority ? index - 1 : index + 1, downloadFile);
        }

        var eventArgs = new DownloadQueueListPriorityChangedEventArgs
        {
            NewList = list,
        };

        DownloadQueueListPriorityChanged?.Invoke(this, eventArgs);
    }

    #endregion
}
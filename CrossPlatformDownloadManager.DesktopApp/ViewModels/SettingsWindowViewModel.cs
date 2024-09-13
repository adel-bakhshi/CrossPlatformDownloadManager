using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
    #region Properties

    private ObservableCollection<string> _tabItems = [];

    public ObservableCollection<string> TabItems
    {
        get => _tabItems;
        set => this.RaiseAndSetIfChanged(ref _tabItems, value);
    }

    private string? _selectedTabItem;

    public string? SelectedTabItem
    {
        get => _selectedTabItem;
        set
        {
            var oldValue = SelectedTabItem;
            var newValue = this.RaiseAndSetIfChanged(ref _selectedTabItem, value);
            if (!newValue.IsNullOrEmpty() && !newValue!.Equals(oldValue))
                ChangeView();
        }
    }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    
    public ICommand CancelCommand { get; }

    #endregion
    
    public SettingsWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(unitOfWork, downloadFileService)
    {
        GenerateTabs();
        
        SaveCommand = ReactiveCommand.Create<Window?>(Save);
        CancelCommand = ReactiveCommand.Create<Window?>(Cancel);
    }

    private void GenerateTabs()
    {
        var tabItems = new List<string>
        {
            "Generals",
            "File Types",
            "Save Locations",
            "Downloads",
            "Proxy",
            "Notifications",
        };
        
        TabItems = tabItems.ToObservableCollection();
        SelectedTabItem = TabItems.FirstOrDefault();
    }

    private void Save(Window? owner)
    {
        throw new System.NotImplementedException();
    }

    private void Cancel(Window? owner)
    {
        throw new System.NotImplementedException();
    }

    private void ChangeView()
    {
        
    }
}
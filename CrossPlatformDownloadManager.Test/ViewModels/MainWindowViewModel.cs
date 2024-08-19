using System.Collections.ObjectModel;
using ReactiveUI;

namespace CrossPlatformDownloadManager.Test.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<string> _comboBoxData;

    public ObservableCollection<string> ComboBoxData
    {
        get => _comboBoxData;
        set => this.RaiseAndSetIfChanged(ref _comboBoxData, value);
    }

    public MainWindowViewModel()
    {
        ComboBoxData = new ObservableCollection<string> { "KB", "MB", "GB", "TB" };
    }
}
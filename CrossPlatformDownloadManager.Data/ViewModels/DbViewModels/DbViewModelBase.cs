using CrossPlatformDownloadManager.Utils.PropertyChanged;

namespace CrossPlatformDownloadManager.Data.ViewModels.DbViewModels;

public abstract class DbViewModelBase : PropertyChangedBase
{
    public abstract void UpdateViewModel(DbViewModelBase? viewModel);
}
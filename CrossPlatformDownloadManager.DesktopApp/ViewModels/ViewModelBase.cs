using CrossPlatformDownloadManager.Data.UnitOfWork;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class ViewModelBase : ReactiveObject
{
    #region Properties

    protected IUnitOfWork UnitOfWork { get; }

    #endregion
    
    public ViewModelBase(IUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }
}
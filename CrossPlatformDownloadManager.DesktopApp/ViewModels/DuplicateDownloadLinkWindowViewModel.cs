using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DuplicateDownloadLinkWindowViewModel : ViewModelBase
{
    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand CancelCommand { get; }

    #endregion

    public DuplicateDownloadLinkWindowViewModel(IAppService appService) : base(appService)
    {
        SaveCommand = ReactiveCommand.CreateFromTask<Window?>(SaveAsync);
        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    private async Task SaveAsync(Window? owner)
    {
        try
        {
            await CancelAsync(owner);
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to save the duplicate download link option.");
        }
    }

    private async Task CancelAsync(Window? owner)
    {
        try
        {
            owner?.Close();
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to close the window.");
        }
    }
}
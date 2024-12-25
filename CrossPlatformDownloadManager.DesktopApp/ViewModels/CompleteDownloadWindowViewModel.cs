using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class CompleteDownloadWindowViewModel : ViewModelBase
{
    #region Private Fields

    private bool _dontShowThisDialogAgain;

    #endregion

    #region Properties

    public bool DontShowThisDialogAgain
    {
        get => _dontShowThisDialogAgain;
        set => this.RaiseAndSetIfChanged(ref _dontShowThisDialogAgain, value);
    }

    #endregion

    #region Commands

    public ICommand OpenFileCommand { get; }

    public ICommand OpenFolderCommand { get; }

    public ICommand CloseCommand { get; }

    #endregion

    public CompleteDownloadWindowViewModel(IAppService appService) : base(appService)
    {
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
        CloseCommand = ReactiveCommand.CreateFromTask<Window?>(CloseAsync);
    }

    private async Task OpenFileAsync()
    {
        try
        {
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to open the file.");
        }
    }

    private async Task OpenFolderAsync()
    {
        try
        {
        }
        catch (Exception ex)
        {
            await ShowErrorDialogAsync(ex);
            Log.Error(ex, "An error occured while trying to open the folder.");
        }
    }

    private async Task CloseAsync(Window? owner)
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
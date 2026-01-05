using System;
using System.Collections.ObjectModel;
using System.Linq;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.CustomEventArgs;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels.Download.Views;

public class DownloadOptionsViewModel : ViewModelBase
{
    #region Private Fields

    private bool _openFolderAfterDownloadFinished;
    private bool _exitProgramAfterDownloadFinished;
    private bool _turnOffComputerAfterDownloadFinished;
    private ObservableCollection<string> _turnOffComputerModes = [];
    private string? _selectedTurnOffComputerMode;

    #endregion

    #region Properties

    public bool OpenFolderAfterDownloadFinished
    {
        get => _openFolderAfterDownloadFinished;
        set
        {
            this.RaiseAndSetIfChanged(ref _openFolderAfterDownloadFinished, value);
            RaiseOptionsChanged();
        }
    }

    public bool ExitProgramAfterDownloadFinished
    {
        get => _exitProgramAfterDownloadFinished;
        set
        {
            this.RaiseAndSetIfChanged(ref _exitProgramAfterDownloadFinished, value);
            RaiseOptionsChanged();
        }
    }

    public bool TurnOffComputerAfterDownloadFinished
    {
        get => _turnOffComputerAfterDownloadFinished;
        set
        {
            this.RaiseAndSetIfChanged(ref _turnOffComputerAfterDownloadFinished, value);
            RaiseOptionsChanged();
        }
    }

    public ObservableCollection<string> TurnOffComputerModes
    {
        get => _turnOffComputerModes;
        set => this.RaiseAndSetIfChanged(ref _turnOffComputerModes, value);
    }

    public string? SelectedTurnOffComputerMode
    {
        get => _selectedTurnOffComputerMode;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedTurnOffComputerMode, value);
            RaiseOptionsChanged();
        }
    }

    #endregion

    #region Events

    public event EventHandler<DownloadOptionsChangedEventArgs>? OptionsChanged;

    #endregion

    public DownloadOptionsViewModel(IAppService appService) : base(appService)
    {
        TurnOffComputerModes = Constants.TurnOffComputerModes.ToObservableCollection();
        SelectedTurnOffComputerMode = TurnOffComputerModes.FirstOrDefault();
    }

    private void RaiseOptionsChanged()
    {
        OptionsChanged?.Invoke(this, new DownloadOptionsChangedEventArgs
        {
            OpenFolderAfterDownloadFinished = OpenFolderAfterDownloadFinished,
            ExitProgramAfterDownloadFinished = ExitProgramAfterDownloadFinished,
            TurnOffComputerAfterDownloadFinished = TurnOffComputerAfterDownloadFinished,
            TurnOffComputerMode = SelectedTurnOffComputerMode
        });
    }
}
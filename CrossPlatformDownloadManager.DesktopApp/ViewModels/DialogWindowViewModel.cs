using Avalonia.Controls;
using CrossPlatformDownloadManager.Data.Services.AppService;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DialogWindowViewModel : ViewModelBase
{
    #region Private Fields

    private string _dialogHeader = string.Empty;
    private string _dialogMessage = string.Empty;
    private DialogButtons _dialogButtons = DialogButtons.Ok;
    private DialogType _dialogType = DialogType.Information;

    #endregion

    #region Properties

    public string DialogHeader
    {
        get => _dialogHeader;
        set => this.RaiseAndSetIfChanged(ref _dialogHeader, value);
    }

    public string DialogMessage
    {
        get => _dialogMessage;
        set => this.RaiseAndSetIfChanged(ref _dialogMessage, value);
    }

    public DialogButtons DialogButtons
    {
        get => _dialogButtons;
        set
        {
            this.RaiseAndSetIfChanged(ref _dialogButtons, value);
            this.RaisePropertyChanged(nameof(IsOkButtonVisible));
            this.RaisePropertyChanged(nameof(IsYesButtonVisible));
            this.RaisePropertyChanged(nameof(IsNoButtonVisible));
            this.RaisePropertyChanged(nameof(IsCancelButtonVisible));
        }
    }

    public bool IsOkButtonVisible => DialogButtons is DialogButtons.Ok or DialogButtons.OkCancel;

    public bool IsYesButtonVisible => DialogButtons is DialogButtons.YesNo or DialogButtons.YesNoCancel;

    public bool IsNoButtonVisible => DialogButtons is DialogButtons.YesNo or DialogButtons.YesNoCancel;

    public bool IsCancelButtonVisible => DialogButtons is DialogButtons.OkCancel or DialogButtons.YesNoCancel;

    public DialogType DialogType
    {
        get => _dialogType;
        set
        {
            this.RaiseAndSetIfChanged(ref _dialogType, value);
            this.RaisePropertyChanged(nameof(IsInformationDialog));
            this.RaisePropertyChanged(nameof(IsWarningDialog));
            this.RaisePropertyChanged(nameof(IsSuccessDialog));
            this.RaisePropertyChanged(nameof(IsDangerDialog));
        }
    }

    public bool IsInformationDialog => DialogType is DialogType.Information;

    public bool IsWarningDialog => DialogType is DialogType.Warning;

    public bool IsSuccessDialog => DialogType is DialogType.Success;

    public bool IsDangerDialog => DialogType is DialogType.Danger;

    public DialogResult DialogResult { get; set; } = DialogResult.None;

    #endregion

    public DialogWindowViewModel(IAppService appService) : base(appService)
    {
    }

    public void SendDialogResult(Window? owner, DialogResult dialogResult)
    {
        if (owner == null)
            return;

        DialogResult = dialogResult;
        owner.Close();
    }
}
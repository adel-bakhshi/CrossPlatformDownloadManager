using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Enums;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Models;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.ViewModels;

public class DialogWindowViewModel : ViewModelBase
{
    #region Private Fields

    private string _dialogHeader = string.Empty;
    private string _dialogMessage = string.Empty;
    private DialogButtons _dialogButtons = DialogButtons.Ok;
    private DialogType _dialogType = DialogType.Information;
    private bool _copyToClipboardButtonIsVisible;
    private string? _infoMessage;
    private string _okButtonText = string.Empty;
    private string _yesButtonText = string.Empty;
    private string _noButtonText = string.Empty;
    private string _cancelButtonText = string.Empty;
    private HorizontalAlignment _titleAlignment = HorizontalAlignment.Left;
    private FlowDirection _flowDirection = FlowDirection.RightToLeft;

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

    public bool CopyToClipboardButtonIsVisible
    {
        get => _copyToClipboardButtonIsVisible;
        set => this.RaiseAndSetIfChanged(ref _copyToClipboardButtonIsVisible, value);
    }

    public string? InfoMessage
    {
        get => _infoMessage;
        set
        {
            this.RaiseAndSetIfChanged(ref _infoMessage, value);
            this.RaisePropertyChanged(nameof(InfoMessageIsVisible));
        }
    }

    public bool InfoMessageIsVisible => !InfoMessage.IsStringNullOrEmpty();

    public string OkButtonText
    {
        get => _okButtonText;
        set => this.RaiseAndSetIfChanged(ref _okButtonText, value);
    }

    public string YesButtonText
    {
        get => _yesButtonText;
        set => this.RaiseAndSetIfChanged(ref _yesButtonText, value);
    }

    public string NoButtonText
    {
        get => _noButtonText;
        set => this.RaiseAndSetIfChanged(ref _noButtonText, value);
    }

    public string CancelButtonText
    {
        get => _cancelButtonText;
        set => this.RaiseAndSetIfChanged(ref _cancelButtonText, value);
    }

    public HorizontalAlignment TitleAlignment
    {
        get => _titleAlignment;
        set => this.RaiseAndSetIfChanged(ref _titleAlignment, value);
    }

    public FlowDirection FlowDirection
    {
        get => _flowDirection;
        set => this.RaiseAndSetIfChanged(ref _flowDirection, value);
    }

    #endregion

    #region Commands

    public ICommand CopyToClipboardCommand { get; }

    #endregion

    public DialogWindowViewModel(IAppService appService, DialogOptions options) : base(appService)
    {
        // Initialize required properties using options
        CopyToClipboardButtonIsVisible = options.ShowCopyToClipboardButton;
        InfoMessage = options.InfoMessage;
        OkButtonText = options.OkButtonText;
        YesButtonText = options.YesButtonText;
        NoButtonText = options.NoButtonText;
        CancelButtonText = options.CancelButtonText;
        TitleAlignment = options.CenterTitle ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        FlowDirection = options.FlowDirection;

        CopyToClipboardCommand = ReactiveCommand.CreateFromTask<Window?>(CopyToClipboardAsync);
    }

    private async Task CopyToClipboardAsync(Window? owner)
    {
        try
        {
            if (owner?.Clipboard == null)
                return;

            await owner.Clipboard.SetTextAsync(DialogMessage);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while trying to copy dialog message to clipboard. Error message: {ErrorMessage}", ex.Message);
        }
    }

    public void SendDialogResult(Window? owner, DialogResult dialogResult)
    {
        if (owner == null)
            return;

        DialogResult = dialogResult;
        owner.Close();
    }
}
using Avalonia.Media;

namespace CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox.Models;

/// <summary>
/// Options class for DialogBox.
/// </summary>
public class DialogOptions
{
    #region Properties

    /// <summary>
    /// Gets or sets a value that indicates whether the copy to clipboard button should be shown.
    /// </summary>
    public bool ShowCopyToClipboardButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value that indicates the info message that will be shown in the dialog box.
    /// </summary>
    public string? InfoMessage { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the MainWindow should be used as the owner of the dialog box.
    /// </summary>
    public bool UseMainWindowAsOwner { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates the text of the OK button.
    /// </summary>
    public string OkButtonText { get; set; } = "Ok";

    /// <summary>
    /// Gets or sets a value that indicates the text of the cancel button.
    /// </summary>
    public string CancelButtonText { get; set; } = "Cancel";

    /// <summary>
    /// Gets or sets a value that indicates the text of the yes button.
    /// </summary>
    public string YesButtonText { get; set; } = "Yes";

    /// <summary>
    /// Gets or sets a value that indicates the text of the no button.
    /// </summary>
    public string NoButtonText { get; set; } = "No";

    /// <summary>
    /// Gets or sets a value that indicates whether the title should be centered.
    /// </summary>
    public bool CenterTitle { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates the flow direction of the dialog box window (RightToLeft or LeftToRight).
    /// </summary>
    public FlowDirection FlowDirection { get; set; } = FlowDirection.LeftToRight;

    #endregion
}
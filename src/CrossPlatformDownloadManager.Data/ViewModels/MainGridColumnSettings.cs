using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels;

/// <summary>
/// Represents the column settings for the main data grid.
/// </summary>
public class MainGridColumnSettings : PropertyChangedBase
{
    #region Private Fields

    // Backing fields for properties
    private bool _isNameColumnVisible = true;
    private bool _isUrlColumnVisible;
    private bool _isDescriptionColumnVisible;
    private bool _isQueueColumnVisible = true;
    private bool _isPriorityInQueueColumnVisible;
    private bool _isSizeColumnVisible = true;
    private bool _isStatusColumnVisible = true;
    private bool _isTimeLeftColumnVisible = true;
    private bool _isElapsedTimeColumnVisible;
    private bool _isTransferRateColumnVisible = true;
    private bool _isLastTryDateColumnVisible = true;
    private bool _isDateAddedColumnVisible = true;
    private bool _isSaveLocationColumnVisible;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether the Name column is visible.
    /// </summary>
    [JsonProperty("isNameColumnVisible")]
    public bool IsNameColumnVisible
    {
        get => _isNameColumnVisible;
        set => SetField(ref _isNameColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the URL column is visible.
    /// </summary>
    [JsonProperty("isUrlColumnVisible")]
    public bool IsUrlColumnVisible
    {
        get => _isUrlColumnVisible;
        set => SetField(ref _isUrlColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Description column is visible.
    /// </summary>
    [JsonProperty("isDescriptionColumnVisible")]
    public bool IsDescriptionColumnVisible
    {
        get => _isDescriptionColumnVisible;
        set => SetField(ref _isDescriptionColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Queue column is visible.
    /// </summary>
    [JsonProperty("isQueueColumnVisible")]
    public bool IsQueueColumnVisible
    {
        get => _isQueueColumnVisible;
        set => SetField(ref _isQueueColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Priority In Queue column is visible.
    /// </summary>
    [JsonProperty("isPriorityInQueueColumnVisible")]
    public bool IsPriorityInQueueColumnVisible
    {
        get => _isPriorityInQueueColumnVisible;
        set => SetField(ref _isPriorityInQueueColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Size column is visible.
    /// </summary>
    [JsonProperty("isSizeColumnVisible")]
    public bool IsSizeColumnVisible
    {
        get => _isSizeColumnVisible;
        set => SetField(ref _isSizeColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Status column is visible.
    /// </summary>
    [JsonProperty("isStatusColumnVisible")]
    public bool IsStatusColumnVisible
    {
        get => _isStatusColumnVisible;
        set => SetField(ref _isStatusColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Time Left column is visible.
    /// </summary>
    [JsonProperty("isTimeLeftColumnVisible")]
    public bool IsTimeLeftColumnVisible
    {
        get => _isTimeLeftColumnVisible;
        set => SetField(ref _isTimeLeftColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Elapsed Time column is visible.
    /// </summary>
    [JsonProperty("isElapsedTimeColumnVisible")]
    public bool IsElapsedTimeColumnVisible
    {
        get => _isElapsedTimeColumnVisible;
        set => SetField(ref _isElapsedTimeColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Transfer Rate column is visible.
    /// </summary>
    [JsonProperty("isTransferRateColumnVisible")]
    public bool IsTransferRateColumnVisible
    {
        get => _isTransferRateColumnVisible;
        set => SetField(ref _isTransferRateColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Last Try Date column is visible.
    /// </summary>
    [JsonProperty("isLastTryDateColumnVisible")]
    public bool IsLastTryDateColumnVisible
    {
        get => _isLastTryDateColumnVisible;
        set => SetField(ref _isLastTryDateColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Date Added column is visible.
    /// </summary>
    [JsonProperty("isDateAddedColumnVisible")]
    public bool IsDateAddedColumnVisible
    {
        get => _isDateAddedColumnVisible;
        set => SetField(ref _isDateAddedColumnVisible, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the Save Location column is visible.
    /// </summary>
    [JsonProperty("isSaveLocationColumnVisible")]
    public bool IsSaveLocationColumnVisible
    {
        get => _isSaveLocationColumnVisible;
        set => SetField(ref _isSaveLocationColumnVisible, value);
    }

    #endregion
}
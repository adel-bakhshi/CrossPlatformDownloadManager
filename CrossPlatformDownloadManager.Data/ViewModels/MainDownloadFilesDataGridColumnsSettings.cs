using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class MainDownloadFilesDataGridColumnsSettings : PropertyChangedBase
{
    #region Private Fields

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

    [JsonProperty("isNameColumnVisible")]
    public bool IsNameColumnVisible
    {
        get => _isNameColumnVisible;
        set => SetField(ref _isNameColumnVisible, value);
    }

    [JsonProperty("isUrlColumnVisible")]
    public bool IsUrlColumnVisible
    {
        get => _isUrlColumnVisible;
        set => SetField(ref _isUrlColumnVisible, value);
    }

    [JsonProperty("isDescriptionColumnVisible")]
    public bool IsDescriptionColumnVisible
    {
        get => _isDescriptionColumnVisible;
        set => SetField(ref _isDescriptionColumnVisible, value);
    }

    [JsonProperty("isQueueColumnVisible")]
    public bool IsQueueColumnVisible
    {
        get => _isQueueColumnVisible;
        set => SetField(ref _isQueueColumnVisible, value);
    }

    [JsonProperty("isPriorityInQueueColumnVisible")]
    public bool IsPriorityInQueueColumnVisible
    {
        get => _isPriorityInQueueColumnVisible;
        set => SetField(ref _isPriorityInQueueColumnVisible, value);
    }

    [JsonProperty("isSizeColumnVisible")]
    public bool IsSizeColumnVisible
    {
        get => _isSizeColumnVisible;
        set => SetField(ref _isSizeColumnVisible, value);
    }

    [JsonProperty("isStatusColumnVisible")]
    public bool IsStatusColumnVisible
    {
        get => _isStatusColumnVisible;
        set => SetField(ref _isStatusColumnVisible, value);
    }

    [JsonProperty("isTimeLeftColumnVisible")]
    public bool IsTimeLeftColumnVisible
    {
        get => _isTimeLeftColumnVisible;
        set => SetField(ref _isTimeLeftColumnVisible, value);
    }

    [JsonProperty("isElapsedTimeColumnVisible")]
    public bool IsElapsedTimeColumnVisible
    {
        get => _isElapsedTimeColumnVisible;
        set => SetField(ref _isElapsedTimeColumnVisible, value);
    }

    [JsonProperty("isTransferRateColumnVisible")]
    public bool IsTransferRateColumnVisible
    {
        get => _isTransferRateColumnVisible;
        set => SetField(ref _isTransferRateColumnVisible, value);
    }

    [JsonProperty("isLastTryDateColumnVisible")]
    public bool IsLastTryDateColumnVisible
    {
        get => _isLastTryDateColumnVisible;
        set => SetField(ref _isLastTryDateColumnVisible, value);
    }

    [JsonProperty("isDateAddedColumnVisible")]
    public bool IsDateAddedColumnVisible
    {
        get => _isDateAddedColumnVisible;
        set => SetField(ref _isDateAddedColumnVisible, value);
    }

    [JsonProperty("isSaveLocationColumnVisible")]
    public bool IsSaveLocationColumnVisible
    {
        get => _isSaveLocationColumnVisible;
        set => SetField(ref _isSaveLocationColumnVisible, value);
    }

    #endregion
}
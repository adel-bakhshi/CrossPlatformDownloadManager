using CrossPlatformDownloadManager.Utils.PropertyChanged;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DaysOfWeekViewModel : PropertyChangedBase
{
    #region Private Fields

    private bool _saturday;
    private bool _sunday;
    private bool _monday;
    private bool _tuesday;
    private bool _wednesday;
    private bool _thursday;
    private bool _friday;

    #endregion

    #region Properties

    [JsonProperty("saturday")]
    public bool Saturday
    {
        get => _saturday;
        set => SetField(ref _saturday, value);
    }

    [JsonProperty("sunday")]
    public bool Sunday
    {
        get => _sunday;
        set => SetField(ref _sunday, value);
    }

    [JsonProperty("monday")]
    public bool Monday
    {
        get => _monday;
        set => SetField(ref _monday, value);
    }

    [JsonProperty("tuesday")]
    public bool Tuesday
    {
        get => _tuesday;
        set => SetField(ref _tuesday, value);
    }

    [JsonProperty("wednesday")]
    public bool Wednesday
    {
        get => _wednesday;
        set => SetField(ref _wednesday, value);
    }

    [JsonProperty("thursday")]
    public bool Thursday
    {
        get => _thursday;
        set => SetField(ref _thursday, value);
    }

    [JsonProperty("friday")]
    public bool Friday
    {
        get => _friday;
        set => SetField(ref _friday, value);
    }
    
    public bool IsAnyDaySelected => Saturday || Sunday || Monday || Tuesday || Wednesday || Thursday || Friday;

    #endregion
}
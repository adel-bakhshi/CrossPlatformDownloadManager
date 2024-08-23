using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace CrossPlatformDownloadManager.Data.ViewModels;

public class DaysOfWeekViewModel : INotifyPropertyChanged
{
    private bool _saturday;

    [JsonProperty("saturday")]
    public bool Saturday
    {
        get => _saturday;
        set => SetField(ref _saturday, value);
    }

    private bool _sunday;

    [JsonProperty("sunday")]
    public bool Sunday
    {
        get => _sunday;
        set => SetField(ref _sunday, value);
    }

    private bool _monday;

    [JsonProperty("monday")]
    public bool Monday
    {
        get => _monday;
        set => SetField(ref _monday, value);
    }

    private bool _tuesday;

    [JsonProperty("tuesday")]
    public bool Tuesday
    {
        get => _tuesday;
        set => SetField(ref _tuesday, value);
    }

    private bool _wednesday;

    [JsonProperty("wednesday")]
    public bool Wednesday
    {
        get => _wednesday;
        set => SetField(ref _wednesday, value);
    }

    private bool _thursday;

    [JsonProperty("thursday")]
    public bool Thursday
    {
        get => _thursday;
        set => SetField(ref _thursday, value);
    }

    private bool _friday;

    [JsonProperty("friday")]
    public bool Friday
    {
        get => _friday;
        set => SetField(ref _friday, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
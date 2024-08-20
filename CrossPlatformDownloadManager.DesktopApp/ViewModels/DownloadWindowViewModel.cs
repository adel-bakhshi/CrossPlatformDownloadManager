using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
    #region Properties

    private bool _showStatusView;

    public bool ShowStatusView
    {
        get => _showStatusView;
        set => this.RaiseAndSetIfChanged(ref _showStatusView, value);
    }

    private bool _showSpeedLimiterView;

    public bool ShowSpeedLimiterView
    {
        get => _showSpeedLimiterView;
        set => this.RaiseAndSetIfChanged(ref _showSpeedLimiterView, value);
    }

    private bool _showOptionsView;

    public bool ShowOptionsView
    {
        get => _showOptionsView;
        set => this.RaiseAndSetIfChanged(ref _showOptionsView, value);
    }

    private ObservableCollection<string> _speedLimiterUnits;

    public ObservableCollection<string> SpeedLimiterUnits
    {
        get => _speedLimiterUnits;
        set => this.RaiseAndSetIfChanged(ref _speedLimiterUnits, value);
    }

    private ObservableCollection<string> _optionsTurnOffModes;

    public ObservableCollection<string> OptionsTurnOffModes
    {
        get => _optionsTurnOffModes;
        set => this.RaiseAndSetIfChanged(ref _optionsTurnOffModes, value);
    }

    private ObservableCollection<ChunkDataViewModel> _chunksData;

    public ObservableCollection<ChunkDataViewModel> ChunksData
    {
        get => _chunksData;
        set => this.RaiseAndSetIfChanged(ref _chunksData, value);
    }

    #endregion

    #region Commands

    public ICommand ChangeViewCommand { get; }

    public ICommand SpeedLimiterStateChangedCommand { get; }

    public ICommand? OptionsStateChangedCommand { get; set; }

    #endregion

    public DownloadWindowViewModel()
    {
        ShowStatusView = true;
        SpeedLimiterUnits = new ObservableCollection<string> { "KB", "MB" };
        OptionsTurnOffModes = new ObservableCollection<string> { "Shut down", "Sleep", "Hibernate" };
        ChunksData = GetChunksData();

        ChangeViewCommand = ReactiveCommand.Create<object?>(ChangeView);
        SpeedLimiterStateChangedCommand =
            ReactiveCommand.Create<DownloadSpeedLimiterViewEventArgs>(SpeedLimiterStateChanged);
        OptionsStateChangedCommand = ReactiveCommand.Create<DownloadOptionsViewEventArgs>(OptionsStateChanged);
    }

    private ObservableCollection<ChunkDataViewModel> GetChunksData()
    {
        return new ObservableCollection<ChunkDataViewModel>
        {
            new ChunkDataViewModel { ChunkIndex = 0, TotalSize = 409600, DownloadedSize = 112230.4, Info = "Receiving..." },
            new ChunkDataViewModel { ChunkIndex = 1, TotalSize = 409600, DownloadedSize = 130252.8, Info = "Receiving..." },
            new ChunkDataViewModel { ChunkIndex = 2, TotalSize = 409600, DownloadedSize = 67174.4, Info = "Receiving..." },
            new ChunkDataViewModel { ChunkIndex = 3, TotalSize = 409600, DownloadedSize = 200704, Info = "Receiving..." },
            new ChunkDataViewModel { ChunkIndex = 4, TotalSize = 409600, DownloadedSize = 90931.2, Info = "Receiving..." },
        };
    }

    private void ChangeView(object? obj)
    {
        var buttonName = (obj as ToggleButton)?.Name;
        switch (buttonName)
        {
            case "BtnStatus":
            {
                ChangeViewsVisibility(nameof(ShowStatusView));
                break;
            }

            case "BtnSpeedLimiter":
            {
                ChangeViewsVisibility(nameof(ShowSpeedLimiterView));
                break;
            }

            case "BtnOptions":
            {
                ChangeViewsVisibility(nameof(ShowOptionsView));
                break;
            }
        }
    }

    private void ChangeViewsVisibility(string propName)
    {
        ShowStatusView = ShowSpeedLimiterView = ShowOptionsView = false;
        this.GetType().GetProperty(propName)?.SetValue(this, true);
    }

    private void OptionsStateChanged(DownloadOptionsViewEventArgs obj)
    {
    }

    private void SpeedLimiterStateChanged(DownloadSpeedLimiterViewEventArgs obj)
    {
    }
}
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using CrossPlatformDownloadManager.Data.ViewModels.EventArgs;
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

    #endregion

    #region Commands

    public ICommand ChangeViewCommand { get; }
    public ICommand SpeedLimiterStateChangedCommand { get; }

    #endregion

    public DownloadWindowViewModel()
    {
        ShowStatusView = true;
        SpeedLimiterUnits = new ObservableCollection<string> { "KB", "MB" };

        ChangeViewCommand = ReactiveCommand.Create<object?>(ChangeView);
        SpeedLimiterStateChangedCommand = ReactiveCommand.Create<SpeedLimiterEventArgs>((value) =>
        {
            Console.WriteLine(
                $"Speed limiter is: {value.Enabled}, and Speed is: {value.Speed}, and Unit is: {value.Unit}.");
        });
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
}
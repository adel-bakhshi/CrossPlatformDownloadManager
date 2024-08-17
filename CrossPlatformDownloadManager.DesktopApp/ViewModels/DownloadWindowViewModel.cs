using System.Windows.Input;
using Avalonia.Controls.Primitives;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class DownloadWindowViewModel : ViewModelBase
{
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
    
    public ICommand ChangeViewCommand { get; }

    public DownloadWindowViewModel()
    {
        ShowStatusView = true;

        ChangeViewCommand = ReactiveCommand.Create<object?>(ChangeView);
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
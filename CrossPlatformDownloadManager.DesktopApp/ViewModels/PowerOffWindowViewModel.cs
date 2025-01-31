using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.DialogBox;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.PlatformManager;
using CrossPlatformDownloadManager.DesktopApp.Infrastructure.Services.AppService;
using ReactiveUI;
using Serilog;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class PowerOffWindowViewModel : ViewModelBase
{
    #region Private Fields

    private readonly DispatcherTimer _reverseTimer;
    private readonly string _powerOffMode;
    private readonly TimeSpan _duration;
    private TimeSpan _remaining;

    private string _title = string.Empty;
    private string _timeRemaining = string.Empty;
    private string _timeUnit = string.Empty;
    private double _progress;
    private string _message = string.Empty;

    #endregion

    #region Properties

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public string TimeRemaining
    {
        get => _timeRemaining;
        set => this.RaiseAndSetIfChanged(ref _timeRemaining, value);
    }

    public string TimeUnit
    {
        get => _timeUnit;
        set => this.RaiseAndSetIfChanged(ref _timeUnit, value);
    }

    public double Progress
    {
        get => _progress;
        set => this.RaiseAndSetIfChanged(ref _progress, value);
    }

    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    #endregion

    #region Commands

    public ICommand CancelCommand { get; }

    #endregion

    public PowerOffWindowViewModel(IAppService appService, string powerOffMode, TimeSpan duration) : base(appService)
    {
        _duration = _remaining = duration;
        _powerOffMode = powerOffMode;
        _reverseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _reverseTimer.Tick += ReverseTimerOnTick;

        InitializePowerOff();

        CancelCommand = ReactiveCommand.CreateFromTask<Window?>(CancelAsync);
    }

    public void StopReverseTimer()
    {
        // Stop reverse timer
        _reverseTimer.Stop();
        _reverseTimer.Tick -= ReverseTimerOnTick;
    }

    private void InitializePowerOff()
    {
        if (_duration.TotalSeconds < 1)
            throw new InvalidOperationException("Duration must be at least 1 second.");

        switch (_powerOffMode.ToLower())
        {
            case "shut down":
            case "sleep":
            case "hibernate":
            {
                Title = $"Time remaining until {_powerOffMode.ToLower()}:";
                Message = $"Your computer is scheduled to {_powerOffMode.ToLower()} when the time limit is reached. If you wish to keep working, " +
                          $"please press the 'Cancel' button to postpone the {_powerOffMode.ToLower()}.";

                break;
            }

            default:
                throw new InvalidOperationException("Power off mode is invalid.");
        }

        CalculateTimeRemaining();
        _reverseTimer.Start();
    }

    private static async Task CancelAsync(Window? owner)
    {
        try
        {
            owner?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occured while trying to cancel. Error message: {ErrorMessage}", ex.Message);
            await DialogBoxManager.ShowErrorDialogAsync(ex);
        }
    }

    private void ReverseTimerOnTick(object? sender, EventArgs e)
    {
        CalculateTimeRemaining();
    }

    private void CalculateTimeRemaining()
    {
        _remaining -= _reverseTimer.Interval;
        var timeUnit = _remaining.TotalSeconds > 1 ? "seconds" : "second";
        var progress = 100 - (_duration.TotalSeconds - _remaining.TotalSeconds) / _duration.TotalSeconds * 100;

        // Stop timer when remaining time is less than 1 second
        if (_remaining.TotalSeconds < 1)
            _reverseTimer.Stop();

        TimeRemaining = _remaining.TotalSeconds.ToString("00");
        TimeUnit = timeUnit;
        Progress = _remaining.TotalSeconds > 0 ? progress : 0;

        // Check if timer is enabled
        if (_remaining.TotalSeconds > 0)
            return;

        // Power off computer
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                switch (_powerOffMode.ToLower())
                {
                    case "shut down":
                    {
                        PlatformSpecificManager.Shutdown();
                        break;
                    }

                    case "sleep":
                    {
                        PlatformSpecificManager.Sleep();
                        break;
                    }

                    case "hibernate":
                    {
                        PlatformSpecificManager.Hibernate();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured while trying to power off the computer. Error message: {ErrorMessage}", ex.Message);
                await DialogBoxManager.ShowErrorDialogAsync(ex);
            }
        });
    }
}
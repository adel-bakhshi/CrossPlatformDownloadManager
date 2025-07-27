using Avalonia.Threading;
using Serilog;

namespace CrossPlatformDownloadManager.Utils;

/// <summary>
/// Debounce actions to avoid multiple calls in a short period of time.
/// </summary>
public class Debouncer
{
    #region Private fields

    /// <summary>
    /// The delay before the action is executed.
    /// </summary>
    private readonly TimeSpan _delay;

    /// <summary>
    /// The timer used to debounce the action.
    /// </summary>
    private DispatcherTimer? _debouncerTimer;

    /// <summary>
    /// The action to execute.
    /// </summary>
    private object? _action;

    /// <summary>
    /// The completion source to check the async action completion.
    /// </summary>
    private TaskCompletionSource<bool>? _actionCompletionSource;

    /// <summary>
    /// Indicates if the action is async.
    /// </summary>
    private bool _isAsync;

    #endregion Private fields

    /// <summary>
    /// Creates a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    /// <param name="delay">The delay before the action is executed.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the delay is less than or equal to 0.</exception>
    public Debouncer(TimeSpan delay)
    {
        if (delay.TotalMicroseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(delay), "Debouncer delay should be greater than 0.");

        _delay = delay;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Debouncer"/> class.
    /// </summary>
    /// <param name="delay">The delay before the action is executed. Default is 1000 milliseconds.</param>
    public Debouncer(int delay = 1000) : this(TimeSpan.FromMilliseconds(delay))
    {
    }

    /// <summary>
    /// Executes the action after the delay.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <exception cref="ArgumentNullException">Thrown when the action is null.</exception>
    public void Run(Action? action)
    {
        ClearTimer();
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _isAsync = false;
        _debouncerTimer = CreateTimer();
    }

    /// <summary>
    /// Executes the async action after the delay.
    /// </summary>
    /// <param name="asyncAction">The async action to execute.</param>
    /// <param name="waitToFinish">Indicates if the method should wait for the async action to finish.</param>
    /// <returns>A task that represents the completion of the async action.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the async action is null.</exception>
    public Task RunAsync(Func<Task>? asyncAction, bool waitToFinish = true)
    {
        ClearTimer();
        _action = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
        _isAsync = true;
        _debouncerTimer = CreateTimer();
        return waitToFinish ? _actionCompletionSource!.Task : Task.CompletedTask;
    }

    #region Event handlers

    /// <summary>
    /// Handles the tick event of the debouncer timer and executes the action.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private async void DebouncerTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            ClearTimer();
            // Check if the action is a Func<Task> or an Action and execute it
            if (_isAsync)
            {
                await ((Func<Task>)_action!).Invoke();
                _actionCompletionSource?.TrySetResult(true);
            }
            else
            {
                ((Action)_action!).Invoke();
            }
        }
        catch (Exception ex)
        {
            if (_isAsync)
                _actionCompletionSource?.TrySetException(ex);

            Log.Error(ex, "An error occurred while executing the debounced action.");
        }
    }

    #endregion Event handlers

    #region Helpers

    /// <summary>
    /// Clears the debouncer timer.
    /// </summary>
    private void ClearTimer()
    {
        if (_debouncerTimer == null)
            return;

        _debouncerTimer.Stop();
        _debouncerTimer.Tick -= DebouncerTimerOnTick;
        _debouncerTimer = null;
    }

    /// <summary>
    /// Creates a new debouncer timer.
    /// </summary>
    /// <returns>The new debouncer timer.</returns>
    private DispatcherTimer CreateTimer()
    {
        if (_isAsync)
            _actionCompletionSource = new TaskCompletionSource<bool>();

        var timer = new DispatcherTimer { Interval = _delay };
        timer.Tick += DebouncerTimerOnTick;
        timer.Start();
        return timer;
    }

    #endregion Helpers
}
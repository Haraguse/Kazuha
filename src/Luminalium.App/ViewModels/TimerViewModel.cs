using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Luminalium.App.Services;
using Luminalium.Core.Localization;

namespace Luminalium.App.ViewModels;

/// <summary>
/// Countdown timer view model. Driven by an injectable <see cref="IClockService"/>
/// so tests can advance time deterministically; the countdown task is cancelled
/// on termination so no timer thread outlives the plugin window.
/// </summary>
public partial class TimerViewModel : ObservableObject
{
    private readonly IClockService _clock;
    private readonly IAudioCueService _audioCue;
    private readonly INotificationCueService _notificationCue;
    private CancellationTokenSource? _countdownCts;

    public TimerViewModel(
        ILocalizationService? localization = null,
        IClockService? clock = null,
        IAudioCueService? audioCue = null,
        INotificationCueService? notificationCue = null)
    {
        Localization = localization ?? new LocalizationService();
        _clock = clock ?? new SystemClockService();
        _audioCue = audioCue ?? NullAudioCueService.Instance;
        _notificationCue = notificationCue ?? NullNotificationCueService.Instance;
    }

    public ILocalizationService Localization { get; }

    public string WindowTitle => Localization["Timer.Window.Title"];

    [ObservableProperty]
    private string _inputText = "00:00:03";

    [ObservableProperty]
    private string _displayText = "00:00:00";

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isFinished;

    private int _totalSeconds;
    private int _remainingSeconds;

    public int TotalSeconds => _totalSeconds;

    public int RemainingSeconds => _remainingSeconds;

    public bool CanPause => IsRunning;

    [RelayCommand]
    private async Task StartAsync()
    {
        if (IsRunning)
        {
            return;
        }

        if (!TryParseInput(InputText, out var totalSeconds))
        {
            StatusText = Localization["Timer.Status.InvalidFormat"];
            return;
        }

        _totalSeconds = totalSeconds;
        _remainingSeconds = totalSeconds;
        IsFinished = false;
        StatusText = string.Empty;
        UpdateDisplay();
        IsRunning = true;
        OnPropertyChanged(nameof(CanPause));

        var cts = new CancellationTokenSource();
        _countdownCts = cts;
        try
        {
            await RunCountdownAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Pause or termination; swallow so the task ends cleanly.
        }
        finally
        {
            if (ReferenceEquals(_countdownCts, cts))
            {
                _countdownCts = null;
            }

            cts.Dispose();
        }
    }

    [RelayCommand]
    private void Pause()
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        OnPropertyChanged(nameof(CanPause));
        StatusText = Localization["Timer.Status.Paused"];
        _countdownCts?.Cancel();
    }

    [RelayCommand]
    private void Reset()
    {
        IsRunning = false;
        IsFinished = false;
        _countdownCts?.Cancel();
        _countdownCts = null;

        if (TryParseInput(InputText, out var totalSeconds))
        {
            _totalSeconds = totalSeconds;
            _remainingSeconds = totalSeconds;
        }
        else
        {
            _totalSeconds = 0;
            _remainingSeconds = 0;
        }

        StatusText = string.Empty;
        UpdateDisplay();
        OnPropertyChanged(nameof(CanPause));
    }

    /// <summary>
    /// Cancels the running countdown so no timer task outlives the window.
    /// </summary>
    public void Terminate()
    {
        _countdownCts?.Cancel();
        _countdownCts = null;
        IsRunning = false;
        OnPropertyChanged(nameof(CanPause));
    }

    private async Task RunCountdownAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _remainingSeconds > 0)
            {
                await _clock.DelayAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                if (_remainingSeconds > 0)
                {
                    _remainingSeconds--;
                    UpdateDisplay();
                }
            }

            if (!cancellationToken.IsCancellationRequested && _remainingSeconds == 0)
            {
                IsRunning = false;
                IsFinished = true;
                StatusText = Localization["Timer.Status.Finished"];
                _audioCue.PlayFinishedCue();
                _notificationCue.ShowFinished(
                    Localization["Timer.Notification.Title"],
                    Localization["Timer.Notification.Body"]);
                OnPropertyChanged(nameof(CanPause));
            }
        }
        catch (OperationCanceledException)
        {
            // Pause or termination; swallow so the task ends cleanly.
        }
    }

    private void UpdateDisplay()
    {
        var time = TimeSpan.FromSeconds(Math.Max(0, _remainingSeconds));
        DisplayText = $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
    }

    /// <summary>
    /// Parses "00:00:03"-style (HH:MM:SS) input into a positive second count.
    /// </summary>
    public static bool TryParseInput(string? input, out int totalSeconds)
    {
        totalSeconds = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Trim().Split(':');
        if (parts.Length is < 1 or > 3)
        {
            return false;
        }

        int hours = 0;
        int minutes = 0;
        int seconds;
        int total;
        try
        {
            if (parts.Length == 3)
            {
                hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
                minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
                seconds = int.Parse(parts[2], CultureInfo.InvariantCulture);
            }
            else if (parts.Length == 2)
            {
                minutes = int.Parse(parts[0], CultureInfo.InvariantCulture);
                seconds = int.Parse(parts[1], CultureInfo.InvariantCulture);
            }
            else
            {
                seconds = int.Parse(parts[0], CultureInfo.InvariantCulture);
            }

            if (hours < 0 || minutes < 0 || seconds < 0)
            {
                return false;
            }

            total = checked((hours * 3600) + (minutes * 60) + seconds);
        }
        catch (Exception)
        {
            return false;
        }

        if (total <= 0)
        {
            return false;
        }

        totalSeconds = total;
        return true;
    }
}

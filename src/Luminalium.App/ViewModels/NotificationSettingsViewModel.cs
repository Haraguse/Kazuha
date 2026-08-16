using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class NotificationSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    private static readonly int[] IntervalSeconds = [0, 30, 60, 300, 600, 1800, 2700, 3600];

    public NotificationSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var settings = config?.Notifications ?? new NotificationsSettings();
        _initializing = true;
        timerNotifyEnabled = settings.TimerNotifyEnabled;
        selectedMonitorIntervalIndex = Array.IndexOf(IntervalSeconds, settings.ResourceMonitorInterval);
        if (selectedMonitorIntervalIndex < 0) selectedMonitorIntervalIndex = 0;
        _initializing = false;
    }

    [ObservableProperty]
    private bool timerNotifyEnabled;

    [ObservableProperty]
    private int selectedMonitorIntervalIndex;

    public string PageTitle => "通知";

    public string ResourceMonitorSectionTitle => "资源监视";

    public string ResourceMonitorLabel => "资源监视刷新间隔";

    public string ResourceMonitorDescription => "每隔指定时间刷新 CPU 与内存占用显示；0 表示关闭资源监视";

    public string TimerNotifySectionTitle => "定时器";

    public string TimerNotifyLabel => "定时器到点通知";

    public string TimerNotifyDescription => "倒计时结束时发送系统通知提醒";

    public IReadOnlyList<string> MonitorIntervals { get; } =
    [
        "关闭 (0 秒)",
        "30 秒",
        "1 分钟",
        "5 分钟",
        "10 分钟",
        "30 分钟",
        "45 分钟",
        "60 分钟"
    ];

    partial void OnTimerNotifyEnabledChanged(bool value) => Persist(c => c.Notifications.TimerNotifyEnabled = value);

    partial void OnSelectedMonitorIntervalIndexChanged(int value)
    {
        if (value >= 0 && value < IntervalSeconds.Length)
        {
            Persist(c => c.Notifications.ResourceMonitorInterval = IntervalSeconds[value]);
        }
    }

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}
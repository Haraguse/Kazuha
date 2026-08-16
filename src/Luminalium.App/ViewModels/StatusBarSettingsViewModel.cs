using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class StatusBarSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    public StatusBarSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var overlay = config?.Overlay ?? new OverlaySettings();
        _initializing = true;
        showStatusBar = overlay.ShowStatusBar;
        showTime = overlay.StatusBarShowTime;
        showSeconds = overlay.StatusBarShowSeconds;
        showBattery = overlay.StatusBarShowBattery;
        showVolume = overlay.StatusBarShowVolume;
        showNetwork = overlay.StatusBarShowNetwork;
        showMusic = overlay.StatusBarShowMusic;
        _initializing = false;
    }

    [ObservableProperty]
    private bool showStatusBar;

    [ObservableProperty]
    private bool showTime;

    [ObservableProperty]
    private bool showSeconds;

    [ObservableProperty]
    private bool showBattery;

    [ObservableProperty]
    private bool showVolume;

    [ObservableProperty]
    private bool showNetwork;

    [ObservableProperty]
    private bool showMusic;

    public string PageTitle => "状态栏";

    public string VisibilitySectionTitle => "显示状态栏";

    public string ShowStatusBarLabel => "显示状态栏";
    public string ShowStatusBarDescription => "在 Overlay 顶部显示状态栏，展示时间、电量等信息";

    public string ItemsSectionTitle => "状态栏项目";

    public string ShowTimeLabel => "显示时间";
    public string ShowSecondsLabel => "显示秒";
    public string ShowBatteryLabel => "显示电量";
    public string ShowVolumeLabel => "显示音量";
    public string ShowNetworkLabel => "显示网络";
    public string ShowMusicLabel => "显示音乐";

    partial void OnShowStatusBarChanged(bool value) => Persist(c => c.Overlay.ShowStatusBar = value);
    partial void OnShowTimeChanged(bool value) => Persist(c => c.Overlay.StatusBarShowTime = value);
    partial void OnShowSecondsChanged(bool value) => Persist(c => c.Overlay.StatusBarShowSeconds = value);
    partial void OnShowBatteryChanged(bool value) => Persist(c => c.Overlay.StatusBarShowBattery = value);
    partial void OnShowVolumeChanged(bool value) => Persist(c => c.Overlay.StatusBarShowVolume = value);
    partial void OnShowNetworkChanged(bool value) => Persist(c => c.Overlay.StatusBarShowNetwork = value);
    partial void OnShowMusicChanged(bool value) => Persist(c => c.Overlay.StatusBarShowMusic = value);

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using Luminalium.App.Services;
using Luminalium.Core.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Luminalium.App.ViewModels;

[SuppressMessage(
    "Performance",
    "CA1822:Mark members as static",
    Justification = "Settings labels are bound to the UI as instance members so compiled bindings resolve consistently.")]
public sealed partial class WindowSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    private static readonly string[] ScreenOptions = ["Auto", "Primary", "Screen1", "Screen2", "Screen3"];
    private static readonly int[] ZOrderIntervals = [100, 200, 500, 1000];

    public WindowSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var overlay = config?.Overlay ?? new OverlaySettings();
        _initializing = true;
        selectedScreenIndex = Array.IndexOf(ScreenOptions, overlay.OverlayScreen);
        if (selectedScreenIndex < 0) selectedScreenIndex = 0;
        selectedToolbarPositionIndex = (int)overlay.ToolbarPosition;
        selectedFlipperPositionIndex = (int)overlay.FlipperPosition;
        safeArea = overlay.SafeArea;
        scale = overlay.Scale;
        popWindowScale = overlay.PopWindowScale;
        strictEdgeAlignment = overlay.StrictEdgeAlignment;
        toolbarAutoHalfCollapse = overlay.ToolbarAutoHalfCollapse;
        uiAccessTopmost = overlay.UiAccessTopmost;
        allowRecording = overlay.AllowRecording;
        selectedZOrderIntervalIndex = Array.IndexOf(ZOrderIntervals, overlay.ZOrderCheckInterval);
        if (selectedZOrderIntervalIndex < 0) selectedZOrderIntervalIndex = 0;
        _initializing = false;
    }

    [ObservableProperty]
    private int selectedScreenIndex;

    [ObservableProperty]
    private int selectedToolbarPositionIndex;

    [ObservableProperty]
    private int selectedFlipperPositionIndex;

    [ObservableProperty]
    private int safeArea;

    [ObservableProperty]
    private double scale;

    [ObservableProperty]
    private double popWindowScale;

    [ObservableProperty]
    private bool strictEdgeAlignment;

    [ObservableProperty]
    private bool toolbarAutoHalfCollapse;

    [ObservableProperty]
    private bool uiAccessTopmost;

    [ObservableProperty]
    private bool allowRecording;

    [ObservableProperty]
    private int selectedZOrderIntervalIndex;

    public string PageTitle => "窗口";

    public string DisplaySectionTitle => "显示与位置";

    public string OverlayScreenLabel => "显示屏幕";
    public string ToolbarPositionLabel => "工具栏位置";
    public string FlipperPositionLabel => "翻页器位置";
    public string SafeAreaLabel => "安全区域";
    public string ScaleLabel => "缩放比例";
    public string PopWindowScaleLabel => "弹出窗口缩放";

    public string EdgeSectionTitle => "边缘与对齐";

    public string StrictEdgeAlignmentLabel => "严格边缘对齐";
    public string StrictEdgeAlignmentDescription => "让 Overlay 边缘吸附到屏幕边缘，移动时自动对齐";
    public string ToolbarAutoHalfCollapseLabel => "工具栏自动半折";
    public string ToolbarAutoHalfCollapseDescription => "鼠标离开工具栏后自动折叠为半宽状态";

    public string AdvancedSectionTitle => "高级";

    public string UiAccessTopmostLabel => "UI 访问置顶";
    public string UiAccessTopmostDescription => "让 Overlay 在系统 UI 应用切换时始终保持在最上层";
    public string AllowRecordingLabel => "允许录屏";
    public string AllowRecordingDescription => "允许 Overlay 出现在录屏或截图画面中";
    public string ZOrderCheckIntervalLabel => "置顶检查间隔";

    public IReadOnlyList<string> Screens => ScreenOptions;

    public IReadOnlyList<string> ToolbarPositions { get; } = ["顶部", "底部", "左侧", "右侧"];

    public IReadOnlyList<string> FlipperPositions { get; } = ["居中", "底部"];

    public IReadOnlyList<string> ZOrderIntervalsDisplay { get; } = ["100ms", "200ms", "500ms", "1000ms"];

    public string SafeAreaDisplay => SafeArea.ToString(CultureInfo.InvariantCulture);

    public string ScaleDisplay => Scale.ToString("0.##", CultureInfo.InvariantCulture);

    public string PopWindowScaleDisplay => PopWindowScale.ToString("0.##", CultureInfo.InvariantCulture);

    partial void OnSafeAreaChanged(int value)
    {
        OnPropertyChanged(nameof(SafeAreaDisplay));
        Persist(c => c.Overlay.SafeArea = value);
    }

    partial void OnScaleChanged(double value)
    {
        OnPropertyChanged(nameof(ScaleDisplay));
        Persist(c => c.Overlay.Scale = value);
    }

    partial void OnPopWindowScaleChanged(double value)
    {
        OnPropertyChanged(nameof(PopWindowScaleDisplay));
        Persist(c => c.Overlay.PopWindowScale = value);
    }

    partial void OnStrictEdgeAlignmentChanged(bool value) => Persist(c => c.Overlay.StrictEdgeAlignment = value);

    partial void OnToolbarAutoHalfCollapseChanged(bool value) => Persist(c => c.Overlay.ToolbarAutoHalfCollapse = value);

    partial void OnUiAccessTopmostChanged(bool value) => Persist(c => c.Overlay.UiAccessTopmost = value);

    partial void OnAllowRecordingChanged(bool value) => Persist(c => c.Overlay.AllowRecording = value);

    partial void OnSelectedScreenIndexChanged(int value)
    {
        if (value >= 0 && value < ScreenOptions.Length) Persist(c => c.Overlay.OverlayScreen = ScreenOptions[value]);
    }

    partial void OnSelectedToolbarPositionIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(ToolbarPosition), value)) Persist(c => c.Overlay.ToolbarPosition = (ToolbarPosition)value);
    }

    partial void OnSelectedFlipperPositionIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(FlipperPosition), value)) Persist(c => c.Overlay.FlipperPosition = (FlipperPosition)value);
    }

    partial void OnSelectedZOrderIntervalIndexChanged(int value)
    {
        if (value >= 0 && value < ZOrderIntervals.Length) Persist(c => c.Overlay.ZOrderCheckInterval = ZOrderIntervals[value]);
    }

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}
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
public sealed partial class BuiltInSettingsViewModel : ObservableObject
{
    private readonly SettingsCoordinator? _coordinator;
    private bool _initializing;

    private static readonly string[] BackgroundColorHexes = ["#FFFFFF", "#000000", "#F2F2F2", "#1E1E1E", "#FFFBEA"];

    public BuiltInSettingsViewModel(LuminaliumConfig? config = null, SettingsCoordinator? coordinator = null)
    {
        _coordinator = coordinator;
        var board = config?.BoardInBoard ?? new BoardInBoardSettings();
        var timer = config?.Timer ?? new TimerSettings();
        _initializing = true;
        selectedWindowPositionIndex = (int)board.WindowPosition;
        selectedBackgroundColorIndex = Array.IndexOf(BackgroundColorHexes, board.BackgroundColor);
        if (selectedBackgroundColorIndex < 0) selectedBackgroundColorIndex = 0;
        selectedEraserModeIndex = (int)board.EraserMode;
        selectedPenEffectIndex = (int)board.PenEffect;
        windowEnterAnimation = board.WindowEnterAnimation;
        selectedFullscreenBehaviorIndex = (int)timer.FullscreenBehavior;
        enableSoundEffects = timer.EnableSoundEffects;
        quickAddPresetsText = string.Join(", ", timer.QuickAddPresets);
        _initializing = false;
    }

    [ObservableProperty]
    private int selectedWindowPositionIndex;

    [ObservableProperty]
    private int selectedBackgroundColorIndex;

    [ObservableProperty]
    private int selectedEraserModeIndex;

    [ObservableProperty]
    private int selectedPenEffectIndex;

    [ObservableProperty]
    private bool windowEnterAnimation;

    [ObservableProperty]
    private int selectedFullscreenBehaviorIndex;

    [ObservableProperty]
    private bool enableSoundEffects;

    [ObservableProperty]
    private string quickAddPresetsText = string.Empty;

    public string PageTitle => "内建功能";

    public string BoardInBoardSectionTitle => "板中板";

    public string WindowPositionLabel => "窗口位置";
    public string BackgroundColorLabel => "背景颜色";
    public string EraserModeLabel => "橡皮擦模式";
    public string PenEffectLabel => "笔迹效果";
    public string WindowEnterAnimationLabel => "进入动画";
    public string WindowEnterAnimationDescription => "板中板出现时播放缩放与淡入动画";

    public string TimerSectionTitle => "计时器";

    public string FullscreenBehaviorLabel => "全屏行为";
    public string FullscreenBehaviorDescription => "计时器进入全屏时的窗口处理方式";
    public string EnableSoundEffectsLabel => "启用音效";
    public string EnableSoundEffectsDescription => "计时器开始、暂停与结束时播放提示音";
    public string QuickAddPresetsLabel => "快捷加时预设（分钟）";

    public IReadOnlyList<string> WindowPositions { get; } = ["右下", "右上", "左下", "左上"];

    public IReadOnlyList<string> BackgroundColors { get; } = ["白色", "黑色", "浅灰", "深灰", "米白"];

    public IReadOnlyList<string> EraserModes { get; } = ["点选", "笔画"];

    public IReadOnlyList<string> PenEffects { get; } = ["关闭", "有限", "完整"];

    public IReadOnlyList<string> FullscreenBehaviors { get; } = ["普通", "最大化", "全屏"];

    public string WindowEnterAnimationStatusText => WindowEnterAnimation ? "开" : "关";

    public string EnableSoundEffectsStatusText => EnableSoundEffects ? "开" : "关";

    partial void OnSelectedWindowPositionIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(BoardInBoardPosition), value)) Persist(c => c.BoardInBoard.WindowPosition = (BoardInBoardPosition)value);
    }

    partial void OnSelectedBackgroundColorIndexChanged(int value)
    {
        if (value >= 0 && value < BackgroundColorHexes.Length) Persist(c => c.BoardInBoard.BackgroundColor = BackgroundColorHexes[value]);
    }

    partial void OnSelectedEraserModeIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(BoardEraserMode), value)) Persist(c => c.BoardInBoard.EraserMode = (BoardEraserMode)value);
    }

    partial void OnSelectedPenEffectIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(BoardPenEffect), value)) Persist(c => c.BoardInBoard.PenEffect = (BoardPenEffect)value);
    }

    partial void OnWindowEnterAnimationChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowEnterAnimationStatusText));
        Persist(c => c.BoardInBoard.WindowEnterAnimation = value);
    }

    partial void OnSelectedFullscreenBehaviorIndexChanged(int value)
    {
        if (Enum.IsDefined(typeof(TimerFullscreenBehavior), value)) Persist(c => c.Timer.FullscreenBehavior = (TimerFullscreenBehavior)value);
    }

    partial void OnEnableSoundEffectsChanged(bool value)
    {
        OnPropertyChanged(nameof(EnableSoundEffectsStatusText));
        Persist(c => c.Timer.EnableSoundEffects = value);
    }

    partial void OnQuickAddPresetsTextChanged(string value)
    {
        var presets = value
            .Split(['，', ',', '、', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) ? minutes : -1)
            .Where(minutes => minutes >= 1)
            .ToArray();
        Persist(c => c.Timer.QuickAddPresets = presets);
    }

    private void Persist(Action<LuminaliumConfig> update)
    {
        if (!_initializing && _coordinator is not null)
        {
            _ = _coordinator.UpdateAsync(update);
        }
    }
}